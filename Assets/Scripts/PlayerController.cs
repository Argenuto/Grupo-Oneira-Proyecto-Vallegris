using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 16f;

    [Header("Jump Physics Tuning")]
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Game Feel Settings")]
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.2f, 0.2f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private InputAction jumpAction;

    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.right;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float nextAttackTime;

    private bool isJumping;
    private bool isHoldingJump;
    private bool canInteract;
    private Collider2D interactableObject;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

        jumpAction = playerInput.actions["Jump"];
    }

    private void Update()
    {
        isHoldingJump = jumpAction != null && jumpAction.IsPressed();

        bool grounded = IsGrounded();

        if (grounded && rb.linearVelocity.y <= 0.01f)
        {
            coyoteTimeCounter = coyoteTime;
            isJumping = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f && !isJumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            isJumping = true;
        }
    }

    private void FixedUpdate()
    {
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) || (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))
    {
        rb.linearVelocity = Vector2.zero;     
        return; 
    }
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        ApplyBetterJumpGravity();
    }

    private void ApplyBetterJumpGravity()
    {
        float gravityY = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;

        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.down * gravityY * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !isHoldingJump)
        {
            rb.linearVelocity += Vector2.down * gravityY * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }


    private void PerformAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        Vector2 attackDirection = facingDirection;


        Vector2 attackPosition = (Vector2)transform.position + (attackDirection * attackDistance);

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPosition, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            //Debug.Log("We hit " + enemy.name);
        }
    }


    private void OnMove(InputValue value)
    {

        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) || (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))
        {
            moveInput = Vector2.zero;
            Debug.Log(moveInput);
            return;
        }
        moveInput = value.Get<Vector2>();


        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            facingDirection = moveInput.x > 0 ? Vector2.right : Vector2.left;
        }
    }

    private void OnJump(InputValue value)
    {
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) || (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))        {
            return;
        }

        if (value.isPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void OnAttack(InputValue value)
    {

        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) || (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))
        {
            return;
        }

        if (value.isPressed && Time.time >= nextAttackTime)
        {
            PerformAttack();
        }

        if (canInteract && interactableObject != null)
        {
            interactableObject.GetComponent<DialogueTrigger>().Interact();
        }
    }


    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        Gizmos.color = Color.red;

        Vector2 drawDirection = facingDirection;


        Vector2 drawPosition = (Vector2)transform.position + (drawDirection * attackDistance);
        Gizmos.DrawWireSphere(drawPosition, attackRange);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Interactable")
        {
            canInteract = true;
            interactableObject = collision;
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Interactable")
        {
            canInteract = false;
            interactableObject = null;
        }
    }
}

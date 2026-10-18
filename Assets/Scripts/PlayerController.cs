using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 16f;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sprite;


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
    [SerializeField] private float comboWindow = 1.5f;

    [SerializeField] private float attackBufferTime = 0.2f; // Tiempo que el juego "recuerda" que presionaste atacar
    private float attackBufferCounter;
    private float lastAttackTime = 0f;
    private bool isSecondAttack = false;
    [SerializeField] private float attackDistance = 1f;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private MeleeAttack meleeAttack;
    [Header("Gravity Inversion Settings")]
    [SerializeField] private bool invertHorizontalInputWithCamera = true;
    public bool IsGravityInverted => isGravityInverted;
    private bool isGravityInverted;
    private float baseGravityScale;
    private float baseScaleY;


    public bool CanMove { get; set; } = true;
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
        animator = GetComponent<Animator>();

        jumpAction = playerInput.actions["Jump"];
        baseGravityScale = Mathf.Abs(rb.gravityScale);
        baseScaleY = Mathf.Abs(transform.localScale.y);
    }

    private void Update()
    {

        isHoldingJump = jumpAction != null && jumpAction.IsPressed();

        bool grounded = IsGrounded();
        float verticalVelocityRel = isGravityInverted ? -rb.linearVelocity.y : rb.linearVelocity.y;

        if (grounded && verticalVelocityRel <= 0.01f)
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

        if (attackBufferCounter > 0f)
        {
            attackBufferCounter -= Time.deltaTime;
        }

        if (attackBufferCounter > 0f && Time.time >= nextAttackTime)
        {
            PerformAttack();
            attackBufferCounter = 0f;
        }

        if (coyoteTimeCounter > 0f && jumpBufferCounter > 0f && !isJumping)
        {
            float jumpDir = isGravityInverted ? -1f : 1f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * jumpDir);

            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
            isJumping = true;
        }
    }

    private void FixedUpdate()
    {
        bool grounded = IsGrounded();
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) || (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (CanMove)
        {
            float horizontalInput = (isGravityInverted && invertHorizontalInputWithCamera) ? -moveInput.x : moveInput.x;
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
            if (facingDirection.x > 0)
                sprite.flipX = false;
            else if (facingDirection.x < 0)
                sprite.flipX = true;
        }
        animator.SetInteger("yVelocity", (int)Math.Floor(rb.linearVelocityY * 100f));
        if (grounded)
            animator.SetInteger("yVelocity", 0);

        if (rb.linearVelocity.x != 0)
            animator.SetBool("isWalking", true);
        else
            animator.SetBool("isWalking", false);
        ApplyBetterJumpGravity();
    }

    private void ApplyBetterJumpGravity()
    {
        float gravityY = Mathf.Abs(Physics2D.gravity.y) * Mathf.Abs(rb.gravityScale);
        Vector2 gravityDir = isGravityInverted ? Vector2.up : Vector2.down;
        float velocityAlongGravity = Vector2.Dot(rb.linearVelocity, gravityDir);

        if (velocityAlongGravity > 0f)
        {
            rb.linearVelocity += gravityDir * gravityY * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (velocityAlongGravity < 0f && !isHoldingJump)
        {
            rb.linearVelocity += gravityDir * gravityY * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    public void SetGravityInverted(bool inverted)
    {
        if (isGravityInverted == inverted) return;
        isGravityInverted = inverted;

        rb.gravityScale = inverted ? -baseGravityScale : baseGravityScale;

        Vector3 scale = transform.localScale;
        scale.y = inverted ? -baseScaleY : baseScaleY;
        transform.localScale = scale;

        isJumping = true;
        coyoteTimeCounter = 0f;

        // Actualizar dirección visual si hay input activo
        float horizontalInput = (isGravityInverted && invertHorizontalInputWithCamera) ? -moveInput.x : moveInput.x;
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            facingDirection = horizontalInput > 0 ? Vector2.right : Vector2.left;
        }
    }


    private void PerformAttack()
    {
        // Tu lógica de cooldown
        nextAttackTime = Time.time + attackCooldown;

        // 1. Si pasó demasiado tiempo desde el último ataque, reiniciamos el combo
        if (Time.time - lastAttackTime > comboWindow)
        {
            isSecondAttack = false;
        }

        // 2. Alternar animaciones
        if (!isSecondAttack)
        {
            animator.Play("attack", -1, 0f);
            isSecondAttack = true; // El próximo será el ataque 2
        }
        else
        {
            animator.Play("attack2", -1, 0f);
            isSecondAttack = false; // El próximo volverá a ser el ataque 1
        }

        // 3. Guardamos el tiempo actual para calcular la ventana del próximo combo
        lastAttackTime = Time.time;

        // Tu lógica original de posición y ejecución de daño
        Vector2 attackDirection = facingDirection;
        Vector2 attackPosition = (Vector2)transform.position + (attackDirection * attackDistance);

        meleeAttack.Perform(attackPosition, attackRange);
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

        float effectiveInputX = (isGravityInverted && invertHorizontalInputWithCamera) ? -moveInput.x : moveInput.x;

        if (Mathf.Abs(effectiveInputX) > 0.1f)
        {
            facingDirection = effectiveInputX > 0 ? Vector2.right : Vector2.left;
        }
    }

    private void OnJump(InputValue value)
    {
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) || (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))
        {
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

        if (value.isPressed)
        {
            attackBufferCounter = attackBufferTime;
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
    public void setAttackTrue()
    {
      
        animator.SetBool("isAttacking", true);

    }

     public void setAttackFalse()
    {
        animator.SetBool("isAttacking", false);
    }

    
}

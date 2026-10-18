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

    [Header("Climb Settings")]
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.2f, 0.5f);
    [SerializeField] private LayerMask climbableLayer;

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
    [SerializeField] private float attackBufferTime = 0.2f;

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
    private float baseScaleX;

    public bool CanMove { get; set; } = true;
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private InputAction jumpAction;

    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.right;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    private float climbCooldownTimer;
    private float nextAttackTime;

    private bool isJumping;
    private bool isHoldingJump;
    private bool canInteract;
    private Collider2D interactableObject;

    // Climb State Variables
    private bool isTouchingWall;
    private bool isClimbing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        jumpAction = playerInput.actions["Jump"];
        baseGravityScale = Mathf.Abs(rb.gravityScale);
        baseScaleY = Mathf.Abs(transform.localScale.y);
        baseScaleX = Mathf.Abs(transform.localScale.x);
    }

    /// <summary>
    /// Aplica el volteo horizontal modificando la escala local del personaje.
    /// </summary>
    private void UpdateFlip()
    {
        Vector3 scale = transform.localScale;
        bool flipHorizontally = (facingDirection.x < 0f) != isGravityInverted;
        scale.x = flipHorizontally ? -baseScaleX : baseScaleX;
        transform.localScale = scale;
    }

    private void Update()
    {
        isHoldingJump = jumpAction != null && jumpAction.IsPressed();

        bool grounded = IsGrounded();
        float verticalVelocityRel = isGravityInverted ? -rb.linearVelocity.y : rb.linearVelocity.y;

        if (climbCooldownTimer > 0f)
        {
            climbCooldownTimer -= Time.deltaTime;
        }

        // --- Climbing Logic ---
        isTouchingWall = IsTouchingWall();

        if (isTouchingWall && Mathf.Abs(moveInput.y) > 0.1f && climbCooldownTimer <= 0f)
        {
            isClimbing = true;
            isJumping = false; // <--- FIX: Reset jump state when grabbing a wall
        }
        else if (!isTouchingWall || grounded)
        {
            isClimbing = false;
        }

        if (grounded && verticalVelocityRel <= 0.01f && !isClimbing)
        {
            coyoteTimeCounter = coyoteTime;
            isJumping = false;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        if (attackBufferCounter > 0f)
            attackBufferCounter -= Time.deltaTime;

        if (attackBufferCounter > 0f && Time.time >= nextAttackTime)
        {
            PerformAttack();
            attackBufferCounter = 0f;
        }

        // --- Jump Logic ---
        if (coyoteTimeCounter > 0f || isClimbing)
        {
            // FIX: Added "|| isClimbing" inside the parenthesis so the jump block runs on walls
            if (jumpBufferCounter > 0f && (!isJumping || isClimbing))
            {
                float jumpDir = isGravityInverted ? -1f : 1f;

                if (isClimbing)
                {
                    climbCooldownTimer = 0.2f;
                    isClimbing = false;
                }

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * jumpDir);

                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                isJumping = true;
            }
        }
    }
    private void FixedUpdate()
    {
        bool grounded = IsGrounded();
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) ||
            (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isClimbing)
        {
            // Stop gravity while climbing
            rb.gravityScale = 0f;

            // Apply vertical climbing velocity
            float verticalClimbSpeed = moveInput.y * climbSpeed;
            if (isGravityInverted) verticalClimbSpeed *= -1f;

            float horizontalInput = (isGravityInverted && invertHorizontalInputWithCamera) ? -moveInput.x : moveInput.x;
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, verticalClimbSpeed);

            // --- ANIMATION LOGIC FOR CLIMBING ---
            animator.SetBool("isClimbing", true);
            animator.SetBool("isWalking", false);

            // Freeze animation if not moving vertically, play normally if moving
            if (Mathf.Abs(moveInput.y) > 0.1f)
            {
                animator.speed = 1f;
            }
            else
            {
                animator.speed = 0f;
            }
        }
        else
        {
            // --- RESET ANIMATION LOGIC FOR NORMAL MOVEMENT ---
            animator.SetBool("isClimbing", false);
            animator.speed = 1f; // IMPORTANT: Always reset speed to 1 when not climbing!

            // Restore proper gravity scale
            rb.gravityScale = isGravityInverted ? -baseGravityScale : baseGravityScale;

            if (CanMove)
            {
                float horizontalInput = (isGravityInverted && invertHorizontalInputWithCamera) ? -moveInput.x : moveInput.x;
                rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

                if (facingDirection.x > 0)
                    sprite.flipX = false;
                else if (facingDirection.x < 0)
                    sprite.flipX = true;
            }

            animator.SetInteger("yVelocity", (int)Math.Floor(rb.linearVelocity.y * 100f));
            if (grounded)
                animator.SetInteger("yVelocity", 0);

            if (rb.linearVelocity.x != 0 && grounded)
                animator.SetBool("isWalking", true);
            else
                animator.SetBool("isWalking", false);

            ApplyBetterJumpGravity();
        }
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

        if (!isClimbing)
        {
            rb.gravityScale = inverted ? -baseGravityScale : baseGravityScale;
        }

        // Rotar el personaje 180° en Z para que los pies queden orientados hacia el techo (el nuevo suelo)
        transform.localRotation = Quaternion.Euler(0f, 0f, inverted ? 180f : 0f);

        // Mantener la escala positiva en Y para evitar invertir la física del collider
        Vector3 scale = transform.localScale;
        scale.y = baseScaleY;
        transform.localScale = scale;

        isJumping = true;
        coyoteTimeCounter = 0f;

        float horizontalInput = (isGravityInverted && invertHorizontalInputWithCamera) ? -moveInput.x : moveInput.x;
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            facingDirection = horizontalInput > 0 ? Vector2.right : Vector2.left;
        }

        // Aplicar flip visual con la nueva orientación
        UpdateFlip();
    }

    private void PerformAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        if (Time.time - lastAttackTime > comboWindow)
        {
            isSecondAttack = false;
        }

        if (!isSecondAttack)
        {
            animator.Play("attack", -1, 0f);
            isSecondAttack = true;
        }
        else
        {
            animator.Play("attack2", -1, 0f);
            isSecondAttack = false;
        }

        lastAttackTime = Time.time;

        Vector2 attackDirection = facingDirection;
        Vector2 attackPosition = (Vector2)transform.position + (attackDirection * attackDistance);

        meleeAttack.Perform(attackPosition, attackRange);
    }

    private void OnMove(InputValue value)
    {
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) ||
            (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = value.Get<Vector2>();

        float effectiveInputX = (isGravityInverted && invertHorizontalInputWithCamera) ? -moveInput.x : moveInput.x;

        if (Mathf.Abs(effectiveInputX) > 0.1f && !isClimbing)
        {
            facingDirection = effectiveInputX > 0 ? Vector2.right : Vector2.left;
            UpdateFlip();
        }
    }

    private void OnJump(InputValue value)
    {
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) ||
            (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))
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
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) ||
            (ItemObtainedManager.Instance != null && ItemObtainedManager.Instance.IsActive))
        {
            return;
        }

        if (value.isPressed && GameManager.Instance != null && GameManager.Instance.isAttackUnlocked)
        {
            attackBufferCounter = attackBufferTime;
        }

        if (canInteract && interactableObject != null && !isClimbing)
        {
            interactableObject.GetComponent<DialogueTrigger>().Interact();
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer) != null;
    }

    private bool IsTouchingWall()
    {
        if (wallCheck == null) return false;
        return Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, climbableLayer) != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(wallCheck.position, wallCheckSize);
        }

        Gizmos.color = Color.red;
        Vector2 drawDirection = facingDirection;
        Vector2 drawPosition = (Vector2)transform.position + (drawDirection * attackDistance);
        Gizmos.DrawWireSphere(drawPosition, attackRange);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactable"))
        {
            canInteract = true;
            interactableObject = collision;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactable"))
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
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 16f;

    [SerializeField] private string sceneToLoad;

    [Header("Interaction Settings")]
    [SerializeField] private SpriteRenderer interactPromptSprite;
    [SerializeField] private SpriteRenderer interactPromptBG;

    [SerializeField] private float promptFadeDuration = 0.2f;

    [Header("Climb Settings")]
    [SerializeField] private float climbSpeed = 5f;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.2f, 0.5f);
    [SerializeField] private LayerMask climbableLayer;

    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    //[SerializeField] private SpriteRenderer sprite;

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

    [Header("Destroy Interaction Settings")]
    [SerializeField] private float destroyDelayBeforeFade = 0.5f; // Time for the animation to play
    [SerializeField] private float objectFadeDuration = 1f;       // How long the fade out takes
    [SerializeField] private string interactAnimationName = "interact"; // Name of the animation state

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

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkSFX;
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private float walkSFXInterval = 0.3f;
    private float walkTimer;

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
    private bool canDestroy;
    private Collider2D interactableObject;
    private Coroutine promptFadeCoroutine;

    private int velocityConverted;

    // Climb State Variables
    private bool isTouchingWall;
    private bool isClimbing;

    private void Awake()
    {
        StartCoroutine(FadeManager.Instance.FadeToClear());
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        animator = GetComponent<Animator>();

        // Make the prompt transparent at the start
        if (interactPromptSprite != null)
        {
            Color c = interactPromptSprite.color;
            c.a = 0f;
            interactPromptSprite.color = c;
            interactPromptBG.color = c;
        }

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
                PlaySFX(jumpSFX);
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

                /*if (facingDirection.x > 0)
                    sprite.flipX = false;
                else if (facingDirection.x < 0)
                    sprite.flipX = true;*/
            }

            if (isGravityInverted)
                velocityConverted = (int)Math.Floor(rb.linearVelocity.y * -100f);
            else
                velocityConverted = (int)Math.Floor(rb.linearVelocity.y * 100f);

            animator.SetInteger("yVelocity", velocityConverted);
            if (grounded)
                animator.SetInteger("yVelocity", 0);

            if (rb.linearVelocity.x != 0 && grounded)
            {
                animator.SetBool("isWalking", true);

                // Add this block for footstep sounds
                walkTimer -= Time.fixedDeltaTime;
                if (walkTimer <= 0f)
                {
                    PlaySFX(walkSFX);
                    walkTimer = walkSFXInterval;
                }
            }
            else
            {
                animator.SetBool("isWalking", false);
                walkTimer = 0f; // Reset timer when stopping
            }
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
        PlaySFX(attackSFX);
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
    [UnityEngine.Scripting.Preserve]
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
    [UnityEngine.Scripting.Preserve]
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
    [UnityEngine.Scripting.Preserve]
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
    [UnityEngine.Scripting.Preserve]
    private void OnAstral(InputValue value)
    {
        Debug.Log("ggbeo");
        if (value.isPressed && GameManager.Instance != null && GameManager.Instance.isDestroyUnlocked)
        {
            Debug.Log("wpbozo");

            if (canDestroy && interactableObject != null && !isClimbing)
            {
                Debug.Log("wtfbrotha");

                // 1. Play the player animation
                animator.Play(interactAnimationName, -1, 0f);

                // 2. Start the coroutine to fade and disable the object
                StartCoroutine(FadeAndDisableObject(interactableObject.gameObject));

                // Clear the reference so the player can't spam the interaction
                canDestroy = false;
                interactableObject = null;
            }
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

            if (promptFadeCoroutine != null) StopCoroutine(promptFadeCoroutine);
            promptFadeCoroutine = StartCoroutine(FadePrompt(1f));
        }
        else if (collision.CompareTag("Destroyable"))
        {
            canDestroy = true;
            interactableObject = collision;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Interactable"))
        {
            canInteract = false;
            interactableObject = null;

            if (promptFadeCoroutine != null) StopCoroutine(promptFadeCoroutine);
            promptFadeCoroutine = StartCoroutine(FadePrompt(0f));
        }
        else if (collision.CompareTag("Destroyable"))
        {
            Debug.Log("lol bro");

            canDestroy = false;
            interactableObject = null;
        }
    }

    private IEnumerator FadePrompt(float targetAlpha)
    {
        if (interactPromptSprite == null) yield break;

        Color currentColor = interactPromptSprite.color;
        float startAlpha = currentColor.a;
        float time = 0f;

        while (time < promptFadeDuration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / promptFadeDuration);
            interactPromptSprite.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
            interactPromptBG.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
            yield return null;
        }

        interactPromptSprite.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);
        interactPromptBG.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);

    }

    public void setAttackTrue()
    {
        animator.SetBool("isAttacking", true);
    }

    public void setAttackFalse()
    {
        animator.SetBool("isAttacking", false);
    }

    public IEnumerator DieScene()
    {


        yield return new WaitForSeconds(4f);
        yield return StartCoroutine(FadeManager.Instance.FadeToBlack());

        SceneManager.LoadScene(sceneToLoad);

    }

    private IEnumerator FadeAndDisableObject(GameObject targetObject)
    {
        // Wait for the player's animation to reach the right moment
        yield return new WaitForSeconds(destroyDelayBeforeFade);

        // Try to get the SpriteRenderer of the object we are destroying
        SpriteRenderer targetSprite = targetObject.GetComponent<SpriteRenderer>();

        if (targetSprite != null)
        {
            Color startColor = targetSprite.color;
            float time = 0f;

            // Fade out over the specified duration
            while (time < objectFadeDuration)
            {
                time += Time.deltaTime;
                float newAlpha = Mathf.Lerp(1f, 0f, time / objectFadeDuration);
                targetSprite.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
                yield return null;
            }
        }

        // Disable the object entirely once the fade is complete
        targetObject.SetActive(false);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
        }
    }
}
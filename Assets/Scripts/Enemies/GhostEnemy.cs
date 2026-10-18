using UnityEngine;

/// <summary>
/// Enemigo híbrido tipo Fantasma.
/// Se desplaza horizontalmente (patrullaje / persecución terrestre), pero levita
/// suspendido en el aire sin tocar físicamente el suelo ni sufrir fricción.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class GhostEnemy : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float chasingMultiplier = 2.5f;

    [SerializeField] private float patrolDistance = 4f;
    [SerializeField] private float playerDetectionRange = 6f;

    [Header("Levitación")]
    [Tooltip("Altura objetivo respecto al suelo/techo")]
    [SerializeField] private float hoverHeight = 1.2f;
    [Tooltip("Frecuencia del balanceo flotante")]
    [SerializeField] private float bobFrequency = 2.5f;
    [Tooltip("Amplitud del balanceo flotante")]
    [SerializeField] private float bobAmplitude = 0.15f;
    [Tooltip("Capa de superficies para mantener distancia flotante")]
    [SerializeField] private LayerMask surfaceLayer;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 0.3f; // Tiempo que dura el empuje
    private float knockbackTimer = 0f;

    private float detectionTimer = 0f;
    private bool wasChasing = false;

    [Header("Animaciones")]
    [SerializeField] private Animator animator;
    [Tooltip("Tiempo que el enemigo se detiene a reproducir su animación al encontrar al jugador")]
    [SerializeField] private float detectionPauseDuration = 0.5f;

    [Header("Referencias Opcionales")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private Transform playerTransform;
    private PlayerController playerController;

    private Vector2 spawnPosition;
    private int patrolDirection = 1;
    private float baseScaleX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Buscar el Animator automáticamente si no se asignó en el inspector
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        spawnPosition = transform.position;
        baseScaleX = Mathf.Abs(transform.localScale.x);

        FindPlayer();
    }
    private void OnEnable()
    {
        spawnPosition = transform.position;
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }
    }

    private void FixedUpdate()
    {
        // 1. Prioridad máxima: Si está sufriendo knockback, no hacemos nada para que la física actúe
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            return; 
        }

        if (playerTransform == null)
            FindPlayer();

        // 2. Comprobar distancias
        bool isChasing = false;
        if (playerTransform != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            isChasing = distToPlayer <= playerDetectionRange;
        }

        // 3. Lógica de animación de "Encontrar al jugador"
        if (isChasing && !wasChasing)
        {
            // Justo en este frame entró en el rango
            if (animator != null) animator.SetTrigger("Detect");
            detectionTimer = detectionPauseDuration; // Iniciar pausa
        }
        wasChasing = isChasing; // Guardar el estado para el siguiente frame

        // 4. Si está en medio de la animación de detección, solo levita en su sitio y no avanza
        if (detectionTimer > 0f)
        {
            detectionTimer -= Time.fixedDeltaTime;
            
            float hoverY = GetHoverTargetY();
            float pausedY = Mathf.Lerp(rb.position.y, hoverY, Time.fixedDeltaTime * 6f);
            rb.MovePosition(new Vector2(rb.position.x, pausedY)); // Mantiene X, actualiza Y
            return;
        }

        // 5. MOVIMIENTO NORMAL (Persecución o Patrulla)
        Vector2 nextPosition;

        if (isChasing)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            nextPosition = rb.position + (direction * moveSpeed * chasingMultiplier * Time.fixedDeltaTime);

            if (spriteRenderer != null) spriteRenderer.flipX = direction.x > 0f;
        }
        else
        {
            float targetDirX = GetPatrolDirection();
            float newX = rb.position.x + (targetDirX * moveSpeed * Time.fixedDeltaTime);

            float targetY = GetHoverTargetY();
            float newY = Mathf.Lerp(rb.position.y, targetY, Time.fixedDeltaTime * 6f);

            nextPosition = new Vector2(newX, newY);

            if (spriteRenderer != null) spriteRenderer.flipX = targetDirX > 0f;
        }

        rb.MovePosition(nextPosition);
    }

    private float GetPatrolDirection()
    {
        // Patrullaje de ida y vuelta alrededor del punto de spawn
        if (transform.position.x > spawnPosition.x + patrolDistance)
            patrolDirection = -1;
        else if (transform.position.x < spawnPosition.x - patrolDistance)
            patrolDirection = 1;

        return patrolDirection;
    }

    private float GetHoverTargetY()
    {
        bool isInverted = playerController != null && playerController.IsGravityInverted;
        Vector2 checkDir = isInverted ? Vector2.up : Vector2.down;

        // Raycast para buscar el suelo/techo
        RaycastHit2D hit = Physics2D.Raycast(transform.position, checkDir, hoverHeight * 3f, surfaceLayer);

        // Fallback: Si no detecta suelo (ej: pozo sin fondo), se mantiene en la altura donde apareció
        float targetY = spawnPosition.y; 

        if (hit.collider != null)
        {
            float surfaceOffset = isInverted ? -hoverHeight : hoverHeight;
            targetY = hit.point.y + surfaceOffset;
        }

        // Añadir ondulación senoidal de fantasma
        float bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        targetY += bobOffset;

        return targetY;
    }

    private void UpdateHorizontalMovement()
    {
        float targetDirX = patrolDirection;

        // Comprobar si el jugador está en rango de persecución terrestre
        if (playerTransform != null)
        {
            float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distToPlayer <= playerDetectionRange)
            {
                float diffX = playerTransform.position.x - transform.position.x;
                if (Mathf.Abs(diffX) > 0.2f)
                {
                    targetDirX = Mathf.Sign(diffX);
                }
            }
            else
            {
                // Patrullaje de ida y vuelta alrededor del punto de spawn
                if (transform.position.x > spawnPosition.x + patrolDistance)
                    patrolDirection = -1;
                else if (transform.position.x < spawnPosition.x - patrolDistance)
                    patrolDirection = 1;

                targetDirX = patrolDirection;
            }
        }

        // Aplicar velocidad horizontal continua
        rb.linearVelocity = new Vector2(targetDirX * moveSpeed, rb.linearVelocity.y);

        // Orientación visual del sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = targetDirX > 0f;
        }
    }

    private void UpdateHoverAltitude()
    {
        bool isInverted = playerController != null && playerController.IsGravityInverted;
        Vector2 checkDir = isInverted ? Vector2.up : Vector2.down;

        // Raycast para mantener distancia exacta de levitación respecto a la superficie (suelo o techo)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, checkDir, hoverHeight * 3f, surfaceLayer);

        float targetY = transform.position.y;
        if (hit.collider != null)
        {
            float surfaceOffset = isInverted ? -hoverHeight : hoverHeight;
            targetY = hit.point.y + surfaceOffset;
        }

        // Añadir ondulación senoidal sutil de fantasma
        float bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        targetY += bobOffset;

        // Suavizado vertical para no dar tirones
        float newY = Mathf.Lerp(transform.position.y, targetY, Time.fixedDeltaTime * 6f);
        rb.MovePosition(new Vector2(rb.position.x, newY));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);

        Gizmos.color = Color.yellow;
        Vector2 origin = Application.isPlaying ? spawnPosition : (Vector2)transform.position;
        Gizmos.DrawLine(origin + Vector2.left * patrolDistance, origin + Vector2.right * patrolDistance);
    }

    public void TriggerKnockbackState()
    {
        knockbackTimer = knockbackDuration;
        
        // Disparar animación de daño
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }
}


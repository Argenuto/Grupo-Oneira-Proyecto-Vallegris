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
        // Los fantasmas levitan sin fricción rígida
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

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
        if (playerTransform == null)
        {
            FindPlayer();
        }

        UpdateHorizontalMovement();
        UpdateHoverAltitude();
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
            spriteRenderer.flipX = targetDirX < 0f;
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
}


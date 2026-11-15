using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile : DamageDealer
{
    private const int MaximumActiveProjectiles = 6;
    private static int activeProjectileCount;
    private static int activeHazardCount;

    [SerializeField] private float lifetime = 6f;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private bool destroyOnHit = true;

    private Rigidbody2D rb;
    private BlindPayload blind;
    private bool registeredAsHazard;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static int nextDebugId;
    private int debugId;
    private float spawnTime;
    private Vector2 spawnPosition;
    private string debugCause = "lifetime";
    private string debugDetail = "";
#endif

    public static int ActiveProjectileCount => activeProjectileCount;
    public static int ActiveHazardCount => activeHazardCount;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public LayerMask ObstacleLayers => obstacleLayers;

    public static void ResetDebugIds()
    {
        nextDebugId = 0;
    }
#endif

    public static bool CanSpawn(int count = 1)
    {
        return count > 0 && activeProjectileCount + count <= MaximumActiveProjectiles;
    }

    private void Awake()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        debugId = ++nextDebugId;
        spawnTime = Time.time;
        spawnPosition = transform.position;
        BossDebug.ProjectileCreated();
#endif
        activeProjectileCount++;
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.gravityScale = 0f;
    }

    private void Start() { Destroy(gameObject, lifetime); }

    public void Init(Vector2 direction, float speed, BlindPayload blindPayload, float damageOverride, bool arenaHazard = false)
    {
        blind = blindPayload;
        registeredAsHazard = arenaHazard;
        if (registeredAsHazard)
            activeHazardCount++;
        if (damageOverride > 0f)
            damage = damageOverride;

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float age = Time.time - spawnTime;
        if (debugCause == "lifetime" && age < lifetime - 0.1f)
            debugCause = "unknown";
        BossDebug.ProjectileDeath(debugId, age, debugCause, debugDetail, transform.position,
                                  spawnPosition, Vector2.Distance(transform.position, spawnPosition));
#endif
        activeProjectileCount = Mathf.Max(0, activeProjectileCount - 1);
        if (registeredAsHazard)
            activeHazardCount = Mathf.Max(0, activeHazardCount - 1);
    }

    public static void ClearAll()
    {
        foreach (BossProjectile projectile in Object.FindObjectsByType<BossProjectile>(FindObjectsInactive.Include))
        {
            if (projectile == null)
                continue;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            projectile.debugCause = "clearAll";
#endif
            projectile.gameObject.SetActive(false);
            Destroy(projectile.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int bit = 1 << other.gameObject.layer;

        if ((targetLayers.value & bit) != 0)
        {
            if (!TryDealDamage(other, other.ClosestPoint(transform.position)))
                return;

            if (blind != null && blind.applyBlind && other.GetComponentInParent<PlayerStatusEffects>() is PlayerStatusEffects fx)
                fx.ApplyBlind(blind.duration, blind.intensity);

            if (destroyOnHit)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                debugCause = "player";
                debugDetail = other.gameObject.name;
#endif
                Destroy(gameObject);
            }
        }
        else if ((obstacleLayers.value & bit) != 0)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugCause = "obstacle";
            debugDetail = $"{other.gameObject.name}|layer={LayerMask.LayerToName(other.gameObject.layer)}|trigger={other.isTrigger}|bounds=({BossDebug.Format(other.bounds.min.x)},{BossDebug.Format(other.bounds.min.y)})-({BossDebug.Format(other.bounds.max.x)},{BossDebug.Format(other.bounds.max.y)})";
#endif
            Destroy(gameObject);
        }
    }
}

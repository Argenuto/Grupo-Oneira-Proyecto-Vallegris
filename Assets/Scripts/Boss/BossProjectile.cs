using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossProjectile : DamageDealer
{
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private bool destroyOnHit = true;

    private Rigidbody2D rb;
    private BlindPayload blind;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.gravityScale = 0f;
    }

    private void Start() { Destroy(gameObject, lifetime); }

    public void Init(Vector2 direction, float speed, BlindPayload blindPayload, float damageOverride)
    {
        blind = blindPayload;
        if (damageOverride > 0f)
            damage = damageOverride;

        if (rb != null)
        {
            rb.linearVelocity = direction.normalized * speed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
                Destroy(gameObject);
        }
        else if ((obstacleLayers.value & bit) != 0)
        {
            Destroy(gameObject);
        }
    }
}

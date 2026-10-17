using UnityEngine;


public class ContactDamage : DamageDealer
{
    [Header("Contacto")]
    [Tooltip("Segundos mínimos entre golpes, para no dañar en cada frame")]
    [SerializeField] private float hitCooldown = 0.5f;

    private Health owner;
    private float nextHitTime;

    private void Awake()
    {
        // Si el dueño está muerto, no debe seguir haciendo daño.
        owner = GetComponentInParent<Health>();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other, other.ClosestPoint(transform.position));
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Vector2 point = collision.contactCount > 0
            ? collision.GetContact(0).point
            : (Vector2)collision.transform.position;

        TryHit(collision.collider, point);
    }

    private void TryHit(Collider2D target, Vector2 hitPoint)
    {
        if (Time.time < nextHitTime) return;
        if (owner != null && owner.IsDead) return;
        if ((targetLayers.value & (1 << target.gameObject.layer)) == 0) return;

        if (TryDealDamage(target, hitPoint))
            nextHitTime = Time.time + hitCooldown;
    }
}

using UnityEngine;

public abstract class DamageDealer : MonoBehaviour
{
    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float knockbackForce = 6f;
    [SerializeField] protected LayerMask targetLayers;

    protected bool TryDealDamage(Collider2D target, Vector2 hitPoint)
    {
        if (!target.TryGetComponent(out IDamageable damageable)) return false;
        if (damageable.IsDead) return false;

        Vector2 dir = ((Vector2)target.transform.position - (Vector2)transform.position).normalized;
        damageable.TakeDamage(new DamageInfo(damage, gameObject, hitPoint, dir, knockbackForce));
        return true;
    }
}
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : DamageDealer
{
    [SerializeField] private Animator animatorController;
    public void Perform(Vector2 position, float radius)
    {
        // Se deduplica por IDamageable (no por collider): un enemigo con varios
        // colliders recibe un solo golpe por ataque.
        var hitTargets = new HashSet<IDamageable>();
        foreach (Collider2D col in Physics2D.OverlapCircleAll(position, radius, targetLayers))
        {
            if (col.TryGetComponent(out IDamageable target) && hitTargets.Add(target))
                TryDealDamage(col, position);
        }
    }

    override protected bool TryDealDamage(Collider2D target, Vector2 hitPoint)
    {
        bool result = base.TryDealDamage(target, hitPoint);
        if (result && animatorController != null)
            animatorController.SetTrigger("AttackHit");
        return result;
    }
}

using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack: DamageDealer
{
public void Perform(Vector2 position, float radius)
    {
        var hitTargets = new HashSet<Collider2D>();
        foreach (var collider in Physics2D.OverlapCircleAll(position, radius, targetLayers))
        {
            if (hitTargets.Add(collider))
                TryDealDamage(collider, position);
        }
    }
}

using UnityEngine;

/// <summary>
/// Vida del jefe. Hereda todo de EnemyHealth y añade invulnerabilidad para transiciones de fase.
/// </summary>
public class BossHealth : EnemyHealth
{
    public bool Invulnerable { get; set; }
    public float DamageTakenMultiplier { get; set; } = 1f;

    protected override bool CanTakeDamage(DamageInfo damageInfo)
    {
        if (Invulnerable)
        {
            BossDebug.DamageBlocked(BossDebug.DamageSource(damageInfo.source), "Boss", "bossInvulnerable", damageInfo.amount);
            return false;
        }

        return base.CanTakeDamage(damageInfo);
    }

    protected override void HandleDamaged(DamageInfo damageInfo)
    {
        base.HandleDamaged(damageInfo);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        BossController controller = GetComponent<BossController>();
        BossDebug.BossDamage(damageInfo, CurrentHealth, MaxHealth, DamageTakenMultiplier,
                             controller != null ? controller.CurrentPhaseIndex : -1);
#endif
    }

    protected override float ModifyDamage(DamageInfo damageInfo)
    {
        return base.ModifyDamage(damageInfo) * Mathf.Max(1f, DamageTakenMultiplier);
    }
    
   
}

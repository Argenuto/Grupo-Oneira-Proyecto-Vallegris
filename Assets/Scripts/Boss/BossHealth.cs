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
        return !Invulnerable && base.CanTakeDamage(damageInfo);
    }

    protected override float ModifyDamage(DamageInfo damageInfo)
    {
        return base.ModifyDamage(damageInfo) * Mathf.Max(1f, DamageTakenMultiplier);
    }
    
   
}

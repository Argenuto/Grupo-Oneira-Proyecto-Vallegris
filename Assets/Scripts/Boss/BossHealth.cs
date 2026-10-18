/// <summary>
/// Vida del jefe. Hereda todo de EnemyHealth y añade invulnerabilidad para transiciones de fase.
/// </summary>
public class BossHealth : EnemyHealth
{
    public bool Invulnerable { get; set; }

    protected override bool CanTakeDamage(DamageInfo damageInfo)
    {
        return !Invulnerable && base.CanTakeDamage(damageInfo);
    }
}

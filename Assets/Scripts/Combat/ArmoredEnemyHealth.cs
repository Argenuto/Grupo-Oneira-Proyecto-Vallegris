using UnityEngine;

public class ArmoredEnemyHealth : EnemyHealth
{
    [SerializeField] private float flatReduction = 5f;

    override protected float ModifyDamage(DamageInfo damageInfo)
    {
        return Mathf.Max(0f, damageInfo.amount - flatReduction);
    }
}

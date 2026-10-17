using UnityEngine;

public interface IDamageable
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsDead { get; }

    void TakeDamage(DamageInfo info);
    void Heal(float amount);
    void ResetHealth();   // vuelve a vida máxima y revive
}

[System.Serializable]
public struct DamageInfo
{
    public float amount;
    public GameObject source;
    public Vector2 hitPoint;
    public Vector2 knockbackDirection; // normalizada
    public float knockbackForce;


    public DamageInfo(float amount, GameObject source, Vector2 hitPoint,
                      Vector2 knockbackDirection, float knockbackForce) {    this.amount = amount;
        this.source = source;
        this.hitPoint = hitPoint;
        this.knockbackDirection = knockbackDirection.normalized; // o simplemente = knockbackDirection;
        this.knockbackForce = knockbackForce;
         }
}
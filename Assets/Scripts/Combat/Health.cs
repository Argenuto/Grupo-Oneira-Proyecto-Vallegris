using UnityEngine;
using UnityEngine.Events;
   
public abstract class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
   [Header("Eventos")]
    public UnityEvent<float, float> onHealthChanged = new();
    public UnityEvent<DamageInfo> onTakeDamage = new();
    public UnityEvent<float> onHeal = new();
    public UnityEvent onDeath = new();
    public UnityEvent onRevive = new();
    
     public float CurrentHealth { get; protected set; }
     public float MaxHealth => maxHealth;
      
     public bool IsDead { get ; private set; }

     protected virtual void Awake(){
        CurrentHealth = maxHealth;
        IsDead = false;
        onHealthChanged.Invoke(CurrentHealth, MaxHealth);
     }
public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead) return;
        if (!CanTakeDamage(damageInfo)) return;

        // Aplica armadura/resistencias de los hijos. Nunca negativo (no cura).
        float finalDamage = Mathf.Max(0f, ModifyDamage(damageInfo));

        // DamageInfo es readonly: se crea uno nuevo para que listeners y hooks vean el daño real.
        damageInfo = new DamageInfo(finalDamage, damageInfo.source, damageInfo.hitPoint,
                                    damageInfo.knockbackDirection, damageInfo.knockbackForce);

        CurrentHealth = Mathf.Clamp(CurrentHealth - finalDamage, 0f, MaxHealth);
        onTakeDamage.Invoke(damageInfo);
        HandleDamaged(damageInfo);
        onHealthChanged.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0f)
            Die();
    }

     public void Die()
     {
        if(IsDead) return;
        IsDead = true;
        OnDeath();
        onDeath.Invoke();
     }

public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, MaxHealth);
        onHeal.Invoke(amount);
        onHealthChanged.Invoke(CurrentHealth, MaxHealth);
    }

// Deja la vida al máximo. Funciona vivo o muerto; si estaba muerto, revive.
    public void ResetHealth()
    {
        bool wasDead = IsDead;

        CurrentHealth = MaxHealth;
        IsDead = false;

        HandleReset(); // hook para que los hijos restauren lo suyo

        if (wasDead)
            onRevive.Invoke();
        onHealthChanged.Invoke(CurrentHealth, MaxHealth);
    }

     protected virtual bool CanTakeDamage(DamageInfo damageInfo)
     {
        return true;
     }
     protected virtual void HandleDamaged(DamageInfo damageInfo)
     {
     }
protected virtual float ModifyDamage(DamageInfo damageInfo)
    {
        return damageInfo.amount;
    }
// Hook: la vida y IsDead ya fueron restaurados por ResetHealth().
    protected virtual void HandleReset()
    {
    }

     protected abstract void OnDeath();
}

using System.Collections;
using UnityEngine;

public class EnemyHealth : Health
{
    [Header("Feedback de golpe")]
    [SerializeField] private float hitFlashDuration = 0.08f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float knockbackMultiplier = 1f;

    [Header("Muerte")]
    [SerializeField] private float destroyDelay = 0.5f;
    [Tooltip("Scripts (IA, movimiento, etc.) que se desactivan al morir")]
    [SerializeField] private Behaviour[] behavioursToDisableOnDeath;

    [Header("Referencias (opcionales, se buscan solas)")]
    [SerializeField] protected SpriteRenderer spriteRenderer;

    protected Rigidbody2D rb;
    protected Collider2D[] colliders;
    protected Color originalColor = Color.white;
    protected Coroutine flashRoutine;
    protected Coroutine destroyRoutine;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody2D>();
        colliders = GetComponentsInChildren<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // Se llama dentro de TakeDamage, después de restar la vida.
    protected override void HandleDamaged(DamageInfo damageInfo)
    {
        // Knockback
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.AddForce(damageInfo.knockbackDirection * damageInfo.knockbackForce * knockbackMultiplier,
                        ForceMode2D.Impulse);
        }

        // Parpadeo
        if (spriteRenderer != null && isActiveAndEnabled)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }
    }

    // Lo llama Die() en la clase base (una sola vez).
    protected override void OnDeath()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        RestoreColor();

        // Apagar IA / movimiento
        SetBehavioursEnabled(false);

        // Apagar colisiones y física para que no caiga ni estorbe
        foreach (var col in colliders)
            if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        destroyRoutine = StartCoroutine(DestroyAfterDelay());
    }

    // Lo llama ResetHealth() en la clase base.
    protected override void HandleReset()
    {
        base.HandleReset(); // restaura vida e IsDead

        if (destroyRoutine != null)
        {
            StopCoroutine(destroyRoutine);
            destroyRoutine = null;
        }

        foreach (var col in colliders)
            if (col != null) col.enabled = true;

        if (rb != null)
            rb.simulated = true;

        SetBehavioursEnabled(true);
        RestoreColor();
    }

    private IEnumerator FlashRoutine()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        RestoreColor();
        flashRoutine = null;
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    private void RestoreColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    protected virtual void SetBehavioursEnabled(bool enabled)
    {
        if (behavioursToDisableOnDeath == null) return;
        foreach (var b in behavioursToDisableOnDeath)
            if (b != null) b.enabled = enabled;
    }
}

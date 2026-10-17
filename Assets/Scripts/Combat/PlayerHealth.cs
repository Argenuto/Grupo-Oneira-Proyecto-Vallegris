using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class PlayerHealth:Health
{
    // Variables
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private float knockbackStunTime = 0.2f;
    private PlayerController playerController;
    private Rigidbody2D body;
    private Coroutine invulnerabilityCoroutine;
    private bool isInvulnerable;

    // Fases de Unity
    protected override void Awake()
    {
        base.Awake();
        playerController = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody2D>();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    // Métodos
    protected override bool CanTakeDamage(DamageInfo damageInfo)
    {
        return !isInvulnerable;
    }

    protected override void HandleDamaged(DamageInfo info)
    {
        if (invulnerabilityDuration > 0f && invulnerabilityCoroutine == null)
            invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine());

        body.linearVelocity = Vector2.zero;
        body.AddForce(info.knockbackDirection * info.knockbackForce, ForceMode2D.Impulse);
        StartCoroutine(KnockbackStun());
    }

    protected override void OnDeath()
    {
        StopInvulnerability();
        playerController.enabled = false;
        body.linearVelocity = Vector2.zero;
    }

    protected override void HandleReset()
    {
        base.HandleReset();
        StopInvulnerability();
        playerController.enabled = true;
        playerController.CanMove = true;
        playerController.SetGravityInverted(false);
    }

  
    private void StopInvulnerability()
    {
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
            invulnerabilityCoroutine = null;
        }

        isInvulnerable = false;
        SetRenderersVisible(true);
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;
        }
    }

    // Corutinas
    private IEnumerator KnockbackStun()
    {
        playerController.CanMove = false;
        yield return new WaitForSeconds(knockbackStunTime);
        playerController.CanMove = true;
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float remaining = Mathf.Max(0f, invulnerabilityDuration);
        float interval = Mathf.Max(0.01f, blinkInterval);
        bool visible = true;

        while (remaining > 0f)
        {
            visible = !visible;
            SetRenderersVisible(visible);
            float wait = Mathf.Min(interval, remaining);
            yield return new WaitForSeconds(wait);
            remaining -= wait;
        }

        SetRenderersVisible(true);
        isInvulnerable = false;
        invulnerabilityCoroutine = null;
    }
}

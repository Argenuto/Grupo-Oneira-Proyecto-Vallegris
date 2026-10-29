using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : Health
{
    [Header("Invulnerability & Feedback")]
    [SerializeField] private float invulnerabilityDuration = 1f;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private SpriteRenderer[] renderers;
    [SerializeField] private float knockbackStunTime = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hurtSFX;
    [SerializeField] private AudioClip deathClip;

    [Header("Save Settings")]
    [SerializeField] private string healthPrefsKey = "PlayerCurrentHealth";

    [Header("Regeneration")]
    [SerializeField] private float regenDelayAfterDamage = 5f; // Time before regen starts
    [SerializeField] private float regenInterval = 1f;         // Time between each heal tick
    [SerializeField] private float regenAmount = 1f;           // Amount to heal per tick

    private Animator animator;
    private PlayerController playerController;
    private Rigidbody2D body;

    private Coroutine invulnerabilityCoroutine;
    private Coroutine regenCoroutine;
    private bool isInvulnerable;

    // Fases de Unity
    protected override void Awake()
    {
        base.Awake(); // This sets CurrentHealth = MaxHealth initially
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody2D>();

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    protected virtual void Start()
    {
        // Load health from PlayerPrefs if it exists
        if (PlayerPrefs.HasKey(healthPrefsKey))
        {
            float savedHealth = PlayerPrefs.GetFloat(healthPrefsKey);

            // If the saved health is 0 or less, the player died in the last session.
            // We should respawn them with full health instead of instantly killing them.
            if (savedHealth <= 0f)
            {
                savedHealth = MaxHealth;
            }

            CurrentHealth = savedHealth;
            onHealthChanged.Invoke(CurrentHealth, MaxHealth);
            SaveHealth(); // Update the save file with the fixed health
        }

        RestartRegeneration();
    }

    // Save Helper
    private void SaveHealth()
    {
        PlayerPrefs.SetFloat(healthPrefsKey, CurrentHealth);
        PlayerPrefs.Save();
    }

    // Métodos
    protected override bool CanTakeDamage(DamageInfo damageInfo)
    {
        return !isInvulnerable;
    }

    protected override void HandleDamaged(DamageInfo info)
    {
        SaveHealth(); // Save immediately upon taking damage
        RestartRegeneration(); // Interrupt regen and restart the safe-time delay
        PlaySFX(hurtSFX);
        if (invulnerabilityDuration > 0f && invulnerabilityCoroutine == null)
            invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine());

        animator.SetTrigger("damaged");
        animator.SetBool("isHurt", true);
        body.linearVelocity = Vector2.zero;
        body.AddForce(info.knockbackDirection * info.knockbackForce, ForceMode2D.Impulse);
        StartCoroutine(KnockbackStun());
    }

    protected override void OnDeath()
    {
        // Delete the saved health so we don't spawn with 0 health next time
        PlayerPrefs.DeleteKey(healthPrefsKey);
        PlayerPrefs.Save();
        

        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
        GameManager.Instance.TogglePlayerControl(false);
        // Fix: Force Animator parameters to neutral so airborne states don't block the death animation
        animator.SetInteger("yVelocity", 0);
        animator.SetBool("isWalking", false);
        animator.SetBool("isClimbing", false);

        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource source in allAudioSources)
        {
            source.Stop();
        }

        // 2. Play the specific death clip
        if (audioSource != null && deathClip != null)
        {
            // Using PlayOneShot allows the clip to play even if the AudioSource was just stopped above
            audioSource.PlayOneShot(deathClip);
        }

        animator.SetTrigger("dead");
        StopInvulnerability();
        playerController.enabled = false;
        body.linearVelocity = Vector2.zero;
    }

    protected override void HandleReset()
    {
        // This hook runs after base.ResetHealth() has restored CurrentHealth to MaxHealth
        SaveHealth();

        StopHurt();
        StopInvulnerability();
        RestartRegeneration();

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
        StopHurt();
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

    public void StopHurt()
    {
        animator.SetBool("isHurt", false);
    }

    // Corutinas
    private IEnumerator KnockbackStun()
    {
        playerController.CanMove = false;
        yield return new WaitForSeconds(knockbackStunTime);
        playerController.CanMove = true;

        StopHurt();
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

    // Regeneration logic
    private void RestartRegeneration()
    {
        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        if (!IsDead && CurrentHealth < MaxHealth)
        {
            regenCoroutine = StartCoroutine(RegenerationRoutine());
        }
    }

    private IEnumerator RegenerationRoutine()
    {
        // 1. Wait for the designated safe time after taking damage
        yield return new WaitForSeconds(regenDelayAfterDamage);

        // 2. Loop and heal periodically using your base class method
        while (!IsDead && CurrentHealth < MaxHealth)
        {
            Heal(regenAmount); // Triggers your onHeal and onHealthChanged events automatically
            SaveHealth();
            yield return new WaitForSeconds(regenInterval);
        }


        regenCoroutine = null;
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
        }
    }
}
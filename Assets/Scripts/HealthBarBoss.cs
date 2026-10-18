using UnityEngine;
using UnityEngine.UI;

public class HealthBarBoss : MonoBehaviour
{
    [SerializeField] private BossHealth bossHealth;
    [SerializeField] private Image healthBarFill;

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            // Subscribe to the UnityEvent
            bossHealth.onHealthChanged.AddListener(UpdateHealthBar);
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            // Unsubscribe to prevent memory leaks
            bossHealth.onHealthChanged.RemoveListener(UpdateHealthBar);
        }
    }

    private void Start()
    {
        if (bossHealth != null)
        {
            // Set the initial fill amount using the public properties from your Health class
            UpdateHealthBar(bossHealth.CurrentHealth, bossHealth.MaxHealth);
        }
    }

    // This signature matches your UnityEvent<float, float>
    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (maxHealth > 0f) // Safety check to prevent division by zero
        {
            float healthPercentage = currentHealth / maxHealth;
            healthBarFill.fillAmount = healthPercentage;
        }
    }
}
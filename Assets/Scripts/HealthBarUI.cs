using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthBarFill;

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            // Subscribe to the UnityEvent
            playerHealth.onHealthChanged.AddListener(UpdateHealthBar);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            // Unsubscribe to prevent memory leaks
            playerHealth.onHealthChanged.RemoveListener(UpdateHealthBar);
        }
    }

    private void Start()
    {
        if (playerHealth != null)
        {
            // Set the initial fill amount using the public properties from your Health class
            UpdateHealthBar(playerHealth.CurrentHealth, playerHealth.MaxHealth);
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
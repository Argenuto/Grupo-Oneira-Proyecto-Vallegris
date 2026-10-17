using UnityEngine;

/// <summary>
/// Trigger que detecta la entrada del jugador en la zona de desafío y activa
/// el GravityArenaManager correspondiente.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GravityTriggerZone : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Gestor de arena a disparar (se auto-detecta si está vacío)")]
    [SerializeField] private GravityArenaManager arenaManager;

    [Tooltip("Si es verdadero, el trigger solo se dispara una vez")]
    [SerializeField] private bool triggerOnce = true;

    private Collider2D triggerCollider;
    private bool hasTriggered = false;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        if (arenaManager == null)
            arenaManager = FindAnyObjectByType<GravityArenaManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (arenaManager == null)
                arenaManager = FindAnyObjectByType<GravityArenaManager>();

            if (arenaManager != null)
            {
                arenaManager.StartArena();
            }

            if (triggerOnce)
            {
                triggerCollider.enabled = false;
            }
        }
    }

    /// <summary>
    /// Permite rearmar el trigger si la arena se reinicia por muerte del jugador.
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        if (triggerCollider != null)
            triggerCollider.enabled = true;
    }
}


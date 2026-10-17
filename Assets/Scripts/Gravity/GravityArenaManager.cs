using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestiona el encuentro de combate en gravedad invertida.
/// Activa la inversión física del jugador y el giro suave de la cámara,
/// bloquea las salidas y cuenta las bajas de enemigos requeridas para restaurar el orden.
/// </summary>
public class GravityArenaManager : MonoBehaviour
{
    public static GravityArenaManager Instance { get; private set; }

    public enum ArenaState { Inactive, Active, Completed }

    [Header("Condición de Victoria")]
    [Tooltip("Cantidad de enemigos que deben ser derrotados para volver a la gravedad normal")]
    [SerializeField] private int requiredKills = 3;

    [Header("Enemigos del Encuentro (Opcional)")]
    [Tooltip("Si se asignan, se monitorea su muerte directamente")]
    [SerializeField] private List<EnemyHealth> arenaEnemies = new List<EnemyHealth>();

    [Header("Barreras y Confinamiento")]
    [Tooltip("Puertas o barreras que se encienden durante el combate y se apagan al ganar")]
    [SerializeField] private GameObject[] barrierObjects;

    [Header("Referencias (Se auto-detectan si están vacías)")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GravityCameraController cameraController;

    [Header("Eventos")]
    public UnityEvent onArenaStarted;
    public UnityEvent<int, int> onKillCountChanged; // (killsActuales, killsRequeridas)
    public UnityEvent onArenaCompleted;

    public ArenaState State { get; private set; } = ArenaState.Inactive;
    public int CurrentKills { get; private set; } = 0;

    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        FindReferences();
    }

    private void Start()
    {
        SetBarriersActive(false);

        // Si hay un jugador con PlayerHealth, escuchar su muerte para reiniciar limpiamente la sala
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.onDeath.AddListener(OnPlayerDied);
            }
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.onDeath.RemoveListener(OnPlayerDied);
        }
    }

    private void FindReferences()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();

        if (cameraController == null)
            cameraController = FindAnyObjectByType<GravityCameraController>();
    }

    /// <summary>
    /// Inicia el desafío gravitatorio (llamado por el Trigger de la sala).
    /// </summary>
    public void StartArena()
    {
        if (State != ArenaState.Inactive) return;

        State = ArenaState.Active;
        CurrentKills = 0;

        // Activar barreras de contención
        SetBarriersActive(true);

        // Invertir jugador y cámara
        if (player == null) FindReferences();
        if (player != null) player.SetGravityInverted(true);

        if (cameraController == null) FindReferences();
        if (cameraController != null) cameraController.SetInverted(true);

        // Suscribirse a enemigos asignados
        foreach (var enemy in arenaEnemies)
        {
            if (enemy != null)
            {
                enemy.onDeath.AddListener(() => RegisterKill(enemy));
            }
        }

        onArenaStarted?.Invoke();
        onKillCountChanged?.Invoke(CurrentKills, requiredKills);
    }

    /// <summary>
    /// Registra la muerte de un enemigo en la arena.
    /// </summary>
    public void RegisterKill(EnemyHealth enemy = null)
    {
        if (State != ArenaState.Active) return;

        CurrentKills++;
        onKillCountChanged?.Invoke(CurrentKills, requiredKills);

        if (CurrentKills >= requiredKills)
        {
            CompleteArena();
        }
    }

    /// <summary>
    /// Concluye el desafío y restaura el mundo a la normalidad.
    /// </summary>
    public void CompleteArena()
    {
        if (State != ArenaState.Active) return;
        State = ArenaState.Completed;

        // Restaurar gravedad del jugador
        if (player != null)
        {
            player.SetGravityInverted(false);
        }

        // Regresar la cámara a 0°
        if (cameraController != null)
        {
            cameraController.SetInverted(false);
        }

        // Liberar salidas
        SetBarriersActive(false);

        onArenaCompleted?.Invoke();
    }

    /// <summary>
    /// Reinicia la sala si el jugador es derrotado antes de completar el reto.
    /// </summary>
    public void ResetArena()
    {
        if (State == ArenaState.Completed) return;

        State = ArenaState.Inactive;
        CurrentKills = 0;

        if (player != null)
            player.SetGravityInverted(false);

        if (cameraController != null)
            cameraController.SetInverted(false, instant: true);

        SetBarriersActive(false);
    }

    private void OnPlayerDied()
    {
        if (State == ArenaState.Active)
        {
            ResetArena();
        }
    }

    private void SetBarriersActive(bool active)
    {
        if (barrierObjects == null) return;
        foreach (var barrier in barrierObjects)
        {
            if (barrier != null)
                barrier.SetActive(active);
        }
    }
}


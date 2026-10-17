using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestiona el encuentro de combate en gravedad invertida.
/// Activa la inversión física del jugador y el giro suave de la cámara,
/// hace APARECER a los fantasmas cerca del nuevo suelo (el techo),
/// bloquea las salidas y cuenta las bajas requeridas para restaurar el orden.
/// </summary>
public class GravityArenaManager : MonoBehaviour
{
    public static GravityArenaManager Instance { get; private set; }

    public enum ArenaState { Inactive, Active, Completed }

    [Header("Condición de Victoria")]
    [Tooltip("Cantidad de enemigos que deben ser derrotados (0 = total de fantasmas activados)")]
    [SerializeField] private int requiredKills = 2;

    [Header("Aparición de Fantasmas (Cerca del Techo)")]
    [Tooltip("Fantasmas colocados cerca del techo en la escena. Inician apagados y APARECEN al dispararse la antigravedad.")]
    [SerializeField] private List<GameObject> ghostObjectsToActivate = new List<GameObject>();

    [Tooltip("Opcional: Prefab de fantasma a instanciar si prefieres spawn dinámico")]
    [SerializeField] private GameObject ghostPrefab;
    [Tooltip("Puntos de spawn situados cerca del techo donde aparecerán los fantasmas")]
    [SerializeField] private Transform[] ceilingSpawnPoints;

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
    private List<GameObject> spawnedGhostInstances = new List<GameObject>();

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

        // Asegurar que los fantasmas inicien ocultos/desactivados antes del trigger
        foreach (var ghost in ghostObjectsToActivate)
        {
            if (ghost != null)
                ghost.SetActive(false);
        }

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

        // 1. Activar barreras de contención
        SetBarriersActive(true);

        // 2. Invertir gravedad del jugador y rotar la cámara suavemente a 180°
        if (player == null) FindReferences();
        if (player != null) player.SetGravityInverted(true);

        if (cameraController == null) FindReferences();
        if (cameraController != null) cameraController.SetInverted(true);

        // 3. Hacer APARECER a los fantasmas cerca del techo (el nuevo suelo)
        int totalEnemies = 0;

        // A) Activar fantasmas pre-colocados
        foreach (var ghost in ghostObjectsToActivate)
        {
            if (ghost != null)
            {
                ghost.SetActive(true);
                totalEnemies++;

                EnemyHealth health = ghost.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.onDeath.AddListener(() => RegisterKill(health));
                }
            }
        }

        // B) Instanciar fantasmas por prefab si se configuraron puntos de spawn
        if (ghostPrefab != null && ceilingSpawnPoints != null)
        {
            foreach (var spawnPoint in ceilingSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    GameObject newGhost = Instantiate(ghostPrefab, spawnPoint.position, spawnPoint.rotation);
                    spawnedGhostInstances.Add(newGhost);
                    totalEnemies++;

                    EnemyHealth health = newGhost.GetComponent<EnemyHealth>();
                    if (health != null)
                    {
                        health.onDeath.AddListener(() => RegisterKill(health));
                    }
                }
            }
        }

        // Si requiredKills es 0 o mayor a los enemigos disponibles, ajustar al total real
        if (requiredKills <= 0 || requiredKills > totalEnemies)
        {
            requiredKills = Mathf.Max(1, totalEnemies);
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

        // Desactivar fantasmas de nuevo
        foreach (var ghost in ghostObjectsToActivate)
        {
            if (ghost != null)
                ghost.SetActive(false);
        }

        // Destruir instancias dinámicas
        foreach (var ghost in spawnedGhostInstances)
        {
            if (ghost != null)
                Destroy(ghost);
        }
        spawnedGhostInstances.Clear();

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

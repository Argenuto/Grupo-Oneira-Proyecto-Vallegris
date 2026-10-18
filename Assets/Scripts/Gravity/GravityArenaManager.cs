using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Gestiona el encuentro de combate en gravedad invertida.
/// </summary>
public class GravityArenaManager : MonoBehaviour
{
    public static GravityArenaManager Instance { get; private set; }

    public enum ArenaState { Inactive, Active, Completed }

    [Header("Condición de Victoria")]
    [Tooltip("Cantidad de enemigos a derrotar (0 = todos los de la lista)")]
    [SerializeField] private int requiredKills = 2;

    [Header("Aparición de Fantasmas (Cerca del Techo)")]
    [Tooltip("Fantasmas colocados en la escena. Inician ocultos y APARECEN al activarse el trigger.")]
    [SerializeField] private List<GameObject> ghostObjectsToActivate = new List<GameObject>();

    [Tooltip("Prefab opcional para spawn dinámico")]
    [SerializeField] private GameObject ghostPrefab;
    [Tooltip("Puntos de spawn cerca del techo")]
    [SerializeField] private Transform[] ceilingSpawnPoints;

    [Header("Barreras y Confinamiento")]
    [SerializeField] private GameObject[] barrierObjects;

    [Header("Referencias (Se auto-detectan si están vacías)")]
    [SerializeField] private PlayerController player;
    [SerializeField] private GravityCameraController cameraController;

    [Header("Eventos")]
    public UnityEvent onArenaStarted;
    public UnityEvent<int, int> onKillCountChanged;
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
    }

    private void Start()
    {
        FindReferences();
        SetBarriersActive(false);

        // Ocultar fantasmas al inicio
        foreach (var ghost in ghostObjectsToActivate)
        {
            if (ghost != null)
                ghost.SetActive(false);
        }

        // Escuchar la muerte del jugador
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.onDeath.AddListener(OnPlayerDied);
            else
                Debug.LogWarning("[GravityArenaManager] No se encontró PlayerHealth en el jugador.");
        }
        else
        {
            Debug.LogWarning("[GravityArenaManager] No se encontró referencia al PlayerController.");
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.onDeath.RemoveListener(OnPlayerDied);
    }

    private void FindReferences()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerController>();

        if (cameraController == null)
            cameraController = FindAnyObjectByType<GravityCameraController>();
    }

    /// <summary>
    /// Inicia el desafío gravitatorio (llamado por el Trigger).
    /// </summary>
    public void StartArena()
    {
        if (State != ArenaState.Inactive)
        {
            Debug.Log($"[GravityArenaManager] StartArena() ignorado, estado actual: {State}");
            return;
        }

        Debug.Log("[GravityArenaManager] ¡Arena INICIADA!");
        State = ArenaState.Active;
        CurrentKills = 0;

        SetBarriersActive(true);

        // Resolver referencias si faltan
        FindReferences();

        if (player != null)
        {
            player.SetGravityInverted(true);
            Debug.Log("[GravityArenaManager] Gravedad invertida en el jugador.");
        }
        else
        {
            Debug.LogError("[GravityArenaManager] ¡player es NULL! No se puede invertir la gravedad.");
        }

        if (cameraController != null)
        {
            cameraController.SetInverted(true);
            Debug.Log("[GravityArenaManager] Cámara comenzando rotación a 180°.");
        }
        else
        {
            Debug.LogError("[GravityArenaManager] ¡cameraController es NULL!");
        }

        // Activar y suscribir fantasmas
        int totalEnemies = ActivateGhostsAndSubscribe();

        // Ajustar requiredKills al total real si es necesario
        if (requiredKills <= 0 || requiredKills > totalEnemies)
            requiredKills = Mathf.Max(1, totalEnemies);

        Debug.Log($"[GravityArenaManager] Fantasmas activados: {totalEnemies}. Bajas requeridas: {requiredKills}");

        onArenaStarted?.Invoke();
        onKillCountChanged?.Invoke(CurrentKills, requiredKills);
    }

    private int ActivateGhostsAndSubscribe()
    {
        int total = 0;

        // A) Fantasmas pre-colocados en la escena
        foreach (var ghost in ghostObjectsToActivate)
        {
            if (ghost == null) continue;

            ghost.SetActive(true);
            total++;

            // Captura local para evitar problemas de closure en el bucle
            EnemyHealth capturedHealth = ghost.GetComponent<EnemyHealth>();
            if (capturedHealth != null)
            {
                // RemoveListener primero para evitar duplicados si la arena se reinicia
                capturedHealth.onDeath.RemoveAllListeners();
                capturedHealth.onDeath.AddListener(() =>
                {
                    Debug.Log($"[GravityArenaManager] Fantasma eliminado: {capturedHealth.gameObject.name}");
                    RegisterKill();
                });
            }
            else
            {
                Debug.LogWarning($"[GravityArenaManager] {ghost.name} no tiene EnemyHealth. No contará como baja.");
            }
        }

        // B) Spawn dinámico por prefab
        if (ghostPrefab != null && ceilingSpawnPoints != null)
        {
            foreach (var sp in ceilingSpawnPoints)
            {
                if (sp == null) continue;

                GameObject newGhost = Instantiate(ghostPrefab, sp.position, sp.rotation);
                spawnedGhostInstances.Add(newGhost);
                total++;

                EnemyHealth capturedHealth = newGhost.GetComponent<EnemyHealth>();
                if (capturedHealth != null)
                {
                    capturedHealth.onDeath.AddListener(() =>
                    {
                        Debug.Log($"[GravityArenaManager] Fantasma (instancia) eliminado.");
                        RegisterKill();
                    });
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Registra una baja. Se llama desde el listener de onDeath de cada fantasma.
    /// </summary>
    public void RegisterKill()
    {
        if (State != ArenaState.Active)
        {
            Debug.Log($"[GravityArenaManager] RegisterKill() ignorado, estado: {State}");
            return;
        }

        CurrentKills++;
        Debug.Log($"[GravityArenaManager] Baja registrada: {CurrentKills}/{requiredKills}");
        onKillCountChanged?.Invoke(CurrentKills, requiredKills);

        if (CurrentKills >= requiredKills)
            CompleteArena();
    }

    /// <summary>
    /// Concluye el desafío y restaura la normalidad.
    /// </summary>
    public void CompleteArena()
    {
        if (State != ArenaState.Active)
        {
            Debug.Log($"[GravityArenaManager] CompleteArena() ignorado, estado: {State}");
            return;
        }

        Debug.Log("[GravityArenaManager] ¡Arena COMPLETADA! Restaurando gravedad y cámara...");
        State = ArenaState.Completed;

        FindReferences();

        if (player != null)
        {
            player.SetGravityInverted(false);
            Debug.Log("[GravityArenaManager] Gravedad restaurada al jugador.");
        }
        else
        {
            Debug.LogError("[GravityArenaManager] ¡player es NULL en CompleteArena! No se pudo restaurar la gravedad.");
        }

        if (cameraController != null)
        {
            cameraController.SetInverted(false);
            Debug.Log("[GravityArenaManager] Cámara restaurando a 0°.");
        }
        else
        {
            Debug.LogError("[GravityArenaManager] ¡cameraController es NULL en CompleteArena!");
        }

        SetBarriersActive(false);
        onArenaCompleted?.Invoke();
    }

    /// <summary>
    /// Reinicia la sala si el jugador muere antes de completar el reto.
    /// </summary>
    public void ResetArena()
    {
        if (State == ArenaState.Completed) return;

        Debug.Log("[GravityArenaManager] Arena REINICIADA por muerte del jugador.");
        State = ArenaState.Inactive;
        CurrentKills = 0;

        FindReferences();

        if (player != null)
            player.SetGravityInverted(false);

        if (cameraController != null)
            cameraController.SetInverted(false, instant: true);

        // Desactivar fantasmas pre-colocados
        foreach (var ghost in ghostObjectsToActivate)
        {
            if (ghost != null)
            {
                ghost.SetActive(false);
            }
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
            ResetArena();
    }

    private void SetBarriersActive(bool active)
    {
        if (barrierObjects == null) return;
        foreach (var b in barrierObjects)
            if (b != null)
                b.SetActive(active);
    }
}

using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D), typeof(BossHealth))]
public class BossController : MonoBehaviour
{
    [TabGroup("Boss", "Fases")]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    [InfoBox("Ordena de mayor a menor % de vida. La primera debe empezar en 100.")]
    public List<BossPhase> phases = new List<BossPhase>();

    [TabGroup("Boss", "Referencias"), Required] public BossArena arena;
    [TabGroup("Boss", "Referencias")] public Transform projectileSpawn;
    [TabGroup("Boss", "Referencias")] public ContactDamage contactDamage;
    [TabGroup("Boss", "Referencias")] public SpriteRenderer sprite;
    [TabGroup("Boss", "Referencias")] public Animator animator;
    [TabGroup("Boss", "Referencias")] public AudioSource audioSource;

    [TabGroup("Boss", "Opciones")] public bool autoStart = true;
    [TabGroup("Boss", "Cerebro"), Min(0f)] public float nearDistance = 2.5f;
    [TabGroup("Boss", "Cerebro"), Min(0f)] public float farDistance = 6f;

    [TabGroup("Boss", "Eventos")] public UnityEvent<int> onPhaseChanged = new UnityEvent<int>();
    [TabGroup("Boss", "Eventos")] public UnityEvent onPatternLoop = new UnityEvent();
    [TabGroup("Boss", "Eventos")] public UnityEvent onTelegraph = new UnityEvent();

    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly] public int CurrentPhaseIndex { get; private set; } = -1;
    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly] public int CurrentActionIndex { get; private set; } = -1;
    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly] public bool FightActive { get; private set; }

    public BossPattern CurrentPattern =>
        CurrentPhaseIndex >= 0 && CurrentPhaseIndex < phases.Count ? phases[CurrentPhaseIndex].pattern : null;

    public float ActionElapsed => Time.time - actionStartTime;
    public float HealthPercent => health != null && health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth * 100f : 0f;
    public float PlayerIdleDuration { get; private set; }
    public float NearDistance => CurrentPhase != null ? CurrentPhase.nearDistance : nearDistance;
    public float FarDistance => CurrentPhase != null ? CurrentPhase.farDistance : farDistance;
    public Transform ProjectileSpawn => projectileSpawn != null ? projectileSpawn : transform;

    private BossPhase CurrentPhase => CurrentPhaseIndex >= 0 && CurrentPhaseIndex < phases.Count ? phases[CurrentPhaseIndex] : null;
    private BossHealth health;
    private Rigidbody2D rb;
    private BossContext ctx;
    private Coroutine runRoutine;
    private bool wantFight;
    private float actionStartTime;
    private Color baseColor = Color.white;
    private Color phaseColor = Color.white;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private readonly Dictionary<BossDecisionLeaf, int> decisionLeafIds = new Dictionary<BossDecisionLeaf, int>();
    public BossDecisionTrace Trace { get; } = new BossDecisionTrace();
    private bool loggedOutOfBounds;
    private float nextOutOfBoundsLogTime;
#endif
    private Health observedPlayerHealth;
    private PlayerController playerController;
    private float idleTimer;
    private float farPlayerTimer;
    private float nextPlayerSearchTime;
    private float adaptationUntil;
    private int playerAttackStreak;
    private object lastDecisionTarget;
    private int consecutiveDecisionCount;
    private readonly Dictionary<BossWeightedEntry, float> decisionCooldowns = new Dictionary<BossWeightedEntry, float>();

    private void Awake()
    {
        health = GetComponent<BossHealth>();
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (contactDamage == null) contactDamage = GetComponent<ContactDamage>();
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (arena == null) arena = FindAnyObjectByType<BossArena>();
        if (sprite != null) baseColor = sprite.color;
        phaseColor = baseColor;

        ctx = new BossContext
        {
            boss = this,
            rb = rb,
            sprite = sprite,
            animator = animator,
            arena = arena,
            contact = contactDamage,
            audioSource = audioSource
        };

        if (sprite != null)
            ctx.FaceDirection(sprite.flipX ? 1f : -1f);

        if (health != null)
        {
            health.onHealthChanged.AddListener(OnHealthChanged);
            health.onTakeDamage.AddListener(OnBossTakeDamage);
            health.onDeath.AddListener(OnBossDeath);
            health.onRevive.AddListener(StartFight);
        }

        ctx.SetContactDamage(false);
    }

    private void Start()
    {
        if (autoStart) StartFight();
    }

    private void OnDisable()
    {
        bool summarizeFight = FightActive;
        string reason = health != null && health.IsDead ? "bossDead" : "stopped";
        StopRoutine(reason);
        FightActive = false;
        if (summarizeFight)
            BossDebug.Summary(reason, observedPlayerHealth != null ? observedPlayerHealth.CurrentHealth : 0f);
        BindPlayer(null, null);
    }

    private void OnEnable()
    {
        if (wantFight && !FightActive && ctx != null)
            StartFight();
    }

    private void Update()
    {
        if (!FightActive || ctx == null)
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        CheckBossBounds();
#endif

        if (!ctx.HasLivingPlayer)
        {
            PlayerIdleDuration = 0f;
            farPlayerTimer = 0f;
            return;
        }

        Rigidbody2D playerBody = ctx.player.GetComponent<Rigidbody2D>();
        Vector2 velocity = playerController != null
            ? playerController.WorldVelocity
            : playerBody != null ? playerBody.linearVelocity : Vector2.zero;

        idleTimer = velocity.sqrMagnitude <= 0.04f ? idleTimer + Time.deltaTime : 0f;
        PlayerIdleDuration = idleTimer;
        float distance = Mathf.Abs(ctx.player.position.x - ctx.Position.x);
        farPlayerTimer = distance > FarDistance ? farPlayerTimer + Time.deltaTime : 0f;
    }

    [Button(ButtonSizes.Medium), HideInEditorMode]
    public void StartFight()
    {
        wantFight = true;
        if (phases == null || phases.Count == 0 || ctx == null)
            return;

        GameObject playerObject = GameObject.FindWithTag("Player");
        BindPlayer(playerObject != null ? playerObject.GetComponent<PlayerController>() : null,
                   playerObject != null ? playerObject.GetComponentInParent<Health>() : null);
        ctx.player = playerObject != null ? playerObject.transform : null;
        CurrentPhaseIndex = -1;
        FightActive = true;
        BossDebug.Begin();
        BossDebug.SetPhaseCount(phases.Count);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Trace.Clear();
        loggedOutOfBounds = false;
        nextOutOfBoundsLogTime = 0f;
#endif
        float playerHealth = observedPlayerHealth != null ? observedPlayerHealth.CurrentHealth : 0f;
        BossDebug.Log("FIGHT_START", $"bossHp={BossDebug.Format(health != null ? health.CurrentHealth : 0f)} playerHp={BossDebug.Format(playerHealth)} phases={phases.Count}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogArenaSnapshot();
#endif
        EnterPhase(ComputePhaseIndex(HealthPercent));
    }

    [Button(ButtonSizes.Medium), HideInEditorMode]
    public void StopFight()
    {
        bool summarizeFight = FightActive;
        wantFight = false;
        FightActive = false;
        StopRoutine("stopped");
        if (summarizeFight)
            BossDebug.Summary("stopped", observedPlayerHealth != null ? observedPlayerHealth.CurrentHealth : 0f);
    }

#if UNITY_EDITOR
    [Button]
    private void DumpBossDebug()
    {
        BossDebug.DumpLast(200);
    }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void LogArenaSnapshot()
    {
        if (!BossDebug.IsActive || arena == null)
            return;

        BossProjectile prefab = FindProjectilePrefabForSnapshot();
        if (prefab == null)
        {
            BossDebug.Log("ARENA_SNAPSHOT", $"arenaX=({BossDebug.Format(arena.LeftX - 2f)},{BossDebug.Format(arena.RightX + 2f)}) bossPos=({BossDebug.Format(transform.position.x)},{BossDebug.Format(transform.position.y)}) colliders=unavailable mask=unavailable");
            return;
        }

        float minX = arena.LeftX - 2f;
        float maxX = arena.RightX + 2f;
        Vector2 center = new Vector2((minX + maxX) * 0.5f, transform.position.y);
        Vector2 size = new Vector2(maxX - minX, 12f);
        LayerMask mask = prefab.ObstacleLayers;
        Collider2D[] colliders = Physics2D.OverlapBoxAll(center, size, 0f, mask);
        int logged = 0;
        foreach (Collider2D collider in colliders)
        {
            if (collider == null || logged >= 20)
                continue;

            Bounds bounds = collider.bounds;
            string name = collider.gameObject.name.Replace("\"", "\\\"");
            string layer = LayerMask.LayerToName(collider.gameObject.layer);
            BossDebug.Log("ARENA_COLLIDER", $"name=\"{name}\" layer={layer} trigger={collider.isTrigger} enabled={collider.enabled} active={collider.gameObject.activeInHierarchy} bounds=({BossDebug.Format(bounds.min.x)},{BossDebug.Format(bounds.min.y)})-({BossDebug.Format(bounds.max.x)},{BossDebug.Format(bounds.max.y)})");
            logged++;
        }

        BossDebug.Log("ARENA_SNAPSHOT", $"arenaX=({BossDebug.Format(minX)},{BossDebug.Format(maxX)}) bossPos=({BossDebug.Format(transform.position.x)},{BossDebug.Format(transform.position.y)}) colliders={colliders.Length} mask={LayerMaskName(mask)}");
    }

    private void CheckBossBounds()
    {
        if (!BossDebug.IsActive || arena == null)
            return;

        float x = ctx.Position.x;
        bool outside = x < arena.LeftX - 0.05f || x > arena.RightX + 0.05f;
        if (!outside)
        {
            loggedOutOfBounds = false;
            return;
        }

        if (loggedOutOfBounds || Time.time < nextOutOfBoundsLogTime)
            return;

        float limit = x < arena.LeftX ? arena.LeftX : arena.RightX;
        float over = x < arena.LeftX ? arena.LeftX - x : x - arena.RightX;
        BossDebug.BossOutOfBounds(x, limit, over, ctx.CurrentActionName);
        loggedOutOfBounds = true;
        nextOutOfBoundsLogTime = Time.time + 1f;
    }

    private BossProjectile FindProjectilePrefabForSnapshot()
    {
        if (phases == null)
            return null;

        HashSet<BossDecisionNode> visited = new HashSet<BossDecisionNode>();
        foreach (BossPhase phase in phases)
        {
            if (phase == null)
                continue;

            BossProjectile prefab = FindPatternProjectilePrefab(phase.pattern);
            if (prefab != null)
                return prefab;

            prefab = FindProjectilePrefab(phase.fallbackAction);
            if (prefab != null)
                return prefab;

            if (phase.decisionTree != null)
            {
                prefab = FindDecisionProjectilePrefab(phase.decisionTree.root, visited);
                if (prefab != null)
                    return prefab;
            }
        }

        return null;
    }

    private BossProjectile FindPatternProjectilePrefab(BossPattern pattern)
    {
        if (pattern == null || pattern.actions == null)
            return null;

        foreach (BossAction action in pattern.actions)
        {
            BossProjectile prefab = FindProjectilePrefab(action);
            if (prefab != null)
                return prefab;
        }

        return null;
    }

    private BossProjectile FindProjectilePrefab(BossAction action)
    {
        if (action is ProjectileBurstAction burst) return burst.projectilePrefab;
        if (action is ProjectileFanAction fan) return fan.projectilePrefab;
        if (action is ProjectilePairAction pair) return pair.projectilePrefab;
        if (action is ArenaHazardAction hazard) return hazard.hazardPrefab;
        return null;
    }

    private BossProjectile FindDecisionProjectilePrefab(BossDecisionNode node, HashSet<BossDecisionNode> visited)
    {
        if (node == null || !visited.Add(node))
            return null;

        if (node is BossDecisionBranch branch)
        {
            BossProjectile prefab = FindProjectilePrefab(branch.action);
            if (prefab != null)
                return prefab;

            prefab = FindDecisionProjectilePrefab(branch.yes, visited);
            return prefab != null ? prefab : FindDecisionProjectilePrefab(branch.no, visited);
        }

        if (node is BossDecisionLeaf leaf)
        {
            BossProjectile prefab = FindProjectilePrefab(leaf.fallbackAction);
            if (prefab != null)
                return prefab;

            prefab = FindPatternProjectilePrefab(leaf.fallbackPattern);
            if (prefab != null)
                return prefab;

            if (leaf.entries != null)
            {
                foreach (BossWeightedEntry entry in leaf.entries)
                {
                    if (entry == null)
                        continue;

                    prefab = FindProjectilePrefab(entry.action);
                    if (prefab != null)
                        return prefab;

                    if (entry.combo != null)
                    {
                        foreach (BossAction action in entry.combo)
                        {
                            prefab = FindProjectilePrefab(action);
                            if (prefab != null)
                                return prefab;
                        }
                    }

                    prefab = FindPatternProjectilePrefab(entry.pattern);
                    if (prefab != null)
                        return prefab;
                }
            }
        }

        return null;
    }

    private static string LayerMaskName(LayerMask mask)
    {
        List<string> names = new List<string>();
        for (int layer = 0; layer < 32; layer++)
        {
            if ((mask.value & (1 << layer)) == 0)
                continue;

            string name = LayerMask.LayerToName(layer);
            names.Add(string.IsNullOrEmpty(name) ? $"Layer{layer}" : name);
        }

        return names.Count == 0 ? "none" : string.Join(",", names);
    }
#endif
    private void RefreshPlayerReference()
    {
        if (ctx.player != null && ctx.player.gameObject.activeInHierarchy)
            return;
        if (Time.time < nextPlayerSearchTime)
            return;

        nextPlayerSearchTime = Time.time + 0.5f;
        GameObject playerObject = GameObject.FindWithTag("Player");
        BindPlayer(playerObject != null ? playerObject.GetComponent<PlayerController>() : null,
                   playerObject != null ? playerObject.GetComponentInParent<Health>() : null);
        ctx.player = playerObject != null ? playerObject.transform : null;
    }

    private void BindPlayer(PlayerController nextPlayer, Health nextHealth)
    {
        Health resolvedHealth = nextHealth != null ? nextHealth : nextPlayer != null ? nextPlayer.GetComponentInParent<Health>() : null;
        if (playerController == nextPlayer && observedPlayerHealth == resolvedHealth)
            return;

        if (playerController != null)
            playerController.onAttackStarted.RemoveListener(OnPlayerAttackStarted);
        if (observedPlayerHealth != null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            observedPlayerHealth.onTakeDamage.RemoveListener(OnPlayerTakeDamage);
            observedPlayerHealth.onDeath.RemoveListener(OnPlayerDeath);
#endif
        }

        playerController = nextPlayer;
        observedPlayerHealth = resolvedHealth;
        if (playerController != null)
            playerController.onAttackStarted.AddListener(OnPlayerAttackStarted);
        if (observedPlayerHealth != null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            observedPlayerHealth.onTakeDamage.AddListener(OnPlayerTakeDamage);
            observedPlayerHealth.onDeath.AddListener(OnPlayerDeath);
#endif
        }

        if (ctx != null)
        {
            ctx.playerController = playerController;
            ctx.player = playerController != null ? playerController.transform : null;
        }
    }

    private void OnPlayerAttackStarted()
    {
        if (!FightActive)
            return;

        playerAttackStreak = Mathf.Min(playerAttackStreak + 1, 3);
        if (playerAttackStreak >= 3)
            adaptationUntil = Time.time + 8f;
        BossDebug.PlayerAttack(playerAttackStreak, Time.time < adaptationUntil);
    }

    private void OnBossTakeDamage(DamageInfo damageInfo)
    {
        playerAttackStreak = 0;
        adaptationUntil = 0f;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnPlayerTakeDamage(DamageInfo damageInfo)
    {
        BossDebug.PlayerDamage(damageInfo, observedPlayerHealth.CurrentHealth, observedPlayerHealth.MaxHealth,
                               health != null ? health.CurrentHealth : 0f);
    }

    private void OnPlayerDeath()
    {
        BossDebug.Summary("playerDead", observedPlayerHealth != null ? observedPlayerHealth.CurrentHealth : 0f);
    }
#endif

    private void OnBossDeath()
    {
        bool summarizeFight = FightActive;
        wantFight = false;
        FightActive = false;
        StopRoutine("stopped");
        if (summarizeFight)
            BossDebug.Summary("bossDead", observedPlayerHealth != null ? observedPlayerHealth.CurrentHealth : 0f);
    }


    private int ComputePhaseIndex(float percent)
    {
        int index = 0;
        for (int i = 0; i < phases.Count; i++)
        {
            if (percent <= phases[i].startsAtHealthPercent)
                index = i;
        }
        return index;
    }

    private void OnHealthChanged(float current, float max)
    {
        if (!FightActive || current <= 0f || max <= 0f || health == null)
            return;

        int target = ComputePhaseIndex(current / max * 100f);
        if (target > CurrentPhaseIndex)
            EnterPhase(target);
    }

    private void EnterPhase(int index)
    {
        if (index < 0 || index >= phases.Count)
            return;

        StopRoutine();
        CurrentPhaseIndex = index;
        BossPhase phase = phases[index];
        ResetDecisionState();
        BossProjectile.ClearAll();
        ctx.speedMultiplier = phase.speedMultiplier;
        ctx.minimumTelegraphDuration = phase.minimumTelegraphDuration;
        BossDebug.Phase(index, phase.name, HealthPercent,
                        observedPlayerHealth != null ? observedPlayerHealth.CurrentHealth : 0f,
                        phase.speedMultiplier, phase.reactionDelay, phase.minimumPlayerWindow);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (index == 1)
            LogArenaSnapshot();
#endif
        phaseColor = phase.phaseColor;
        if (sprite != null) sprite.color = phaseColor;
        PlaySFX(phase.transitionClip);
        if (phase.onEnter != null) phase.onEnter.Invoke();
        if (onPhaseChanged != null) onPhaseChanged.Invoke(index);
        runRoutine = StartCoroutine(PhaseRoutine(phase));
    }

    private IEnumerator PhaseRoutine(BossPhase phase)
    {
        if (phase == null)
            yield break;

        if (phase.decisionTree != null)
        {
            yield return DecisionRoutine(phase);
            yield break;
        }

        if (phase.transitionInvulnerableTime > 0f && health != null)
        {
            health.Invulnerable = true;
            yield return new WaitForSeconds(phase.transitionInvulnerableTime);
            health.Invulnerable = false;
        }

        BossPattern pattern = phase.pattern;
        if (pattern == null || pattern.actions == null || pattern.actions.Count == 0)
            yield break;

        do
        {
            for (int i = 0; i < pattern.actions.Count; i++)
            {
                BossAction action = pattern.actions[i];
                if (action == null || !action.enabled) continue;

                CurrentActionIndex = i;
                actionStartTime = Time.time;
                yield return action.Execute(ctx);
            }

            CurrentActionIndex = -1;
            if (onPatternLoop != null)
                onPatternLoop.Invoke();

            if (pattern.delayBetweenLoops > 0f)
                yield return new WaitForSeconds(pattern.delayBetweenLoops / Mathf.Max(0.1f, ctx.speedMultiplier));
            else
                yield return null;
        }
        while (pattern.loopMode == BossPattern.LoopMode.Loop && FightActive && CurrentPhase == phase);
    }

    private IEnumerator DecisionRoutine(BossPhase phase)
    {
        if (phase.transitionInvulnerableTime > 0f && health != null)
        {
            health.Invulnerable = true;
            yield return new WaitForSeconds(phase.transitionInvulnerableTime);
            health.Invulnerable = false;
        }

        while (FightActive && CurrentPhase == phase)
        {
            float reactionTime = phase.reactionDelay / Mathf.Max(0.1f, ctx.speedMultiplier);
            yield return new WaitForSeconds(Mathf.Max(phase.minimumPlayerWindow, reactionTime));

            if (!FightActive || CurrentPhase != phase)
                yield break;

            RefreshPlayerReference();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            List<int> pathIndices = new List<int>();
            BossDecisionLeaf leaf = phase.decisionTree.EvaluateWithTrace(this, ctx, pathIndices);
            List<BossCandidateRecord> candidateRecords = new List<BossCandidateRecord>();
            BossWeightedEntry entry = SelectDecision(leaf, out List<string> candidates, out List<string> excluded, candidateRecords);

            int chosenEntryIndex = -1;
            if (entry != null && leaf != null && leaf.entries != null)
                chosenEntryIndex = leaf.entries.IndexOf(entry);

            string chosenChoice = DecisionChoice(entry, leaf, phase);
            bool isFallback = chosenChoice.StartsWith("FALLBACK_", System.StringComparison.Ordinal);
            int leafIdx = phase.decisionTree != null ? phase.decisionTree.IndexOf(leaf) : -1;

            BossDecisionRecord record = new BossDecisionRecord
            {
                Time = BossDebug.IsActive ? Time.time - BossDebug.FightStartTime : Time.time,
                PhaseIndex = CurrentPhaseIndex,
                NodePath = pathIndices.ToArray(),
                LeafNodeIndex = leafIdx,
                DistanceBand = DistanceBand(),
                Distance = ctx.HasLivingPlayer ? Mathf.Abs(ctx.player.position.x - ctx.Position.x) : -1f,
                Adaptive = Time.time < adaptationUntil,
                ChosenAction = chosenChoice,
                ChosenEntryIndex = chosenEntryIndex,
                UsedFallback = isFallback,
                Candidates = candidateRecords
            };
            Trace.Add(record);

            if (BossDebug.IsActive)
            {
                string pathStr = string.Join(">", pathIndices);
                string traceSuffix = $"path={pathStr} leafIdx={leafIdx} entry={chosenEntryIndex}";
                BossDebug.Decision(CurrentPhaseIndex, GetDecisionLeafId(leaf), DistanceBand(),
                                   ctx.HasLivingPlayer ? Mathf.Abs(ctx.player.position.x - ctx.Position.x) : -1f,
                                   candidates, excluded, chosenChoice, Time.time < adaptationUntil, traceSuffix);
            }
#else
            BossDecisionLeaf leaf = phase.decisionTree.Evaluate(this, ctx);
            BossWeightedEntry entry = SelectDecision(leaf, out List<string> candidates, out List<string> excluded);
#endif

            if (entry != null && entry.combo != null && entry.combo.Count > 0)
                yield return ExecuteCombo(entry.combo, phase.maxComboActions, phase.minimumPlayerWindow);
            else if (entry != null && entry.action != null)
            {
                CurrentActionIndex = 0;
                actionStartTime = Time.time;
                yield return entry.action.Execute(ctx);
            }
            else
            {
                BossPattern pattern = entry != null
                    ? entry.pattern
                    : leaf != null && leaf.fallbackPattern != null
                        ? leaf.fallbackPattern
                        : phase.pattern;

                if (pattern != null)
                    yield return ExecutePatternIteration(pattern);
                else
                {
                    BossAction fallback = leaf != null && leaf.fallbackAction != null
                        ? leaf.fallbackAction
                        : phase.fallbackAction;
                    if (fallback != null)
                    {
                        CurrentActionIndex = 0;
                        actionStartTime = Time.time;
                        yield return fallback.Execute(ctx);
                    }
                    else
                        yield return null;
                }
            }

            CurrentActionIndex = -1;
            if (onPatternLoop != null)
                onPatternLoop.Invoke();
        }
    }

    private IEnumerator ExecutePatternIteration(BossPattern pattern)
    {
        if (pattern == null || pattern.actions == null)
            yield break;

        for (int i = 0; i < pattern.actions.Count; i++)
        {
            BossAction action = pattern.actions[i];
            if (action == null || !action.enabled)
                continue;

            CurrentActionIndex = i;
            actionStartTime = Time.time;
            yield return action.Execute(ctx);
        }
    }

    private IEnumerator ExecuteCombo(List<BossAction> combo, int maximumActions, float minimumPlayerWindow)
    {
        int count = Mathf.Min(combo.Count, Mathf.Clamp(maximumActions, 1, 3));
        for (int i = 0; i < count; i++)
        {
            BossAction action = combo[i];
            if (action == null || !action.enabled)
                continue;

            CurrentActionIndex = i;
            actionStartTime = Time.time;
            yield return action.Execute(ctx);

            if (!FightActive)
                yield break;

            if (i < count - 1)
            {
                float scaledRecovery = action.recoveryDuration / Mathf.Max(0.1f, ctx.speedMultiplier);
                float remainingWindow = Mathf.Max(0f, minimumPlayerWindow - scaledRecovery);
                if (remainingWindow > 0f)
                    yield return new WaitForSeconds(remainingWindow);
            }
        }
    }

    private BossWeightedEntry SelectDecision(BossDecisionLeaf leaf, out List<string> candidateDescriptions, out List<string> excludedDescriptions, List<BossCandidateRecord> candidateRecords = null)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        candidateDescriptions = BossDebug.IsActive ? new List<string>() : null;
        excludedDescriptions = BossDebug.IsActive ? new List<string>() : null;
#else
        candidateDescriptions = null;
        excludedDescriptions = null;
#endif
        if (leaf == null || leaf.entries == null)
            return null;

        List<BossWeightedEntry> candidates = new List<BossWeightedEntry>();
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        for (int entryIdx = 0; entryIdx < leaf.entries.Count; entryIdx++)
        {
            BossWeightedEntry entry = leaf.entries[entryIdx];
            bool hasCombo = entry != null && entry.combo != null && entry.combo.Count > 0;
            string entryName = entry != null ? DecisionEntryName(entry) : "invalid";
            float baseW = entry != null ? entry.weight : 0f;

            if (entry == null || (entry.action == null && entry.pattern == null && !hasCombo))
            {
                if (excludedDescriptions != null)
                    excludedDescriptions.Add("invalid:invalid");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (candidateRecords != null)
                    candidateRecords.Add(new BossCandidateRecord { EntryIndex = entryIdx, ActionName = entryName, BaseWeight = baseW, FinalWeight = 0f, Excluded = true, Reason = "invalid" });
#endif
                continue;
            }
            if (entry.weight <= 0f)
            {
                if (excludedDescriptions != null)
                    excludedDescriptions.Add($"{entryName}:weight");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (candidateRecords != null)
                    candidateRecords.Add(new BossCandidateRecord { EntryIndex = entryIdx, ActionName = entryName, BaseWeight = baseW, FinalWeight = 0f, Excluded = true, Reason = "weight" });
#endif
                continue;
            }
            int minimumProjectilesRequired = MinimumProjectilesRequired(entry);
            if (minimumProjectilesRequired > 0 && !BossProjectile.CanSpawn(minimumProjectilesRequired))
            {
                if (excludedDescriptions != null)
                    excludedDescriptions.Add($"{entryName}:tope");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (candidateRecords != null)
                    candidateRecords.Add(new BossCandidateRecord { EntryIndex = entryIdx, ActionName = entryName, BaseWeight = baseW, FinalWeight = 0f, Excluded = true, Reason = "tope" });
#endif
                continue;
            }
            if (ConflictsWithActiveBlindOrProjectiles(entry))
            {
                if (excludedDescriptions != null)
                    excludedDescriptions.Add($"{entryName}:conflicto");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (candidateRecords != null)
                    candidateRecords.Add(new BossCandidateRecord { EntryIndex = entryIdx, ActionName = entryName, BaseWeight = baseW, FinalWeight = 0f, Excluded = true, Reason = "conflicto" });
#endif
                continue;
            }
            if (decisionCooldowns.TryGetValue(entry, out float readyAt) && Time.time < readyAt)
            {
                if (excludedDescriptions != null)
                    excludedDescriptions.Add($"{entryName}:cooldown");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (candidateRecords != null)
                    candidateRecords.Add(new BossCandidateRecord { EntryIndex = entryIdx, ActionName = entryName, BaseWeight = baseW, FinalWeight = 0f, Excluded = true, Reason = "cooldown" });
#endif
                continue;
            }
            if ((entry.action != null && !entry.action.IsReady(ctx)) ||
                (hasCombo && entry.combo.Exists(action => action == null || !action.IsReady(ctx))))
            {
                if (excludedDescriptions != null)
                    excludedDescriptions.Add($"{entryName}:noReady");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (candidateRecords != null)
                    candidateRecords.Add(new BossCandidateRecord { EntryIndex = entryIdx, ActionName = entryName, BaseWeight = baseW, FinalWeight = 0f, Excluded = true, Reason = "noReady" });
#endif
                continue;
            }
            object target = DecisionTarget(entry);
            if (ReferenceEquals(target, lastDecisionTarget) && consecutiveDecisionCount >= Mathf.Max(1, entry.maxConsecutives))
            {
                if (excludedDescriptions != null)
                    excludedDescriptions.Add($"{entryName}:repetición");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (candidateRecords != null)
                    candidateRecords.Add(new BossCandidateRecord { EntryIndex = entryIdx, ActionName = entryName, BaseWeight = baseW, FinalWeight = 0f, Excluded = true, Reason = "repetición" });
#endif
                continue;
            }

            float weight = entry.weight * AdaptiveWeight(entry);
            if (weight <= 0f)
            {
                if (excludedDescriptions != null)
                    excludedDescriptions.Add($"{entryName}:weight");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (candidateRecords != null)
                    candidateRecords.Add(new BossCandidateRecord { EntryIndex = entryIdx, ActionName = entryName, BaseWeight = baseW, FinalWeight = 0f, Excluded = true, Reason = "weight" });
#endif
                continue;
            }

            candidates.Add(entry);
            weights.Add(weight);
            totalWeight += weight;
            if (candidateDescriptions != null)
                candidateDescriptions.Add($"{entryName}:{BossDebug.Format(weight)}");
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (candidateRecords != null)
                candidateRecords.Add(new BossCandidateRecord { EntryIndex = entryIdx, ActionName = entryName, BaseWeight = baseW, FinalWeight = weight, Excluded = false, Reason = "" });
#endif
        }

        if (candidates.Count == 0 || totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.value * totalWeight;
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= weights[i];
            if (roll <= 0f)
            {
                BossWeightedEntry selected = candidates[i];
                decisionCooldowns[selected] = Time.time + selected.cooldown;
                object target = DecisionTarget(selected);
                consecutiveDecisionCount = ReferenceEquals(target, lastDecisionTarget) ? consecutiveDecisionCount + 1 : 1;
                lastDecisionTarget = target;
                return selected;
            }
        }

        return null;
    }

    /// <summary>
    /// Para entradas compuestas se exige el mayor mínimo individual; cada acción revalida el cupo al ejecutarse.
    /// </summary>
    private static int MinimumProjectilesRequired(BossWeightedEntry entry)
    {
        int required = entry.action != null ? entry.action.MinimumProjectilesRequired : 0;
        if (entry.combo != null)
        {
            foreach (BossAction action in entry.combo)
            {
                if (action != null && action.enabled)
                    required = Mathf.Max(required, action.MinimumProjectilesRequired);
            }
        }

        if (entry.pattern != null && entry.pattern.actions != null)
        {
            foreach (BossAction action in entry.pattern.actions)
            {
                if (action != null && action.enabled)
                    required = Mathf.Max(required, action.MinimumProjectilesRequired);
            }
        }

        return required;
    }

    private static string DecisionEntryName(BossWeightedEntry entry)
    {
        if (entry.action != null)
            return DecisionActionName(entry.action);
        if (entry.combo != null && entry.combo.Count > 0 && entry.combo[0] != null)
            return "Combo_" + DecisionActionName(entry.combo[0]);
        if (entry.pattern != null)
            return "Pattern_" + entry.pattern.name.Replace(" ", "_");
        return "invalid";
    }

    private static string DecisionActionName(BossAction action)
    {
        string name = BossDebug.ActionName(action);
        return name.StartsWith("Projectile", System.StringComparison.Ordinal)
            ? name.Substring("Projectile".Length)
            : name;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private string DecisionChoice(BossWeightedEntry entry, BossDecisionLeaf leaf, BossPhase phase)
    {
        if (entry != null)
        {
            if (entry.combo != null && entry.combo.Count > 0)
            {
                foreach (BossAction action in entry.combo)
                    if (action != null && action.enabled) return BossDebug.ActionName(action);
            }
            if (entry.action != null)
                return BossDebug.ActionName(entry.action);
            if (entry.pattern != null)
                return FirstPatternAction(entry.pattern);
        }

        BossPattern fallbackPattern = leaf != null && leaf.fallbackPattern != null ? leaf.fallbackPattern : phase.pattern;
        if (fallbackPattern != null)
            return "FALLBACK_" + FirstPatternAction(fallbackPattern);

        BossAction fallback = leaf != null && leaf.fallbackAction != null ? leaf.fallbackAction : phase.fallbackAction;
        return fallback != null ? "FALLBACK_" + BossDebug.ActionName(fallback) : "FALLBACK_None";
    }

    private static string FirstPatternAction(BossPattern pattern)
    {
        if (pattern == null || pattern.actions == null)
            return "none";
        foreach (BossAction action in pattern.actions)
            if (action != null && action.enabled) return BossDebug.ActionName(action);
        return "none";
    }

    private string GetDecisionLeafId(BossDecisionLeaf leaf)
    {
        if (leaf == null)
            return "none";
        if (!decisionLeafIds.TryGetValue(leaf, out int id))
        {
            id = decisionLeafIds.Count + 1;
            decisionLeafIds.Add(leaf, id);
        }
        return "L" + id;
    }

    private string DistanceBand()
    {
        if (!ctx.HasLivingPlayer)
            return "None";
        float distance = Mathf.Abs(ctx.player.position.x - ctx.Position.x);
        return distance < NearDistance ? "Near" : distance <= FarDistance ? "Medium" : "Far";
    }
#endif


    private static object DecisionTarget(BossWeightedEntry entry)
    {
        if (entry.action != null)
            return entry.action;
        if (entry.pattern != null)
            return entry.pattern;
        if (entry.combo != null && entry.combo.Count > 0 && entry.combo[0] != null)
            return entry.combo[0];
        return entry;
    }

    private bool ConflictsWithActiveBlindOrProjectiles(BossWeightedEntry entry)
    {
        bool playerBlind = false;
        if (ctx.HasLivingPlayer)
        {
            PlayerStatusEffects effects = ctx.player.GetComponentInParent<PlayerStatusEffects>();
            playerBlind = effects != null && effects.IsBlind;
        }

        bool hasActiveProjectiles = BossProjectile.ActiveProjectileCount > 0;
        if (entry.action != null && ActionConflicts(entry.action, playerBlind, hasActiveProjectiles))
            return true;

        if (entry.combo != null)
        {
            foreach (BossAction action in entry.combo)
            {
                if (action != null && ActionConflicts(action, playerBlind, hasActiveProjectiles))
                    return true;
            }
        }

        if (entry.pattern != null && entry.pattern.actions != null)
        {
            foreach (BossAction action in entry.pattern.actions)
            {
                if (action != null && ActionConflicts(action, playerBlind, hasActiveProjectiles))
                    return true;
            }
        }

        return false;
    }

    private static bool ActionConflicts(BossAction action, bool playerBlind, bool hasActiveProjectiles)
    {
        bool projectileAction = action is ProjectileBurstAction || action is ProjectileFanAction ||
                                action is ProjectilePairAction || action is ArenaHazardAction;
        if (projectileAction && playerBlind)
            return true;
        if (action is BlindAction && (playerBlind || hasActiveProjectiles))
            return true;

        return false;
    }

    public bool IsActionReady(BossAction action)
    {
        if (action == null || !action.IsReady(ctx))
            return false;

        foreach (KeyValuePair<BossWeightedEntry, float> cooldown in decisionCooldowns)
        {
            if (cooldown.Key.action == action && Time.time < cooldown.Value)
                return false;
        }

        return true;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public float GetEntryCooldownRemaining(int phaseIndex, int nodeIndex, int entryIndex)
    {
        if (phases == null || phaseIndex < 0 || phaseIndex >= phases.Count)
            return 0f;

        BossPhase phase = phases[phaseIndex];
        if (phase == null || phase.decisionTree == null)
            return 0f;

        IReadOnlyList<BossDecisionNode> nodes = phase.decisionTree.GetNodesInOrder();
        if (nodeIndex < 0 || nodeIndex >= nodes.Count)
            return 0f;

        if (nodes[nodeIndex] is BossDecisionLeaf leaf && leaf.entries != null)
        {
            if (entryIndex >= 0 && entryIndex < leaf.entries.Count)
            {
                BossWeightedEntry entry = leaf.entries[entryIndex];
                if (entry != null && decisionCooldowns.TryGetValue(entry, out float readyAt))
                    return Mathf.Max(0f, readyAt - Time.time);
            }
        }

        return 0f;
    }
#endif

    private float AdaptiveWeight(BossWeightedEntry entry)
    {
        float multiplier = 1f;
        bool adapting = Time.time < adaptationUntil;

        BossAction action = entry.action;
        if (action == null && entry.combo != null && entry.combo.Count > 0)
            action = entry.combo[0];

        if (action is BackstepAction && adapting)
            multiplier *= 2f;
        else if (action is DashAction && (adapting || farPlayerTimer >= 1f))
            multiplier *= 1.5f;
        else if ((action is ProjectileBurstAction || action is ProjectileFanAction ||
                  action is ProjectilePairAction) && farPlayerTimer >= 1f)
            multiplier *= 1.5f;
        else if (action is WaitAction && adapting)
            multiplier *= 0.5f;

        return multiplier;
    }

    private void ResetDecisionState()
    {
        decisionCooldowns.Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        decisionLeafIds.Clear();
#endif
        lastDecisionTarget = null;
        consecutiveDecisionCount = 0;
        playerAttackStreak = 0;
        adaptationUntil = 0f;
        farPlayerTimer = 0f;
        idleTimer = 0f;
        PlayerIdleDuration = 0f;
    }

    public void BeginRecovery(float incomingDamageMultiplier, Color recoveryColor)
    {
        if (health != null)
            health.DamageTakenMultiplier = Mathf.Max(1f, incomingDamageMultiplier);
        if (sprite != null)
            sprite.color = recoveryColor;
    }

    public void EndRecovery()
    {
        if (health != null)
            health.DamageTakenMultiplier = 1f;
        if (sprite != null)
            sprite.color = phaseColor;
    }

    private void StopRoutine(string actionEndReason = "phaseChange")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (ctx != null)
            ctx.SetActionEndReason(actionEndReason);
#endif
        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
            runRoutine = null;
        }

        if (ctx == null) return;

        ctx.SetContactDamage(false);
        ctx.StopHorizontal();
        if (ctx.animator != null)
            ctx.animator.SetBool("Walking", false);
        EndRecovery();
        BossActionUtilities.ClearHazardMarkers();
        BossProjectile.ClearAll();
        if (sprite != null) sprite.color = FightActive ? phaseColor : baseColor;
        if (health != null) health.Invulnerable = false;
        CurrentActionIndex = -1;
    }

    public void setDeathAnimation()
    {
        if (animator != null)
            animator.SetBool("dead", true);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
        }
    }
}

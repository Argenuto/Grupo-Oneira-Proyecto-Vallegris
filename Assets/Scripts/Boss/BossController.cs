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

        if (health != null)
        {
            health.onHealthChanged.AddListener(OnHealthChanged);
            health.onTakeDamage.AddListener(OnBossTakeDamage);
            health.onDeath.AddListener(StopFight);
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
        StopRoutine();
        FightActive = false;
        BindPlayer(null);
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
        BindPlayer(playerObject != null ? playerObject.GetComponent<PlayerController>() : null);
        ctx.player = playerObject != null ? playerObject.transform : null;
        CurrentPhaseIndex = -1;
        FightActive = true;
        EnterPhase(ComputePhaseIndex(HealthPercent));
    }

    [Button(ButtonSizes.Medium), HideInEditorMode]
    public void StopFight()
    {
        wantFight = false;
        FightActive = false;
        StopRoutine();
    }

    private void RefreshPlayerReference()
    {
        if (ctx.player != null && ctx.player.gameObject.activeInHierarchy)
            return;
        if (Time.time < nextPlayerSearchTime)
            return;

        nextPlayerSearchTime = Time.time + 0.5f;
        GameObject playerObject = GameObject.FindWithTag("Player");
        BindPlayer(playerObject != null ? playerObject.GetComponent<PlayerController>() : null);
        ctx.player = playerObject != null ? playerObject.transform : null;
    }

    private void BindPlayer(PlayerController nextPlayer)
    {
        if (playerController == nextPlayer)
            return;

        if (playerController != null)
            playerController.onAttackStarted.RemoveListener(OnPlayerAttackStarted);

        playerController = nextPlayer;
        if (playerController != null)
            playerController.onAttackStarted.AddListener(OnPlayerAttackStarted);

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
    }

    private void OnBossTakeDamage(DamageInfo damageInfo)
    {
        playerAttackStreak = 0;
        adaptationUntil = 0f;
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
            BossDecisionLeaf leaf = phase.decisionTree.Evaluate(this, ctx);
            BossWeightedEntry entry = SelectDecision(leaf);

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

    private BossWeightedEntry SelectDecision(BossDecisionLeaf leaf)
    {
        if (leaf == null || leaf.entries == null)
            return null;

        List<BossWeightedEntry> candidates = new List<BossWeightedEntry>();
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        foreach (BossWeightedEntry entry in leaf.entries)
        {
            bool hasCombo = entry != null && entry.combo != null && entry.combo.Count > 0;
            if (entry == null || (entry.action == null && entry.pattern == null && !hasCombo) || entry.weight <= 0f)
                continue;
            if (ConflictsWithActiveBlindOrProjectiles(entry))
                continue;
            if (decisionCooldowns.TryGetValue(entry, out float readyAt) && Time.time < readyAt)
                continue;
            if (entry.action != null && !entry.action.IsReady(ctx))
                continue;
            if (hasCombo && entry.combo.Exists(action => action == null || !action.IsReady(ctx)))
                continue;
            object target = DecisionTarget(entry);
            if (ReferenceEquals(target, lastDecisionTarget) && consecutiveDecisionCount >= Mathf.Max(1, entry.maxConsecutives))
                continue;

            float weight = entry.weight * AdaptiveWeight(entry);
            if (weight <= 0f)
                continue;

            candidates.Add(entry);
            weights.Add(weight);
            totalWeight += weight;
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

    private void StopRoutine()
    {
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

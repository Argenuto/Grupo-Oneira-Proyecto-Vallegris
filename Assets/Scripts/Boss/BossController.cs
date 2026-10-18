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

    [TabGroup("Boss", "Opciones")] public bool autoStart = true;

    [TabGroup("Boss", "Eventos")] public UnityEvent<int> onPhaseChanged = new UnityEvent<int>();
    [TabGroup("Boss", "Eventos")] public UnityEvent onPatternLoop = new UnityEvent();
    [TabGroup("Boss", "Eventos")] public UnityEvent onTelegraph = new UnityEvent();

    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly] public int CurrentPhaseIndex { get; private set; } = -1;
    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly] public int CurrentActionIndex { get; private set; } = -1;
    [FoldoutGroup("Debug"), ShowInInspector, ReadOnly] public bool FightActive { get; private set; }

    public BossPattern CurrentPattern =>
        CurrentPhaseIndex >= 0 && CurrentPhaseIndex < phases.Count ? phases[CurrentPhaseIndex].pattern : null;

    public float ActionElapsed => Time.time - actionStartTime;
    public Transform ProjectileSpawn => projectileSpawn != null ? projectileSpawn : transform;

    private BossHealth health;
    private Rigidbody2D rb;
    private BossContext ctx;
    private Coroutine runRoutine;
    private bool wantFight;
    private float actionStartTime;
    private Color baseColor = Color.white;

    private void Awake()
    {
        health = GetComponent<BossHealth>();
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (contactDamage == null) contactDamage = GetComponent<ContactDamage>();
        if (sprite != null) baseColor = sprite.color;

        ctx = new BossContext
        {
            boss = this,
            rb = rb,
            sprite = sprite,
            animator = animator,
            arena = arena,
            contact = contactDamage
        };

        if (health != null)
        {
            health.onHealthChanged.AddListener(OnHealthChanged);
            health.onDeath.AddListener(StopFight);
            health.onRevive.AddListener(StartFight);
        }
    }

    private void Start() { if (autoStart) StartFight(); }

    private void OnDisable()
    {
        StopRoutine();
        FightActive = false;
    }

    private void OnEnable()
    {
        if (wantFight && !FightActive && ctx != null)
            StartFight();
    }

    [Button(ButtonSizes.Medium), HideInEditorMode]
    public void StartFight()
    {
        wantFight = true;
        if (phases.Count == 0) return;
        ctx.player = GameObject.FindWithTag("Player")?.transform;
        CurrentPhaseIndex = -1;
        FightActive = true;
        EnterPhase(ComputePhaseIndex(HealthPercent()));
    }

    [Button(ButtonSizes.Medium), HideInEditorMode]
    public void StopFight()
    {
        wantFight = false;
        FightActive = false;
        StopRoutine();
    }

    private float HealthPercent() => health != null && health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth * 100f : 0f;

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
        if (!FightActive || max <= 0f || health == null)
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
        ctx.speedMultiplier = phase.speedMultiplier;
        phase.onEnter.Invoke();
        onPhaseChanged.Invoke(index);
        runRoutine = StartCoroutine(PhaseRoutine(phase));
    }

    private IEnumerator PhaseRoutine(BossPhase phase)
    {
        if (phase == null)
            yield break;

        if (phase.transitionInvulnerableTime > 0f && health != null)
        {
            health.Invulnerable = true;
            yield return new WaitForSeconds(phase.transitionInvulnerableTime);
            health.Invulnerable = false;
        }

        BossPattern pattern = phase.pattern;
        if (pattern == null || pattern.actions.Count == 0)
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
            onPatternLoop.Invoke();

            if (pattern.delayBetweenLoops > 0f)
                yield return new WaitForSeconds(pattern.delayBetweenLoops / Mathf.Max(0.1f, ctx.speedMultiplier));
            else
                yield return null;
        }
        while (pattern.loopMode == BossPattern.LoopMode.Loop);
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
        if (sprite != null) sprite.color = baseColor;
        if (health != null) health.Invulnerable = false;
        CurrentActionIndex = -1;
    }

    public void setDeathAnimation()
    {
        animator.SetBool("dead", true);
    }
}

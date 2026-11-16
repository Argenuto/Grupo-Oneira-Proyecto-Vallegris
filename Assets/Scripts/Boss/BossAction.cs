using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Bloque base de un patrón. Cada acción concreta hereda de aquí.
/// </summary>
[Serializable]
public abstract class BossAction
{
    [FoldoutGroup("Común", false)] public bool enabled = true;
    [FoldoutGroup("Común"), Min(0f), Tooltip("Aviso antes de ejecutar la acción")]
    public float telegraphDuration = 0f;
    [FoldoutGroup("Común")] public Color telegraphColor = new Color(1f, 0.85f, 0.2f);
    [FoldoutGroup("Común"), Min(0f)] public float recoveryDuration;
    [FoldoutGroup("Común")] public Color recoveryColor = new Color(0.55f, 0.55f, 0.55f);
    [FoldoutGroup("Común"), Min(1f)] public float recoveryDamageMultiplier = 1f;
    [FoldoutGroup("Común")] public UnityEvent onStart = new UnityEvent();
    [FoldoutGroup("Común")] public UnityEvent onEnd = new UnityEvent();

    public abstract string Title { get; }

    protected abstract float BodyDuration { get; }

    public virtual Color TimelineColor => new Color(0.35f, 0.55f, 0.9f);

    /// <summary>
    /// Si es true, el jefe se orienta hacia el jugador durante el aviso y al ejecutar.
    /// Las acciones de movimiento lo desactivan porque miran hacia donde caminan.
    /// </summary>
    protected virtual bool FacesPlayer => true;

    public float EstimatedDuration => telegraphDuration + BodyDuration + recoveryDuration;

    /// <summary>
    /// Mínimo de proyectiles libres necesario para ejecutar esta acción.
    /// </summary>
    public virtual int MinimumProjectilesRequired => 0;

    public virtual bool IsReady(BossContext ctx) => true;

    protected abstract IEnumerator Run(BossContext ctx);

    public IEnumerator Execute(BossContext ctx)
    {
        if (!HasProjectileCapacity())
        {
            BossDebug.ProjectileSkipped(this, BossProjectile.ActiveProjectileCount);
            yield break;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float startTime = Time.time;
        int startPhase = ctx != null && ctx.boss != null ? ctx.boss.CurrentPhaseIndex : -1;
        if (ctx != null)
            ctx.BeginAction(this);
        BossDebug.ActionStarted(this, ctx);
#endif
        onStart.Invoke();

        Color original = ctx != null && ctx.sprite != null ? ctx.sprite.color : Color.white;
        bool hasOriginalColor = ctx != null && ctx.sprite != null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool completed = false;
#endif

        try
        {
            if (ctx != null && FacesPlayer)
                ctx.FacePlayer();

            if (telegraphDuration > 0f)
            {
                if (ctx != null && ctx.boss != null)
                    ctx.boss.onTelegraph.Invoke();

                if (hasOriginalColor)
                    ctx.sprite.color = telegraphColor;

                float scaledTelegraph = telegraphDuration / Mathf.Max(0.1f, ctx != null ? ctx.speedMultiplier : 1f);
                float telegraphTime = Mathf.Max(ctx != null ? ctx.minimumTelegraphDuration : 0f, scaledTelegraph);
                float elapsed = 0f;
                while (elapsed < telegraphTime)
                {
                    if (ctx != null && FacesPlayer)
                        ctx.FacePlayer();

                    yield return null;
                    elapsed += Time.deltaTime;
                }

                if (hasOriginalColor)
                    ctx.sprite.color = original;
            }

            bool hasProjectileCapacity = HasProjectileCapacity();
            if (!hasProjectileCapacity)
            {
                BossDebug.ProjectileSkipped(this, BossProjectile.ActiveProjectileCount);
            }
            else if (ctx != null && FacesPlayer)
                ctx.FacePlayer();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (hasProjectileCapacity)
                BossDebug.ActionActive(this);
#endif
            if (ctx != null && hasProjectileCapacity)
                yield return Run(ctx);

            if (hasProjectileCapacity && recoveryDuration > 0f)
            {
                if (ctx != null && ctx.boss != null)
                    ctx.boss.BeginRecovery(recoveryDamageMultiplier, recoveryColor);
                else if (hasOriginalColor)
                    ctx.sprite.color = recoveryColor;

                yield return new WaitForSeconds(recoveryDuration / Mathf.Max(0.1f, ctx != null ? ctx.speedMultiplier : 1f));
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            completed = hasProjectileCapacity;
#endif
        }
        finally
        {
            if (ctx != null && ctx.boss != null)
                ctx.boss.EndRecovery();
            if (hasOriginalColor && ctx.sprite != null)
                ctx.sprite.color = original;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string reason = ctx != null ? ctx.CurrentActionEndReason : null;
            if (!completed && string.IsNullOrEmpty(reason))
            {
                if (ctx != null && ctx.boss != null && ctx.boss.CurrentPhaseIndex != startPhase)
                    reason = "phaseChange";
                else if (ctx != null && ctx.boss != null && !ctx.boss.FightActive)
                    reason = "stopped";
                else
                    reason = "interrupted";
            }
            BossDebug.ActionEnded(this, Time.time - startTime, !completed, reason ?? string.Empty);
            if (ctx != null)
                ctx.EndAction(this);
#endif
        }

        onEnd.Invoke();
    }

    private bool HasProjectileCapacity()
    {
        return MinimumProjectilesRequired <= 0 ||
               BossProjectile.CanSpawn(MinimumProjectilesRequired);
    }

    protected static void SetWalking(BossContext ctx, bool walking)
    {
        if (ctx != null && ctx.animator != null)
            ctx.animator.SetBool("Walking", walking);
    }
}

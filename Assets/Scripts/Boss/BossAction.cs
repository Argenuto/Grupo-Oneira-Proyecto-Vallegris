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

    public float EstimatedDuration => telegraphDuration + BodyDuration + recoveryDuration;

    public virtual bool IsReady(BossContext ctx) => true;

    protected abstract IEnumerator Run(BossContext ctx);

    public IEnumerator Execute(BossContext ctx)
    {
        onStart.Invoke();

        Color original = ctx != null && ctx.sprite != null ? ctx.sprite.color : Color.white;
        bool hasOriginalColor = ctx != null && ctx.sprite != null;

        try
        {
            if (telegraphDuration > 0f)
            {
                if (ctx != null && ctx.boss != null)
                    ctx.boss.onTelegraph.Invoke();

                if (hasOriginalColor)
                    ctx.sprite.color = telegraphColor;

                float scaledTelegraph = telegraphDuration / Mathf.Max(0.1f, ctx != null ? ctx.speedMultiplier : 1f);
                float telegraphTime = Mathf.Max(ctx != null ? ctx.minimumTelegraphDuration : 0f, scaledTelegraph);
                yield return new WaitForSeconds(telegraphTime);

                if (hasOriginalColor)
                    ctx.sprite.color = original;
            }

            if (ctx != null)
                yield return Run(ctx);

            if (recoveryDuration > 0f)
            {
                if (ctx != null && ctx.boss != null)
                    ctx.boss.BeginRecovery(recoveryDamageMultiplier, recoveryColor);
                else if (hasOriginalColor)
                    ctx.sprite.color = recoveryColor;

                yield return new WaitForSeconds(recoveryDuration / Mathf.Max(0.1f, ctx != null ? ctx.speedMultiplier : 1f));
            }
        }
        finally
        {
            if (ctx != null && ctx.boss != null)
                ctx.boss.EndRecovery();
            if (hasOriginalColor && ctx.sprite != null)
                ctx.sprite.color = original;
        }

        onEnd.Invoke();
    }

    protected static void SetWalking(BossContext ctx, bool walking)
    {
        if (ctx != null && ctx.animator != null)
            ctx.animator.SetBool("Walking", walking);
    }
}

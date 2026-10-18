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
    [FoldoutGroup("Común")] public UnityEvent onStart = new UnityEvent();
    [FoldoutGroup("Común")] public UnityEvent onEnd = new UnityEvent();

    public abstract string Title { get; }

    protected abstract float BodyDuration { get; }

    public virtual Color TimelineColor => new Color(0.35f, 0.55f, 0.9f);

    public float EstimatedDuration => telegraphDuration + BodyDuration;

    protected abstract IEnumerator Run(BossContext ctx);

    public IEnumerator Execute(BossContext ctx)
    {
        onStart.Invoke();

        if (telegraphDuration > 0f)
        {
            if (ctx != null && ctx.boss != null)
                ctx.boss.onTelegraph.Invoke();

            Color original = ctx != null && ctx.sprite != null ? ctx.sprite.color : Color.white;
            if (ctx != null && ctx.sprite != null)
                ctx.sprite.color = telegraphColor;

            yield return new WaitForSeconds(telegraphDuration / Mathf.Max(0.1f, ctx != null ? ctx.speedMultiplier : 1f));

            if (ctx != null && ctx.sprite != null)
                ctx.sprite.color = original;
        }

        if (ctx != null)
            yield return Run(ctx);
        else
            yield break;

        onEnd.Invoke();
    }
}

using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class WaitAction : BossAction
{
    [Min(0f)] public float duration = 1f;

    public override string Title => $"Esperar {duration:0.#}s";
    protected override float BodyDuration => duration;
    public override Color TimelineColor => new Color(0.55f, 0.55f, 0.55f);

    protected override IEnumerator Run(BossContext ctx)
    {
        yield return new WaitForSeconds(duration / Mathf.Max(0.1f, ctx.speedMultiplier));
    }
}

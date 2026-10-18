using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class MoveToAction : BossAction
{
    public MoveTarget target = MoveTarget.ArenaRight;
    [ShowIf("target", MoveTarget.Offset)] public float offsetX = 3f;
    [Min(0.1f)] public float speed = 4f;
    [Min(0.5f)] public float timeout = 6f;

    public override string Title => $"Mover → {target}";
    protected override float BodyDuration => 2f;
    public override Color TimelineColor => new Color(0.3f, 0.7f, 0.45f);

    protected override IEnumerator Run(BossContext ctx)
    {
        ctx.animator.SetBool("Walking", true);
        float x = ctx.ResolveX(target, offsetX);
        yield return ctx.MoveToX(x, speed, timeout);
    }
}

using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class PatrolAction : BossAction
{
    [Min(1), Tooltip("Cada pasada = ir de un extremo de la arena al otro")]
    public int passes = 2;
    [Min(0.1f)] public float speed = 5f;
    [Min(0f)] public float pauseAtEdges = 0.3f;
    public bool damageOnContact = true;

    public override string Title => $"Patrullar x{passes}" + (damageOnContact ? " (daño)" : "");
    protected override float BodyDuration => passes * (3f + pauseAtEdges);
    public override Color TimelineColor => new Color(0.9f, 0.4f, 0.35f);

    protected override IEnumerator Run(BossContext ctx)
    {
        ctx.SetContactDamage(damageOnContact);

        if (ctx.arena == null)
            yield break;

        float left = ctx.arena.LeftX;
        float right = ctx.arena.RightX;
        bool goRight = Mathf.Abs(ctx.Position.x - left) < Mathf.Abs(ctx.Position.x - right);

        for (int i = 0; i < passes; i++)
        {
            yield return ctx.MoveToX(goRight ? right : left, speed);
            goRight = !goRight;
            if (pauseAtEdges > 0f)
                yield return new WaitForSeconds(pauseAtEdges / Mathf.Max(0.1f, ctx.speedMultiplier));
        }

        ctx.SetContactDamage(false);
    }
}

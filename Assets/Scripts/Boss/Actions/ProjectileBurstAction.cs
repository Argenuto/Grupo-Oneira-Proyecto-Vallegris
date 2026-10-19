using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public enum AimMode { AtPlayer, Left, Right, Up, Down, FixedAngle }

[Serializable]
public class ProjectileBurstAction : BossAction
{
    [Required, AssetsOnly] public BossProjectile projectilePrefab;
    [Min(1)] public int count = 5;
    [Range(0f, 360f)] public float spreadAngle = 50f;
    public AimMode aim = AimMode.AtPlayer;
    [ShowIf("aim", AimMode.FixedAngle)] public float fixedAngle = 0f;
    [Min(0.1f)] public float projectileSpeed = 8f;
    [Min(0f)] public float intervalBetweenShots = 0f;
    [Min(0f)] public float damageOverride = 0f;
    public AudioClip chargeClip;
    public AudioClip throwBall;
    public BlindPayload blind = new BlindPayload();

    public override string Title => $"Proyectiles x{count}" + (blind != null && blind.applyBlind ? " (ciega)" : "");
    protected override float BodyDuration => Mathf.Max(0.2f, (count - 1) * intervalBetweenShots + 0.2f);
    public override Color TimelineColor => new Color(0.65f, 0.4f, 0.85f);

    protected override IEnumerator Run(BossContext ctx)
    {
        if (projectilePrefab == null || ctx == null || ctx.boss == null)
            yield break;
            
        ctx.animator.SetTrigger("charging attack");
        PlaySFX(chargeClip, ctx);
        Vector2 origin = ctx.boss.ProjectileSpawn.position;
        float baseAngle = ResolveBaseAngle(ctx, origin);
        ctx.FaceDirection(ctx.player.position.x - origin.x);
        yield return new WaitForSeconds(0.8f);

        PlaySFX(throwBall, ctx);

        for (int i = 0; i < count; i++)
        {
            float offset = count > 1 ? Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, i / (float)(count - 1)) : 0f;
            float rad = (baseAngle + offset) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            BossProjectile projectile = UnityEngine.Object.Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectile.Init(dir, projectileSpeed, blind, damageOverride);

            if (intervalBetweenShots > 0f && i < count - 1)
                yield return new WaitForSeconds(intervalBetweenShots / Mathf.Max(0.1f, ctx.speedMultiplier));
        }

        yield return new WaitForSeconds(0.2f);
    }

    private float ResolveBaseAngle(BossContext ctx, Vector2 origin)
    {
        switch (aim)
        {
            case AimMode.Left:
                return 180f;
            case AimMode.Right:
                return 0f;
            case AimMode.Up:
                return 90f;
            case AimMode.Down:
                return -90f;
            case AimMode.FixedAngle:
                return fixedAngle;
            default:
                if (ctx.player == null) return 180f;
                Vector2 to = (Vector2)ctx.player.position - origin;
                return Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
        }
    }

    public void PlaySFX(AudioClip clip, BossContext ctx)
    {
        Debug.Log("HOLAAAA");
        if (clip != null && ctx.audioSource != null)
        {
                    Debug.Log("COLAAAAcaco");

            ctx.audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            ctx.audioSource.PlayOneShot(clip);
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class RecoveryAction : BossAction
{
    [Min(0.1f)] public float duration = 0.7f;
    [Min(1f)] public float incomingDamageMultiplier = 1f;

    public override string Title => $"Recuperar {duration:0.#}s";
    protected override float BodyDuration => duration;
    public override Color TimelineColor => new Color(0.55f, 0.55f, 0.55f);

    protected override IEnumerator Run(BossContext ctx)
    {
        if (ctx != null && ctx.boss != null)
            ctx.boss.BeginRecovery(incomingDamageMultiplier, recoveryColor);
        yield return new WaitForSeconds(duration / Mathf.Max(0.1f, ctx.speedMultiplier));
    }
}

[Serializable]
public class MeleeSlashAction : BossAction
{
    [Min(0.1f)] public float reach = 2.5f;
    [Min(0.1f)] public float verticalSize = 1.5f;
    [Min(0f)] public float damage = 15f;
    [Min(0f)] public float activeDuration = 0.15f;
    [Min(0f)] public float knockbackForce = 6f;
    public LayerMask targetLayers;

    public override string Title => "Corte cuerpo a cuerpo";
    protected override float BodyDuration => activeDuration;
    public override Color TimelineColor => new Color(0.95f, 0.28f, 0.2f);

    protected override IEnumerator Run(BossContext ctx)
    {
        if (ctx == null || ctx.boss == null)
            yield break;

        if (ctx.animator != null)
            ctx.animator.SetTrigger("charging attack");

        float direction = ctx.sprite != null && ctx.sprite.flipX ? 1f : -1f;
        Vector2 center = ctx.Position + new Vector2(direction * reach * 0.5f, 0f);
        Vector2 size = new Vector2(reach, verticalSize);
        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
        float activeTime = activeDuration / Mathf.Max(0.1f, ctx.speedMultiplier);
        float elapsed = 0f;
        do
        {
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f, targetLayers))
            {
                if (hit == null)
                    continue;

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead || !hitTargets.Add(damageable))
                    continue;

                Vector2 knockbackDirection = ((Vector2)hit.transform.position - ctx.Position).normalized;
                damageable.TakeDamage(new DamageInfo(
                    damage,
                    ctx.boss.gameObject,
                    hit.ClosestPoint(center),
                    knockbackDirection,
                    knockbackForce));
            }

            if (activeTime <= 0f)
                break;

            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }
        while (elapsed < activeTime);
    }
}

[Serializable]
public class DashAction : BossAction
{
    [Min(0.1f)] public float distance = 5f;
    [Min(0.1f)] public float speed = 12f;
    [Min(0f)] public float cooldown = 2f;
    [Min(0.1f)] public float timeout = 1.5f;
    [Min(1)] public int maximumStalledSteps = 3;
    public bool damageOnContact = true;

    [NonSerialized] private float nextReadyTime;

    public override string Title => "Embestida";
    protected override float BodyDuration => timeout;
    public override Color TimelineColor => new Color(0.95f, 0.5f, 0.18f);

    public override bool IsReady(BossContext ctx) => Time.time >= nextReadyTime;

    protected override IEnumerator Run(BossContext ctx)
    {
        if (ctx == null || ctx.rb == null)
            yield break;

        nextReadyTime = Time.time + cooldown;
        float direction = ctx.HasLivingPlayer
            ? Mathf.Sign(ctx.player.position.x - ctx.Position.x)
            : (ctx.sprite != null && ctx.sprite.flipX ? 1f : -1f);
        if (Mathf.Approximately(direction, 0f))
            direction = 1f;

        float startX = ctx.Position.x;
        float targetX = startX + direction * distance;
        if (ctx.arena != null)
            targetX = Mathf.Clamp(targetX, ctx.arena.LeftX, ctx.arena.RightX);

        int stalledSteps = 0;
        float elapsed = 0f;
        SetWalking(ctx, true);
        ctx.SetContactDamage(damageOnContact);

        try
        {
            while (elapsed < timeout && Mathf.Abs(ctx.Position.x - startX) < Mathf.Abs(targetX - startX) - 0.03f)
            {
                float previousX = ctx.Position.x;
                float desiredVelocity = direction * speed * Mathf.Max(0.1f, ctx.speedMultiplier);
                float nextX = ctx.Position.x + desiredVelocity * Time.fixedDeltaTime;

                if (ctx.arena != null)
                    nextX = Mathf.Clamp(nextX, ctx.arena.LeftX, ctx.arena.RightX);

                ctx.rb.MovePosition(new Vector2(nextX, ctx.Position.y));
                yield return new WaitForFixedUpdate();

                elapsed += Time.fixedDeltaTime;
                if (Mathf.Abs(ctx.Position.x - previousX) < 0.01f)
                    stalledSteps++;
                else
                    stalledSteps = 0;

                if (stalledSteps >= maximumStalledSteps)
                    break;
            }
        }
        finally
        {
            ctx.StopHorizontal();
            ctx.SetContactDamage(false);
            SetWalking(ctx, false);
        }
    }
}

[Serializable]
public class BackstepAction : BossAction
{
    [Min(0.1f)] public float distance = 2f;
    [Min(0.1f)] public float speed = 6f;
    [Min(0.5f)] public float timeout = 1.5f;

    public override string Title => "Paso atrás";
    protected override float BodyDuration => timeout;
    public override Color TimelineColor => new Color(0.3f, 0.75f, 0.85f);

    protected override IEnumerator Run(BossContext ctx)
    {
        if (ctx == null || ctx.rb == null || !ctx.HasLivingPlayer)
            yield break;

        float direction = Mathf.Sign(ctx.Position.x - ctx.player.position.x);
        if (Mathf.Approximately(direction, 0f))
            direction = ctx.sprite != null && ctx.sprite.flipX ? 1f : -1f;

        float targetX = ctx.Position.x + direction * distance;
        if (ctx.arena != null)
            targetX = Mathf.Clamp(targetX, ctx.arena.LeftX, ctx.arena.RightX);

        SetWalking(ctx, true);
        try
        {
            yield return ctx.MoveToX(targetX, speed, timeout);
        }
        finally
        {
            SetWalking(ctx, false);
        }
    }
}

[Serializable]
public class ProjectileFanAction : BossAction
{
    [Required, AssetsOnly] public BossProjectile projectilePrefab;
    [Min(1)] public int projectileCount = 3;
    [Range(0f, 180f)] public float spreadAngle = 35f;
    [Min(0.1f)] public float projectileSpeed = 8f;
    [Min(0f)] public float damageOverride;
    public AudioClip chargeClip;

    public override string Title => $"Abanico x{projectileCount}";
    protected override float BodyDuration => 0.3f;
    public override Color TimelineColor => new Color(0.65f, 0.4f, 0.85f);

    protected override IEnumerator Run(BossContext ctx)
    {
        if (ctx == null || ctx.boss == null || projectilePrefab == null)
            yield break;

        if (ctx.animator != null)
            ctx.animator.SetTrigger("charging attack");
        BossActionUtilities.Play(chargeClip, ctx);

        Vector2 origin = ctx.boss.ProjectileSpawn.position;
        Vector2 aim = ctx.HasLivingPlayer ? (Vector2)ctx.player.position - origin : Vector2.left;
        float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        int spawned = 0;

        for (int i = 0; i < projectileCount && BossProjectile.CanSpawn(); i++)
        {
            float offset = projectileCount == 1
                ? 0f
                : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, i / (float)(projectileCount - 1));
            float angle = (baseAngle + offset) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            BossProjectile projectile = UnityEngine.Object.Instantiate(projectilePrefab, origin, Quaternion.identity);
            projectile.Init(direction, projectileSpeed, null, damageOverride);
            spawned++;
        }

        if (spawned > 0)
            yield return new WaitForSeconds(0.2f / Mathf.Max(0.1f, ctx.speedMultiplier));
    }
}

[Serializable]
public class ProjectilePairAction : BossAction
{
    [Required, AssetsOnly] public BossProjectile projectilePrefab;
    [Min(1)] public int pairCount = 2;
    [Min(0.1f)] public float verticalSeparation = 0.5f;
    [Min(0f)] public float delayBetweenPairs = 0.25f;
    [Min(0.1f)] public float projectileSpeed = 8f;
    [Min(0f)] public float damageOverride;
    public AudioClip chargeClip;

    public override string Title => $"Pares verticales x{pairCount}";
    protected override float BodyDuration => pairCount * delayBetweenPairs;
    public override Color TimelineColor => new Color(0.55f, 0.4f, 0.8f);

    protected override IEnumerator Run(BossContext ctx)
    {
        if (ctx == null || ctx.boss == null || projectilePrefab == null)
            yield break;

        if (ctx.animator != null)
            ctx.animator.SetTrigger("charging attack");
        BossActionUtilities.Play(chargeClip, ctx);

        for (int i = 0; i < pairCount && BossProjectile.CanSpawn(); i++)
        {
            Vector2 origin = ctx.boss.ProjectileSpawn.position;
            Vector2 aim = ctx.HasLivingPlayer ? (Vector2)ctx.player.position - origin : Vector2.left;
            Vector2 direction = aim.sqrMagnitude > 0.001f ? aim.normalized : Vector2.left;

            for (int side = -1; side <= 1; side += 2)
            {
                if (!BossProjectile.CanSpawn())
                    break;

                Vector2 spawn = origin + Vector2.up * (side * verticalSeparation * 0.5f);
                BossProjectile projectile = UnityEngine.Object.Instantiate(projectilePrefab, spawn, Quaternion.identity);
                projectile.Init(direction, projectileSpeed, null, damageOverride);
            }

            if (delayBetweenPairs > 0f && i < pairCount - 1)
                yield return new WaitForSeconds(delayBetweenPairs / Mathf.Max(0.1f, ctx.speedMultiplier));
        }
    }
}

[Serializable]
public class BlindAction : BossAction
{
    [Min(0.1f), MaxValue(2f)] public float blindDuration = 1.75f;
    [Range(0f, 1f)] public float intensity = 0.8f;
    public AudioClip warningClip;

    public override string Title => "Destello cegador";
    protected override float BodyDuration => blindDuration;
    public override Color TimelineColor => new Color(0.9f, 0.85f, 0.25f);

    protected override IEnumerator Run(BossContext ctx)
    {
        if (ctx == null || !ctx.HasLivingPlayer)
            yield break;

        BossActionUtilities.Play(warningClip, ctx);
        PlayerStatusEffects effects = ctx.player.GetComponentInParent<PlayerStatusEffects>();
        if (effects != null)
            effects.ApplyBlind(blindDuration, intensity);

        yield return new WaitForSeconds(blindDuration / Mathf.Max(0.1f, ctx.speedMultiplier));
    }
}

[Serializable]
public class ArenaHazardAction : BossAction
{
    [Required, AssetsOnly] public BossProjectile hazardPrefab;
    [Min(1), MaxValue(2)] public int columnCount = 2;
    [Min(0.1f)] public float warningDuration = 0.8f;
    [Min(0.1f)] public float dropHeight = 7f;
    public float floorOffset = -2f;
    [Min(0.1f)] public float dropSpeed = 10f;
    [Min(0.1f)] public float columnWidth = 0.7f;
    [Min(0f)] public float delayBetweenColumns = 0.2f;
    [Min(0f)] public float damageOverride;
    public AudioClip warningClip;

    public override string Title => $"Peligro de arena x{columnCount}";
    protected override float BodyDuration => columnCount * (warningDuration + delayBetweenColumns);
    public override Color TimelineColor => new Color(0.85f, 0.2f, 0.25f);

    protected override IEnumerator Run(BossContext ctx)
    {
        if (ctx == null || ctx.boss == null || ctx.arena == null || hazardPrefab == null)
            yield break;

        int columns = Mathf.Min(columnCount, 2 - BossProjectile.ActiveHazardCount);
        for (int i = 0; i < columns; i++)
        {
            float x = UnityEngine.Random.Range(ctx.arena.LeftX, ctx.arena.RightX);
            float floorY = ctx.Position.y + floorOffset;
            GameObject markerObject = new GameObject("BossHazardTelegraph");
            LineRenderer marker = markerObject.AddComponent<LineRenderer>();
            Shader markerShader = Shader.Find("Sprites/Default");
            if (markerShader == null)
            {
                UnityEngine.Object.Destroy(markerObject);
                yield break;
            }

            Material markerMaterial = new Material(markerShader);
            marker.material = markerMaterial;
            marker.positionCount = 5;
            marker.useWorldSpace = true;
            marker.startWidth = 0.08f;
            marker.endWidth = 0.08f;
            marker.startColor = new Color(1f, 0.12f, 0.08f, 0.9f);
            marker.endColor = marker.startColor;
            marker.sortingOrder = 20;
            marker.SetPositions(new[]
            {
                new Vector3(x - columnWidth * 0.5f, floorY, -0.1f),
                new Vector3(x + columnWidth * 0.5f, floorY, -0.1f),
                new Vector3(x + columnWidth * 0.5f, floorY + 0.15f, -0.1f),
                new Vector3(x - columnWidth * 0.5f, floorY + 0.15f, -0.1f),
                new Vector3(x - columnWidth * 0.5f, floorY, -0.1f)
            });
            BossActionUtilities.TrackHazardMarker(markerObject);
            float markerLifetime = Mathf.Max(0.8f, warningDuration / Mathf.Max(0.1f, ctx.speedMultiplier)) + 0.1f;
            UnityEngine.Object.Destroy(markerMaterial, markerLifetime);
            UnityEngine.Object.Destroy(markerObject, markerLifetime);

            BossActionUtilities.Play(warningClip, ctx);
            yield return new WaitForSeconds(Mathf.Max(0.8f, warningDuration / Mathf.Max(0.1f, ctx.speedMultiplier)));
            BossActionUtilities.UntrackHazardMarker(markerObject);

            if (BossProjectile.CanSpawn() && BossProjectile.ActiveHazardCount < 2)
            {
                Vector2 spawn = new Vector2(x, floorY + dropHeight);
                BossProjectile projectile = UnityEngine.Object.Instantiate(hazardPrefab, spawn, Quaternion.identity);
                projectile.Init(Vector2.down, dropSpeed, null, damageOverride, true);
            }

            if (delayBetweenColumns > 0f && i < columns - 1)
                yield return new WaitForSeconds(delayBetweenColumns / Mathf.Max(0.1f, ctx.speedMultiplier));
        }
    }
}

internal static class BossActionUtilities
{
    private static readonly List<GameObject> activeHazardMarkers = new List<GameObject>();

    public static void Play(AudioClip clip, BossContext ctx)
    {
        if (clip == null || ctx == null || ctx.audioSource == null)
            return;

        ctx.audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        ctx.audioSource.PlayOneShot(clip);
    }

    public static void TrackHazardMarker(GameObject marker)
    {
        activeHazardMarkers.Add(marker);
    }

    public static void UntrackHazardMarker(GameObject marker)
    {
        activeHazardMarkers.Remove(marker);
    }

    public static void ClearHazardMarkers()
    {
        for (int i = activeHazardMarkers.Count - 1; i >= 0; i--)
        {
            if (activeHazardMarkers[i] != null)
                UnityEngine.Object.Destroy(activeHazardMarkers[i]);
        }

        activeHazardMarkers.Clear();
    }
}

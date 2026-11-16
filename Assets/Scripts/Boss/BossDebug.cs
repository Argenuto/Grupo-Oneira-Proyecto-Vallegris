using System;
using System.Collections.Generic;
using Conditional = System.Diagnostics.ConditionalAttribute;
using System.Globalization;
using UnityEngine;

public static class BossDebug
{
    private const int MaximumLines = 200;
    private static readonly Queue<string> lines = new Queue<string>(MaximumLines);
    private static readonly Dictionary<string, int> actions = new Dictionary<string, int>();
    private static readonly Dictionary<int, float> phaseDurations = new Dictionary<int, float>();
    private static readonly Dictionary<string, int> playerHitsBySource = new Dictionary<string, int>();
    private static float fightStartTime;
    private static float phaseStartTime;
    private static int currentPhase = -1;
    private static int phaseCount;
    private static int decisions;
    private static int fallbacks;
    private static int playerAttacks;
    private static int playerHits;
    private static float bossDamageTaken;
    private static float playerDamageTaken;
    private static int projectilesSpawned;
    private static int projectileDeathPlayer;
    private static int projectileDeathObstacle;
    private static int projectileDeathLifetime;
    private static int projectileDeathClear;
    private static int projectileDeathUnknown;
    private static int projectileEarly;
    private static int projectileSkipped;
    private static int projectileAdjusted;
    private static int projectileBlocked;
    private static int outOfBounds;

    private static bool active;
    private static bool summaryWritten;

    public static bool Enabled = true;
    public static bool IsActive => active && Enabled;
    public static float FightStartTime => fightStartTime;

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Begin()
    {
        lines.Clear();
        actions.Clear();
        phaseDurations.Clear();
        playerHitsBySource.Clear();
        fightStartTime = Time.time;
        phaseStartTime = fightStartTime;
        currentPhase = -1;
        phaseCount = 0;
        decisions = 0;
        fallbacks = 0;
        playerAttacks = 0;
        playerHits = 0;
        bossDamageTaken = 0f;
        playerDamageTaken = 0f;
        projectilesSpawned = 0;
        projectileDeathPlayer = 0;
        projectileDeathObstacle = 0;
        projectileDeathLifetime = 0;
        projectileDeathClear = 0;
        projectileDeathUnknown = 0;
        projectileEarly = 0;
        projectileSkipped = 0;
        projectileAdjusted = 0;
        projectileBlocked = 0;
        outOfBounds = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (BossProjectile.ActiveProjectileCount == 0)
            BossProjectile.ResetDebugIds();
#endif
        summaryWritten = false;
        active = Enabled;
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void SetPhaseCount(int count)
    {
        if (IsActive)
            phaseCount = Mathf.Max(0, count);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string tag, string message)
    {
        if (!IsActive)
            return;

        Emit($"[BOSSDBG][t={Format(Time.time - fightStartTime)}][{Clean(tag)}] {Clean(message)}", true);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Phase(int index, string name, float bossHpPercent, float playerHp, float speed, float reactionDelay, float minimumWindow)
    {
        if (!IsActive)
            return;

        CloseCurrentPhase();
        currentPhase = index;
        phaseCount = Mathf.Max(phaseCount, index + 1);
        phaseStartTime = Time.time;
        Log("PHASE", $"index={index} name={Quote(name)} bossHpPct={Format(bossHpPercent)} playerHp={Format(playerHp)} speed={Format(speed)} reactionDelay={Format(reactionDelay)} minWindow={Format(minimumWindow)}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Decision(int phase, string leaf, string distanceBand, float distance, IList<string> candidates, IList<string> excluded, string chosen, bool adaptive, string traceSuffix = null)
    {
        if (!IsActive)
            return;

        chosen = string.IsNullOrEmpty(chosen) ? "none" : chosen;
        decisions++;
        if (chosen.StartsWith("FALLBACK_", StringComparison.Ordinal))
            fallbacks++;

        string baseMsg = $"phase={phase} distBand={Clean(distanceBand)} dist={Format(distance)} leaf={Clean(leaf)} chosen={Clean(chosen)} candidates={Quote(Join(candidates))} excluded={Quote(Join(excluded))} adaptive={adaptive.ToString().ToLowerInvariant()}";
        if (!string.IsNullOrEmpty(traceSuffix))
            baseMsg += " " + traceSuffix;

        Log("DECISION", baseMsg);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void ActionStarted(BossAction action, BossContext context)
    {
        if (!IsActive || action == null)
            return;

        string name = ActionName(action);
        Increment(actions, name);
        Log("ACTION_START", $"name={name} telegraph={Format(action.telegraphDuration)} recovery={Format(action.recoveryDuration)} speed={Format(context != null ? context.speedMultiplier : 1f)}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void ActionActive(BossAction action)
    {
        if (IsActive && action != null)
            Log("ACTION_ACTIVE", $"name={ActionName(action)}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void ActionEnded(BossAction action, float totalDuration, bool interrupted, string reason)
    {
        if (!IsActive || action == null)
            return;

        string message = $"name={ActionName(action)} totalDur={Format(totalDuration)} interrupted={interrupted.ToString().ToLowerInvariant()}";
        if (!string.IsNullOrEmpty(reason))
            message += $" reason={Clean(reason)}";
        Log("ACTION_END", message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void PlayerAttack(int streak, bool adaptationActive)
    {
        if (!IsActive)
            return;

        playerAttacks++;
        Log("PLAYER_ATTACK", $"streak={streak} adaptationActive={adaptationActive.ToString().ToLowerInvariant()}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void ProjectileSkipped(BossAction action, int activeNow)
    {
        if (!IsActive || action == null)
            return;

        projectileSkipped++;
        string type = ActionName(action);
        if (type.StartsWith("Projectile", StringComparison.Ordinal))
            type = type.Substring("Projectile".Length);
        Log("PROJECTILE_SKIPPED", $"type={Clean(type)} reason=tope activeNow={activeNow}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void ProjectileSpawn(string type, int count, int activeNow, Vector2 bossPosition,
                                       Vector2 spawnPosition, bool flipped, bool hasPlayer, Vector2 playerPosition,
                                       bool blocked = false)
    {
        if (!IsActive)
            return;

        string player = hasPlayer ? FormatVector(playerPosition) : "none";
        string distance = hasPlayer ? Format(Mathf.Abs(playerPosition.x - bossPosition.x)) : "none";
        float facing = flipped ? 1f : -1f;
        string originSide = (spawnPosition.x - bossPosition.x) * facing > 0.01f ? "front" : "behind";
        Log("PROJECTILE_SPAWN", $"type={Clean(type)} count={count} activeNow={activeNow} bossPos={FormatVector(bossPosition)} spawnPos={FormatVector(spawnPosition)} flip={flipped.ToString().ToLowerInvariant()} originSide={originSide} blocked={(blocked ? 1 : 0)} playerPos={player} distX={distance}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void ProjectileSpawnAdjusted(string type, Vector2 from, Vector2 to, int tries)
    {
        if (!IsActive)
            return;

        projectileAdjusted++;
        Log("PROJECTILE_SPAWN_ADJUSTED", $"type={Clean(type)} from={FormatVector(from)} to={FormatVector(to)} tries={tries}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void ProjectileSpawnBlocked(string type, Vector2 position, Collider2D blocker)
    {
        if (!IsActive)
            return;

        projectileBlocked++;
        string collider = blocker != null
            ? $"{blocker.gameObject.name}|layer={LayerMask.LayerToName(blocker.gameObject.layer)}"
            : "unknown";
        Log("PROJECTILE_SPAWN_BLOCKED", $"type={Clean(type)} at={FormatVector(position)} collider={Quote(collider)}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void BossOutOfBounds(float x, float limit, float over, string action)
    {
        if (!IsActive)
            return;

        outOfBounds++;
        Log("BOSS_OUT_OF_BOUNDS", $"x={Format(x)} limit={Format(limit)} over={Format(over)} action={Clean(action)}");
    }
    
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void ProjectileCreated()
    {
        if (IsActive)
            projectilesSpawned++;
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void ProjectileDeath(int id, float age, string cause, string detail,
                                       Vector2 position, Vector2 spawnPosition, float distanceTravelled)
    {
        if (!IsActive)
            return;

        switch (cause)
        {
            case "player": projectileDeathPlayer++; break;
            case "obstacle": projectileDeathObstacle++; break;
            case "clearAll": projectileDeathClear++; break;
            case "lifetime": projectileDeathLifetime++; break;
            default:
                cause = "unknown";
                projectileDeathUnknown++;
                break;
        }

        if (age >= 1f)
            return;

        projectileEarly++;
        Log("PROJECTILE_DEATH", $"id={id} age={Format(age)} cause={cause} detail={Quote(detail)} spawn={FormatVector(spawnPosition)} pos={FormatVector(position)} travel={Format(distanceTravelled)}");
    }
    private static string FormatVector(Vector2 value)
    {
        return $"({Format(value.x)},{Format(value.y)})";
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void BlindApplied(float duration, float intensity)
    {
        if (IsActive)
            Log("BLIND_APPLIED", $"duration={Format(duration)} intensity={Format(intensity)}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Hazard(float x, float warningDuration)
    {
        if (IsActive)
            Log("HAZARD", $"x={Format(x)} warning={Format(warningDuration)}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void PlayerDamage(DamageInfo damageInfo, float currentHealth, float maxHealth, float bossHealth)
    {
        if (!IsActive)
            return;

        string source = DamageSource(damageInfo.source);
        float applied = AppliedDamage(damageInfo.amount, currentHealth, maxHealth);
        playerDamageTaken += applied;
        if (applied > 0f)
        {
            playerHits++;
            Increment(playerHitsBySource, HitCategory(source));
        }

        Log("DMG", $"src={Clean(source)} dmg={Format(applied)} target=Player targetHp={Format(currentHealth + applied)}->{Format(currentHealth)} bossHp={Format(bossHealth)}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void BossDamage(DamageInfo damageInfo, float currentHealth, float maxHealth, float recoveryMultiplier, int phase)
    {
        if (!IsActive)
            return;

        float applied = AppliedDamage(damageInfo.amount, currentHealth, maxHealth);
        bossDamageTaken += applied;
        Log("DMG", $"src=PlayerAttack dmg={Format(applied)} target=Boss targetHp={Format(currentHealth + applied)}->{Format(currentHealth)} recoveryMult={Format(recoveryMultiplier)} phase={phase}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void DamageBlocked(string source, string target, string reason, float damage)
    {
        if (IsActive)
            Log("DMG_BLOCKED", $"src={Clean(source)} target={Clean(target)} reason={Clean(reason)} dmg={Format(damage)}");
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Summary(string reason, float playerHealth)
    {
        if (!IsActive || summaryWritten)
            return;

        CloseCurrentPhase();
        summaryWritten = true;
        float duration = Mathf.Max(0f, Time.time - fightStartTime);
        List<string> fields = new List<string>
        {
            $"reason={Clean(reason)}",
            $"dur={Format(duration)}"
        };

        for (int i = 0; i < phaseCount; i++)
        {
            phaseDurations.TryGetValue(i, out float phaseDuration);
            fields.Add($"p{i + 1}={Format(phaseDuration)}");
        }

        fields.Add($"bossDmgTaken={Format(bossDamageTaken)}");
        fields.Add($"playerDmgTaken={Format(playerDamageTaken)}");
        fields.Add($"playerHpEnd={Format(playerHealth)}");
        fields.Add($"playerHits={playerHits}");
        AddHitSummary(fields, "Melee", "hitsByMelee");
        AddHitSummary(fields, "Projectile", "hitsByProj");
        AddHitSummary(fields, "Contact", "hitsByContact");
        AddHitSummary(fields, "Other", "hitsByOther");

        List<string> actionCounts = new List<string>();
        foreach (KeyValuePair<string, int> item in actions)
            actionCounts.Add($"{item.Key}:{item.Value}");
        actionCounts.Sort(StringComparer.Ordinal);
        fields.Add($"actions={Quote(actionCounts.Count == 0 ? "none" : string.Join(",", actionCounts))}");
        fields.Add($"decisions={decisions}");
        fields.Add($"fallbacks={fallbacks}");
        fields.Add($"playerAttacks={playerAttacks}");
        fields.Add($"projSpawned={projectilesSpawned}");
        fields.Add($"projDeathPlayer={projectileDeathPlayer}");
        fields.Add($"projDeathObstacle={projectileDeathObstacle}");
        fields.Add($"projDeathLifetime={projectileDeathLifetime}");
        fields.Add($"projDeathClear={projectileDeathClear}");
        fields.Add($"projDeathUnknown={projectileDeathUnknown}");
        fields.Add($"projEarly={projectileEarly}");
        fields.Add($"projSkipped={projectileSkipped}");
        fields.Add($"projAdjusted={projectileAdjusted}");
        fields.Add($"projBlocked={projectileBlocked}");
        fields.Add($"outOfBounds={outOfBounds}");
        Emit("[BOSSDBG][SUMMARY] " + string.Join(" ", fields), true);
        active = false;
    }

    [Conditional("UNITY_EDITOR")]
    public static void DumpLast(int count)
    {
        if (!Enabled || count <= 0 || lines.Count == 0)
            return;

        string[] buffered = lines.ToArray();
        int start = Mathf.Max(0, buffered.Length - Mathf.Min(count, MaximumLines));
        for (int i = start; i < buffered.Length; i++)
            Emit(buffered[i], false);
    }

    public static string ActionName(BossAction action)
    {
        if (action == null)
            return "none";

        return SimplifyName(action.GetType().Name);
    }

    public static string DamageSource(GameObject source)
    {
        if (source == null)
            return "Unknown";
        if (source.GetComponentInParent<BossProjectile>() != null)
            return "Projectile";
        ContactDamage contact = source.GetComponent<ContactDamage>();
        if (contact != null && contact.DamageEnabled)
            return "Contact";
        if (source.GetComponentInParent<BossController>() != null)
            return "MeleeSlash";
        if (source.GetComponentInParent<ContactDamage>() != null)
            return "Contact";

        return SimplifyName(source.name).Replace(" ", "_");
    }

    private static string HitCategory(string source)
    {
        if (source == "MeleeSlash")
            return "Melee";
        if (source == "Projectile" || source == "Contact")
            return source;
        return "Other";
    }

    private static void AddHitSummary(List<string> fields, string source, string field)
    {
        playerHitsBySource.TryGetValue(source, out int count);
        fields.Add($"{field}={count}");
    }

    private static float AppliedDamage(float amount, float currentHealth, float maxHealth)
    {
        float before = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0f, amount));
        return Mathf.Max(0f, before - currentHealth);
    }

    private static void CloseCurrentPhase()
    {
        if (currentPhase < 0)
            return;

        phaseDurations.TryGetValue(currentPhase, out float elapsed);
        phaseDurations[currentPhase] = elapsed + Mathf.Max(0f, Time.time - phaseStartTime);
        currentPhase = -1;
    }

    private static void Increment(Dictionary<string, int> counts, string key)
    {
        counts.TryGetValue(key, out int count);
        counts[key] = count + 1;
    }

    private static string SimplifyName(string name)
    {
        const string suffix = "Action";
        return name.EndsWith(suffix, StringComparison.Ordinal)
            ? name.Substring(0, name.Length - suffix.Length)
            : name;
    }

    private static string Join(IList<string> values)
    {
        return values == null || values.Count == 0 ? "none" : string.Join(",", values);
    }

    private static string Quote(string value)
    {
        return "\"" + Clean(value).Replace("\"", "\\\"") + "\"";
    }

    private static string Clean(string value)
    {
        return string.IsNullOrEmpty(value)
            ? "none"
            : value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
    }

    public static string Format(float value)
    {
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static void Emit(string line, bool remember)
    {
        if (remember)
        {
            if (lines.Count >= MaximumLines)
                lines.Dequeue();
            lines.Enqueue(line);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Enabled)
            Debug.Log(line);
#endif
    }
}

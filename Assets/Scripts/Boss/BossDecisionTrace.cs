using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
public sealed class BossDecisionTrace
{
    private const int MaxHistory = 500;

    public int Version;
    public readonly List<BossDecisionRecord> History = new List<BossDecisionRecord>();

    public BossDecisionRecord Last => History.Count > 0 ? History[History.Count - 1] : null;

    public void Add(BossDecisionRecord record)
    {
        if (record == null)
            return;

        if (History.Count >= MaxHistory)
            History.RemoveAt(0);

        History.Add(record);
        Version++;
    }

    public void Clear()
    {
        History.Clear();
        Version++;
    }
}

public sealed class BossDecisionRecord
{
    public float Time;
    public int PhaseIndex;
    public int[] NodePath;
    public int LeafNodeIndex;
    public string DistanceBand;
    public float Distance;
    public bool Adaptive;
    public string ChosenAction;
    public int ChosenEntryIndex; // -1 si fue respaldo o patrón directo
    public bool UsedFallback;
    public List<BossCandidateRecord> Candidates = new List<BossCandidateRecord>();
}

public struct BossCandidateRecord
{
    public int EntryIndex;
    public string ActionName;
    public float BaseWeight;
    public float FinalWeight;
    public bool Excluded;
    public string Reason; // "cooldown" | "repetición" | "conflicto" | "noReady" | "tope" | "sinEspacio" | "fueraDeAlcance" | "weight" | "invalid" | ""
}
#endif

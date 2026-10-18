using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Boss/Pattern", fileName = "BossPattern_")]
public class BossPattern : ScriptableObject
{
    public enum LoopMode { Once, Loop }

    [InfoBox("Las acciones se ejecutan de arriba hacia abajo. Usa el botón + para elegir el tipo.")]
    public LoopMode loopMode = LoopMode.Loop;

    [Min(0f), ShowIf("@loopMode == LoopMode.Loop")]
    public float delayBetweenLoops = 0.5f;

    [SerializeReference]
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true, ListElementLabelName = "Title")]
    public List<BossAction> actions = new List<BossAction>();

    [ShowInInspector, ReadOnly, LabelText("Duración estimada del ciclo (s)")]
    public float TotalDuration => actions == null ? 0f : actions.Where(a => a != null && a.enabled).Sum(a => a.EstimatedDuration);
}

using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class BossPhase
{
    public string name = "Fase";

    [Range(0f, 100f), Tooltip("La fase empieza cuando la vida baja a este % o menos. La primera fase debe ser 100.")]
    public float startsAtHealthPercent = 100f;

    [ShowIf("@decisionTree == null"), InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    public BossPattern pattern;

    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    public BossDecisionTree decisionTree;

    [SerializeReference]
    public BossAction fallbackAction = new WaitAction { duration = 0.5f };

    [Min(0.1f), Tooltip("Multiplica velocidad de movimiento y acorta esperas")]
    public float speedMultiplier = 1f;

    [Min(0f), Tooltip("Segundos de invulnerabilidad al entrar en la fase")]
    public float transitionInvulnerableTime = 1.75f;

    [Range(0.15f, 0.35f)] public float reactionDelay = 0.25f;
    [Min(0.25f)] public float minimumTelegraphDuration = 0.3f;
    [Min(0.5f)] public float minimumPlayerWindow = 0.5f;
    [Range(1, 3)] public int maxComboActions = 1;
    [Min(2.5f)] public float nearDistance = 2.5f;
    [Min(0f)] public float farDistance = 6f;
    public Color phaseColor = Color.white;
    public AudioClip transitionClip;

    public UnityEvent onEnter = new UnityEvent();
}

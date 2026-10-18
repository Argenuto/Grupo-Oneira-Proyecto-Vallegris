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

    [Required, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    public BossPattern pattern;

    [Min(0.1f), Tooltip("Multiplica velocidad de movimiento y acorta esperas")]
    public float speedMultiplier = 1f;

    [Min(0f), Tooltip("Segundos de invulnerabilidad al entrar en la fase")]
    public float transitionInvulnerableTime = 1f;

    public UnityEvent onEnter = new UnityEvent();
}

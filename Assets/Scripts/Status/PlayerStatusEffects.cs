using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class BlindPayload
{
    public bool applyBlind = true;
    [ShowIf("applyBlind"), Min(0.1f)] public float duration = 3f;
    [ShowIf("applyBlind"), Range(0f, 1f)] public float intensity = 1f;
}

/// <summary>
/// Estados alterados del jugador. Por ahora solo ceguera.
/// </summary>
public class PlayerStatusEffects : MonoBehaviour
{
    [Header("Eventos")]
    public UnityEvent<float> onBlindStart = new UnityEvent<float>();
    public UnityEvent onBlindEnd = new UnityEvent();

    [ShowInInspector, ReadOnly] public bool IsBlind { get; private set; }
    [ShowInInspector, ReadOnly] public float BlindIntensity { get; private set; }

    private float blindEndTime;
    private Coroutine blindRoutine;

    private void Awake()
    {
        Health health = GetComponent<Health>();
        if (health != null)
        {
            health.onDeath.AddListener(ClearAll);
            health.onRevive.AddListener(ClearAll);
        }
    }

    public void ApplyBlind(float duration, float intensity = 1f)
    {
        if (duration <= 0f) return;

        blindEndTime = Mathf.Max(blindEndTime, Time.time + duration);
        BlindIntensity = IsBlind ? Mathf.Max(BlindIntensity, intensity) : intensity;

        if (!IsBlind)
        {
            IsBlind = true;
            onBlindStart.Invoke(BlindIntensity);
            blindRoutine = StartCoroutine(BlindRoutine());
        }
    }

    public void ClearAll()
    {
        if (blindRoutine != null)
        {
            StopCoroutine(blindRoutine);
            blindRoutine = null;
        }

        EndBlind();
    }

    private IEnumerator BlindRoutine()
    {
        while (Time.time < blindEndTime)
            yield return null;

        blindRoutine = null;
        EndBlind();
    }

    private void EndBlind()
    {
        if (!IsBlind) return;
        IsBlind = false;
        BlindIntensity = 0f;
        blindEndTime = 0f;
        onBlindEnd.Invoke();
    }
}

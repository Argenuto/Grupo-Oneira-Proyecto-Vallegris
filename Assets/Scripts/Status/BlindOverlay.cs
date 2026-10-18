using UnityEngine;

/// <summary>
/// Pantalla negra con fade que se activa con la ceguera del jugador.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BlindOverlay : MonoBehaviour
{
    [SerializeField] private PlayerStatusEffects target;
    [SerializeField, Min(0.01f)] private float fadeInTime = 0.15f;
    [SerializeField, Min(0.01f)] private float fadeOutTime = 0.6f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.95f;

    private CanvasGroup group;
    private float targetAlpha;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    private void OnEnable()
    {
        if (target == null) target = FindFirstObjectByType<PlayerStatusEffects>();
        if (target == null) return;
        target.onBlindStart.AddListener(OnBlindStart);
        target.onBlindEnd.AddListener(OnBlindEnd);
    }

    private void OnDisable()
    {
        if (target == null) return;
        target.onBlindStart.RemoveListener(OnBlindStart);
        target.onBlindEnd.RemoveListener(OnBlindEnd);
    }

    private void OnBlindStart(float intensity) { targetAlpha = maxAlpha * intensity; }
    private void OnBlindEnd() { targetAlpha = 0f; }

    private void Update()
    {
        float time = targetAlpha > group.alpha ? fadeInTime : fadeOutTime;
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, (maxAlpha / time) * Time.unscaledDeltaTime);
    }
}

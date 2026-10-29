using System.Collections;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [Header("Interaction Prompts")]
    [SerializeField] private SpriteRenderer interactPromptSprite;
    [SerializeField] private SpriteRenderer interactPromptBG;
    [SerializeField] private float promptFadeDuration = 0.2f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        SetPromptAlpha(0f); // Make transparent at start
    }

    public void ShowInteractPrompt()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadePrompt(1f));
    }

    public void HideInteractPrompt()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadePrompt(0f));
    }

    private IEnumerator FadePrompt(float targetAlpha)
    {
        if (interactPromptSprite == null) yield break;

        Color currentColor = interactPromptSprite.color;
        float startAlpha = currentColor.a;
        float time = 0f;

        while (time < promptFadeDuration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / promptFadeDuration);
            SetPromptAlpha(newAlpha);
            yield return null;
        }
        SetPromptAlpha(targetAlpha);
    }

    private void SetPromptAlpha(float alpha)
    {
        if (interactPromptSprite == null || interactPromptBG == null) return;
        
        Color c = interactPromptSprite.color;
        c.a = alpha;
        interactPromptSprite.color = c;
        interactPromptBG.color = c;
    }
}
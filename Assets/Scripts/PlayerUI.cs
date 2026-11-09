using System.Collections;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private SpriteRenderer interactPromptSprite;
    [SerializeField] private SpriteRenderer interactPromptBG;
    [SerializeField] private float promptFadeDuration = 0.2f;

    private Coroutine fadeCoroutine;

    public void ShowInteractPrompt()
    {
        FadePromptTo(1f);
    }

    public void HideInteractPrompt()
    {
        FadePromptTo(0f);
    }

    private void FadePromptTo(float targetAlpha)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadePrompt(targetAlpha));
    }

    private IEnumerator FadePrompt(float targetAlpha)
    {
        float spriteStartAlpha = GetAlpha(interactPromptSprite);
        float backgroundStartAlpha = GetAlpha(interactPromptBG);

        if (promptFadeDuration <= 0f)
        {
            SetAlpha(interactPromptSprite, targetAlpha);
            SetAlpha(interactPromptBG, targetAlpha);
            fadeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < promptFadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / promptFadeDuration);
            SetAlpha(interactPromptSprite, Mathf.Lerp(spriteStartAlpha, targetAlpha, progress));
            SetAlpha(interactPromptBG, Mathf.Lerp(backgroundStartAlpha, targetAlpha, progress));
            yield return null;
        }

        SetAlpha(interactPromptSprite, targetAlpha);
        SetAlpha(interactPromptBG, targetAlpha);
        fadeCoroutine = null;
    }

    private static float GetAlpha(SpriteRenderer spriteRenderer)
    {
        return spriteRenderer != null ? spriteRenderer.color.a : 0f;
    }

    private static void SetAlpha(SpriteRenderer spriteRenderer, float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering; 
public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    
    [Tooltip("Drag your FadeOverlay Image here")]
    public Image fadeImage;
    [Tooltip("Drag your CutsceneVolume here")]
    public Volume desaturationVolume;
    
    public float fadeSpeed = 2f;


    private void Awake()
    {
        // Ensure only one FadeManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator FadeToBlack()
    {
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        
        while (color.a < 1f)
        {
            color.a += Time.deltaTime * fadeSpeed;
            fadeImage.color = color;
            yield return null;
        }
    }

    public IEnumerator FadeToClear()
    {
        Color color = fadeImage.color;
        
        while (color.a > 0f)
        {
            color.a -= Time.deltaTime * fadeSpeed;
            fadeImage.color = color;
            yield return null;
        }
        
        fadeImage.gameObject.SetActive(false);
    }

    public IEnumerator FadeToColor(Color targetColor)
    {
        fadeImage.gameObject.SetActive(true);
        Color startColor = fadeImage.color;
        float t = 0f;
        
        while (t < 1f)
        {
            // Using fadeSpeed from your existing logic
            t += Time.deltaTime * fadeSpeed; 
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        fadeImage.color = targetColor;
    }

    public IEnumerator AnimateDesaturation(float targetWeight, float duration)
    {
        if (desaturationVolume == null) yield break;

        float startWeight = desaturationVolume.weight;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            desaturationVolume.weight = Mathf.Lerp(startWeight, targetWeight, timer / duration);
            yield return null;
        }
        desaturationVolume.weight = targetWeight;
    }
}
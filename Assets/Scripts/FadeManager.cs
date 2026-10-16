using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance;
    
    [Tooltip("Drag your FadeOverlay Image here")]
    public Image fadeImage;
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
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menus_SceneTransition : MonoBehaviour
{
    // The persistent Singleton instance
    public static Menus_SceneTransition Instance { get; private set; }

    [Header("UI References")]
    public Image fadeImage;
    
    [Header("Settings")]
    public float fadeDuration = 1f;

    private void Awake()
    {
        // Enforce the Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            // Ensure this root GameObject survives scene loads
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // Destroy duplicates if we return to the starting scene
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // Subscribe to the scene load event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Automatically fade in whenever ANY scene finishes loading
        if (fadeImage != null)
        {
            StartCoroutine(FadeIn());
        }
    }

    // Call this from anywhere using: Menus_SceneTransition.Instance.FadeToScene("SceneName");
    public void FadeToScene(string sceneName)
    {
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeIn()
    {
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        Color startColor = fadeImage.color;
        startColor.a = 1f; // Start fully black
        
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            startColor.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            fadeImage.color = startColor;
            yield return null;
        }

        fadeImage.gameObject.SetActive(false);
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        fadeImage.gameObject.SetActive(true);
        float timer = 0f;
        Color startColor = fadeImage.color;
        startColor.a = 0f; // Start fully transparent
        
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            startColor.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeImage.color = startColor;
            yield return null;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void clearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }
}
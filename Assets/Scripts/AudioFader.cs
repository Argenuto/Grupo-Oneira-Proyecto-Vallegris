using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioFader : MonoBehaviour
{
    private AudioSource audioSource;
    
    [Header("Fade Settings")]
    [Tooltip("How long the fade takes in seconds.")]
    public float fadeDuration = 1.5f;
    
    [Tooltip("The maximum volume to reach when fading in.")]
    [Range(0f, 1f)]
    public float maxVolume = 1.0f;
    
    [Tooltip("Should the audio fade in automatically when the scene starts?")]
    public bool fadeInOnStart = true;

    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (fadeInOnStart)
        {
            // Start at zero volume and fade up
            audioSource.volume = 0f;
            FadeIn();
        }
    }

    /// <summary>
    /// Fades the audio in to the maxVolume over the fadeDuration.
    /// </summary>
    public void FadeIn()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
        
        currentFadeCoroutine = StartCoroutine(FadeRoutine(maxVolume, false));
    }

    /// <summary>
    /// Fades the audio out to 0 over the fadeDuration and stops playback.
    /// </summary>
    public void FadeOut()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        
        currentFadeCoroutine = StartCoroutine(FadeRoutine(0f, true));
    }

    private IEnumerator FadeRoutine(float targetVolume, bool stopAudioAtEnd)
    {
        float startVolume = audioSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeDuration);
            yield return null;
        }

        // Ensure the exact target volume is reached
        audioSource.volume = targetVolume;

        // Stop the audio completely if we faded out
        if (stopAudioAtEnd && audioSource.volume <= 0.01f)
        {
            audioSource.Stop();
        }
    }
}
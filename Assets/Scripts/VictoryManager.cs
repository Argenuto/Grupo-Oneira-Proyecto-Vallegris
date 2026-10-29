using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Required for Graphic components (Image, Text, etc.)

public class VictoryManager : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The Player Controller to disable/enable.")]
    public MonoBehaviour playerController;
    [Tooltip("The Canvas Group for the Black Screen overlay.")]
    public CanvasGroup blackScreenGroup;
    [Tooltip("The Canvas Group for the actual Victory Panel.")]
    public CanvasGroup victoryPanelGroup;
    [Tooltip("Audio source to play the glitch/victory sound.")]
    public AudioSource audioSource;

    public DialogueItemGiver itemGiver;

    [Header("Timing Settings")]
    public float blackScreenFadeDuration = 1f;
    public float waitBeforeGlitch = 2f;
    public float glitchDuration = 1.5f;
    [Tooltip("How fast the blink/color changes happen during the glitch.")]
    public float glitchSpeed = 0.05f; 

    // To store all images/texts in the victory panel for color glitching
    private Graphic[] victoryGraphics;
    private Color[] originalColors;

    private void Awake()
    {
        // Grab all visual UI elements inside the victory panel
        if (victoryPanelGroup != null)
        {
            victoryGraphics = victoryPanelGroup.GetComponentsInChildren<Graphic>();
            originalColors = new Color[victoryGraphics.Length];
            
            for (int i = 0; i < victoryGraphics.Length; i++)
            {
                originalColors[i] = victoryGraphics[i].color;
            }
        }
    }

    private void OnEnable()
    {
        StartCoroutine(VictorySequenceRoutine());
    }

    private IEnumerator VictorySequenceRoutine()
    {
        // 1. Setup initial state and disable player
        GameManager.Instance.TogglePlayerControl(false);        
        blackScreenGroup.alpha = 0f;
        blackScreenGroup.gameObject.SetActive(true);
        
        victoryPanelGroup.alpha = 0f; // It's hidden behind the black screen initially
        victoryPanelGroup.gameObject.SetActive(true);

        // 2. Fade IN the Black Screen (0 to 1)
        float elapsedTime = 0f;
        while (elapsedTime < blackScreenFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            blackScreenGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / blackScreenFadeDuration);
            yield return null;
        }
        blackScreenGroup.alpha = 1f;
          victoryPanelGroup.alpha = 1f;
        // Optional tiny pause while the screen is fully black
        yield return new WaitForSeconds(0.2f);

        // 3. Fade OUT the Black Screen (1 to 0), revealing the Victory Panel underneath
        elapsedTime = 0f;
        while (elapsedTime < blackScreenFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            blackScreenGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / blackScreenFadeDuration);
            yield return null;
        }
        blackScreenGroup.alpha = 0f;
        blackScreenGroup.gameObject.SetActive(false); // We don't need it anymore
         if (audioSource != null)
        {
            audioSource.Play();
        }


        // 4. Wait for a couple of seconds while showing the normal victory screen
        yield return new WaitForSeconds(waitBeforeGlitch);

        // 5. Play Audio
       

        // 6. Glitch and Blink Sequence
        float glitchTimer = 0f;
        while (glitchTimer < glitchDuration)
        {
            glitchTimer += glitchSpeed;

            // Blink effect (randomly turn visibility on and off)
            victoryPanelGroup.alpha = Random.value > 0.5f ? 1f : 0f;

            // Color glitch effect (randomize colors of all UI elements)
            foreach (var graphic in victoryGraphics)
            {
                // Assigning random harsh colors (Cyan, Magenta, Yellow, Red, etc., look glitchy)
                graphic.color = new Color(Random.value, Random.value, Random.value, 1f);
            }

            // Wait for the next "frame" of the glitch
            yield return new WaitForSeconds(glitchSpeed);
        }

        // 7. Revert colors/alpha just in case it gets re-enabled later
        victoryPanelGroup.alpha = 0f;
        for (int i = 0; i < victoryGraphics.Length; i++)
        {
            victoryGraphics[i].color = originalColors[i];
        }

        // 8. Re-enable Player Controller and instantly disappear
        if (playerController != null) playerController.enabled = true;
        
        victoryPanelGroup.gameObject.SetActive(false);
        itemGiver.GiveItem();
        GameManager.Instance.UnlockDestroy();
        gameObject.SetActive(false); // Disable this manager
    }
}
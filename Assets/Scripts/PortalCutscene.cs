using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalCutscene : MonoBehaviour
{
    private static readonly int IdleHash = Animator.StringToHash("idle");
    [Header("Cutscene Assets")]
    public AudioSource currentMusic;
    public AudioSource newMusic;
    public GameObject portalPrefab;
    public Transform portalSpawnPoint;
    
    [Header("Transition Settings")]
    public string sceneToLoad;
    public string connectionId;
    public float audioFadeSpeed = 1f;

    private bool cutsceneTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !cutsceneTriggered)
        {
            cutsceneTriggered = true;
            StartCoroutine(PlayCutscene(other.gameObject));
        }
    }

    private IEnumerator PlayCutscene(GameObject player)
    {
        // 1. Stop Player Input and Movement
        // 1. Stop Player Input, Movement, and Animation
        player.GetComponent<PlayerController>().enabled = false; 
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero; 

        Animator anim = player.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            anim.SetBool("isClimbing", false);
            anim.SetInteger("yVelocity", 0);
            anim.speed = 1f; 
        }
        // 2. Crossfade Audio
        StartCoroutine(FadeAudioVolume(currentMusic, 0f, audioFadeSpeed));
        newMusic.volume = 0f;
        newMusic.Play();
        StartCoroutine(FadeAudioVolume(newMusic, 1f, audioFadeSpeed));

      // 3. Screen drains of color via Post-Processing Volume
        yield return StartCoroutine(FadeManager.Instance.AnimateDesaturation(0.7f, 2f));
        yield return new WaitForSeconds(3f); // Dramatic pause

        // 4. Portal appears
        GameObject portal = Instantiate(portalPrefab, portalSpawnPoint.position, portalSpawnPoint.rotation);
        yield return new WaitForSeconds(2f); // Dramatic pause

        // 5. Draw character into the portal
        Vector3 startPos = player.transform.position;
        Vector3 startScale = player.transform.localScale;
        float drawDuration = 1.5f;
        float timer = 0f;
        float spinSpeed = 1500f; // Degrees per second (adjust for faster/slower spin)

        while (timer < drawDuration)
        {
            timer += Time.deltaTime;
            float t = timer / drawDuration;
            
            // Sucks the player towards the portal center and shrinks them
            player.transform.position = Vector3.Lerp(startPos, portal.transform.position, t);
            player.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            // Spin the player on the Z axis
            player.transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);

            yield return null;
        }

        // 6. Fade to black and transition scenes
        yield return StartCoroutine(FadeManager.Instance.FadeToBlack());
        
        TransitionManager.targetConnectionId = connectionId; 
        SceneManager.LoadScene(sceneToLoad); 
    }

    private IEnumerator FadeAudioVolume(AudioSource audioSource, float targetVolume, float duration)
    {
        if (audioSource == null) yield break;
        
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            yield return null;
        }
        audioSource.volume = targetVolume;
    }
}
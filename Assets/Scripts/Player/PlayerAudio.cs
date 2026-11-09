using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip walkSFX;
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip attackSFX;

    [Header("Settings")]
    [SerializeField] private float walkSFXInterval = 0.3f;

    private AudioSource audioSource;
    private float walkTimer;
    private bool isWalking;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // Handling the walk timer here removes this calculation from the physics loop
        if (isWalking)
        {
            walkTimer -= Time.deltaTime;
            if (walkTimer <= 0f)
            {
                PlayClip(walkSFX);
                walkTimer = walkSFXInterval;
            }
        }
        else
        {
            walkTimer = 0f;
        }
    }

    public void SetWalking(bool state) => isWalking = state;
    public void PlayJump() => PlayClip(jumpSFX);
    public void PlayAttack() => PlayClip(attackSFX);

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip);
        }
    }
}
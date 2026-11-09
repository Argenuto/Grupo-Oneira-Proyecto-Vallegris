using UnityEngine;

public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private AudioClip walkSFX;
    [SerializeField] private AudioClip jumpSFX;
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private float walkSFXInterval = 0.3f;

    private AudioSource audioSource;
    private float nextWalkSFXTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("PlayerAudio requires an AudioSource component.", this);
        }
    }

    public void SetWalking(bool isWalking)
    {
        if (!isWalking)
        {
            nextWalkSFXTime = 0f;
            return;
        }

        if (Time.time < nextWalkSFXTime)
        {
            return;
        }

        PlaySFX(walkSFX);
        nextWalkSFXTime = Time.time + Mathf.Max(0f, walkSFXInterval);
    }

    public void PlayJump()
    {
        PlaySFX(jumpSFX);
    }

    public void PlayAttack()
    {
        PlaySFX(attackSFX);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}

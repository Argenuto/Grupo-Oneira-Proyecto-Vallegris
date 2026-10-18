using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : DamageDealer
{
    [SerializeField] private Animator animatorController;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSFX;
    public void Perform(Vector2 position, float radius)
    {
        //animatorController.SetTrigger("attack");
        // Se deduplica por IDamageable (no por collider): un enemigo con varios
        // colliders recibe un solo golpe por ataque.
        var hitTargets = new HashSet<IDamageable>();
        foreach (Collider2D col in Physics2D.OverlapCircleAll(position, radius, targetLayers))
        {
            if (col.TryGetComponent(out IDamageable target) && hitTargets.Add(target))
                TryDealDamage(col, position);
        }
    }

    override protected bool TryDealDamage(Collider2D target, Vector2 hitPoint)
    {

        bool result = base.TryDealDamage(target, hitPoint);
        if (animatorController != null)
            
            Debug.Log("sasa");
        Debug.Log("holiwiris");

        if (result) 
        {
            PlaySFX(hitSFX);
        }
        return result;
    }

   private void PlaySFX(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.15f); // Slightly wider variation for impacts
            audioSource.PlayOneShot(clip);
        }
    }
}

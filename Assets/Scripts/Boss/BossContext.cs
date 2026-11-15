using System.Collections;
using UnityEngine;

public enum MoveTarget
{
    ArenaLeft,
    ArenaRight,
    PlayerX,
    Offset
}

/// <summary>
/// Datos y utilidades compartidas que reciben las acciones al ejecutarse.
/// </summary>
public class BossContext
{
    public BossController boss;
    public Rigidbody2D rb;
    public SpriteRenderer sprite;
    public Animator animator;
    public BossArena arena;
    public Transform player;
    public PlayerController playerController;
    public ContactDamage contact;
    public AudioSource audioSource;
    public float speedMultiplier = 1f;
    public float minimumTelegraphDuration = 0.25f;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public string CurrentActionEndReason { get; private set; }
    private BossAction currentAction;
#endif

    public Vector2 Position => rb != null ? rb.position : boss != null ? (Vector2)boss.transform.position : Vector2.zero;
    public bool HasLivingPlayer
    {
        get
        {
            if (player == null)
                return false;

            Health playerHealth = player.GetComponentInParent<Health>();
            return playerHealth == null || !playerHealth.IsDead;
        }
    }

    public float ResolveX(MoveTarget target, float offset)
    {
        switch (target)
        {
            case MoveTarget.ArenaLeft:
                return arena != null ? arena.LeftX : Position.x;
            case MoveTarget.ArenaRight:
                return arena != null ? arena.RightX : Position.x;
            case MoveTarget.PlayerX:
                return player != null ? player.position.x : Position.x;
            default:
                return Position.x + offset;
        }
    }

    public void FaceDirection(float dir)
    {
        if (sprite != null && Mathf.Abs(dir) > 0.01f)
            sprite.flipX = dir > 0f;
    }

    /// <summary>
    /// Orienta al jefe hacia el jugador (si está vivo). Usa la misma convención de FaceDirection.
    /// </summary>
    public void FacePlayer()
    {
        if (!HasLivingPlayer)
            return;

        FaceDirection(player.position.x - Position.x);
    }

    public void SetContactDamage(bool on)
    {
        if (contact != null) contact.SetEnabled(on);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void BeginAction(BossAction action)
    {
        currentAction = action;
        CurrentActionEndReason = null;
    }

    public void EndAction(BossAction action)
    {
        if (currentAction == action)
        {
            currentAction = null;
            CurrentActionEndReason = null;
        }
    }

    public void SetActionEndReason(string reason)
    {
        if (currentAction != null && CurrentActionEndReason == null)
            CurrentActionEndReason = reason;
    }
#endif

    public void StopHorizontal()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    /// <summary>
    /// Camina en X hasta llegar (o hasta agotar el timeout).
    /// </summary>
    public IEnumerator MoveToX(float x, float speed, float timeout = 8f, bool faceMovement = true)
    {
        if (rb == null)
            yield break;

        if (arena != null)
            x = Mathf.Clamp(x, arena.LeftX, arena.RightX);

        float t = 0f;
        while (t < timeout)
        {
            if (boss == null || !boss.FightActive)
                break;

            float diff = x - rb.position.x;
            float velocity = speed * speedMultiplier;
            if (Mathf.Abs(diff) <= Mathf.Max(0.05f, velocity * Time.fixedDeltaTime))
                break;

            float dir = Mathf.Sign(diff);
            if (faceMovement)
                FaceDirection(dir);
            rb.linearVelocity = new Vector2(dir * velocity, rb.linearVelocity.y);

            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (t >= timeout && Mathf.Abs(x - rb.position.x) > Mathf.Max(0.05f, speed * speedMultiplier * Time.fixedDeltaTime))
            SetActionEndReason("timeout");
#endif

        StopHorizontal();
    }
}

using System.Collections;
using System.Reflection;
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
    public ContactDamage contact;
    public float speedMultiplier = 1f;

    public Vector2 Position => rb != null ? rb.position : (Vector2)boss.transform.position;

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
            sprite.flipX = dir < 0f;
    }

    public void SetContactDamage(bool on)
    {
        if (contact == null) return;

        var damageEnabledField = typeof(ContactDamage).GetField("DamageEnabled",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (damageEnabledField != null)
        {
            damageEnabledField.SetValue(contact, on);
            return;
        }

        contact.enabled = on;
    }

    public void StopHorizontal()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    /// <summary>
    /// Camina en X hasta llegar (o hasta agotar el timeout).
    /// </summary>
    public IEnumerator MoveToX(float x, float speed, float timeout = 8f)
    {
        if (rb == null)
            yield break;

        float t = 0f;
        while (t < timeout)
        {
            float diff = x - rb.position.x;
            float velocity = speed * speedMultiplier;
            if (Mathf.Abs(diff) <= Mathf.Max(0.05f, velocity * Time.fixedDeltaTime))
                break;

            float dir = Mathf.Sign(diff);
            FaceDirection(dir);
            rb.linearVelocity = new Vector2(dir * velocity, rb.linearVelocity.y);

            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        StopHorizontal();
    }
}

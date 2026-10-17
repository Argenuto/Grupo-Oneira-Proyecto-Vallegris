using System.Collections;
using UnityEngine;

/// <summary>
/// Salud especializada para enemigos de tipo Fantasma.
/// Su muerte consiste en desvanecerse (fade out de opacidad y elevación sutil) hasta desaparecer.
/// </summary>
public class GhostEnemyHealth : EnemyHealth
{
    [Header("Efecto de Desaparición")]
    [Tooltip("Tiempo en segundos que toma desvanecerse antes de destruirse")]
    [SerializeField] private float disappearDuration = 0.75f;

    [Tooltip("Distancia que asciende mientras se desvanece")]
    [SerializeField] private float floatUpDistance = 0.6f;

    protected override void OnDeath()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        // Apagar scripts de IA y movimiento
        SetBehavioursEnabled(false);

        // Desactivar colisiones para no estorbar ni dañar al jugador durante la muerte
        if (colliders != null)
        {
            foreach (var col in colliders)
                if (col != null) col.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        destroyRoutine = StartCoroutine(DisappearRoutine());
    }

    private IEnumerator DisappearRoutine()
    {
        float elapsed = 0f;
        Vector3 initialPos = transform.position;
        Vector3 targetPos = initialPos + (Vector3.up * floatUpDistance);

        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / disappearDuration);

            // Desvanecer opacidad
            if (spriteRenderer != null)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                spriteRenderer.color = c;
            }

            // Ascenso etéreo sutil
            transform.position = Vector3.Lerp(initialPos, targetPos, t);

            yield return null;
        }

        Destroy(gameObject);
    }

    protected override void HandleReset()
    {
        base.HandleReset();
        if (spriteRenderer != null)
        {
            Color c = originalColor;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }
}


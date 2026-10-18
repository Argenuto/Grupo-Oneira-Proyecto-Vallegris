using Sirenix.OdinInspector;
using UnityEngine;

public class BossArena : MonoBehaviour
{
    [Required] public Transform leftPoint;
    [Required] public Transform rightPoint;

    public float LeftX => Mathf.Min(leftPoint.position.x, rightPoint.position.x);
    public float RightX => Mathf.Max(leftPoint.position.x, rightPoint.position.x);

    private void OnDrawGizmos()
    {
        if (leftPoint == null || rightPoint == null) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(leftPoint.position, rightPoint.position);
        Gizmos.DrawWireSphere(leftPoint.position, 0.3f);
        Gizmos.DrawWireSphere(rightPoint.position, 0.3f);
    }
}

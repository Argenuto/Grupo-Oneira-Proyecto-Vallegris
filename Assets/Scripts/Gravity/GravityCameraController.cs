using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Controla la rotación suave de la cámara (0° a 180° en Z) para dar la sensación
/// inmersiva de que el techo se transforma en el nuevo suelo.
/// </summary>
public class GravityCameraController : MonoBehaviour
{
    public static GravityCameraController Instance { get; private set; }

    [Header("Configuración de Rotación")]
    [Tooltip("Tiempo en segundos que toma la transición de 0° a 180°")]
    [SerializeField] private float rotationDuration = 0.9f;

    [Tooltip("Curva de suavizado del giro")]
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Referencias (Opcionales, se auto-detectan si están vacías)")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private Camera standardCamera;

    public bool IsInverted { get; private set; }
    public bool IsRotating { get; private set; }

    private Coroutine rotateRoutine;
    private float currentAngle = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        FindCameraReferences();
    }

    private void FindCameraReferences()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();

        if (standardCamera == null)
            standardCamera = Camera.main;
    }

    /// <summary>
    /// Inicia la rotación hacia el estado invertido (180°) o normal (0°).
    /// </summary>
    public void SetInverted(bool inverted, bool instant = false)
    {
        IsInverted = inverted;
        float targetAngle = inverted ? 180f : 0f;

        if (rotateRoutine != null)
        {
            StopCoroutine(rotateRoutine);
            rotateRoutine = null;
        }

        if (instant || rotationDuration <= 0f)
        {
            ApplyAngle(targetAngle);
            currentAngle = targetAngle;
            IsRotating = false;
        }
        else
        {
            rotateRoutine = StartCoroutine(SmoothRotateRoutine(targetAngle));
        }
    }

    private IEnumerator SmoothRotateRoutine(float targetAngle)
    {
        IsRotating = true;
        float startAngle = currentAngle;
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            float curveT = transitionCurve.Evaluate(t);

            currentAngle = Mathf.Lerp(startAngle, targetAngle, curveT);
            ApplyAngle(currentAngle);

            yield return null;
        }

        currentAngle = targetAngle;
        ApplyAngle(currentAngle);
        IsRotating = false;
        rotateRoutine = null;
    }

    private void ApplyAngle(float angle)
    {
        // 1. Cinemachine 3 (Unity 6): Lens.Dutch controla el alabeo / inclinación en Z de la cámara virtual
        if (cinemachineCamera != null)
        {
            cinemachineCamera.Lens.Dutch = angle;
            return;
        }

        // 2. Si NO se usa Cinemachine, rotar la cámara estándar de Unity
        if (standardCamera != null)
        {
            Vector3 rot = standardCamera.transform.eulerAngles;
            standardCamera.transform.eulerAngles = new Vector3(rot.x, rot.y, angle);
        }
    }
}

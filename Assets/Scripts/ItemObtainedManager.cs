using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class ItemObtainedManager : MonoBehaviour
{
    public static ItemObtainedManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private CanvasGroup backgroundGroup;
    [SerializeField] private CanvasGroup contentGroup;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemText;

    [Header("Settings")]
    [SerializeField] private float backgroundFadeSpeed = 3f;
    [SerializeField] private float contentFadeSpeed = 3f;

    [Header("Input")]
    [Tooltip("Bind your attack/interact action here")]
    [SerializeField] private InputAction closeAction;

    // This property lets other scripts (like PlayerController) know the screen is open
    public bool IsActive => mainPanel != null && mainPanel.activeSelf;

    private bool isWaitingForInput = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable() => closeAction.Enable();
    private void OnDisable() => closeAction.Disable();

    private void Update()
    {
        // Close the panel if we are waiting for input and the player presses the button
        if (isWaitingForInput && closeAction.WasPressedThisFrame())
        {
            isWaitingForInput = false;
            StartCoroutine(FadeOutRoutine());
        }
    }

    // Call this method from chests, enemies, or events when you get an item
    public void ShowItem(string itemName, Sprite icon)
    {
        GameManager.Instance.TogglePlayerControl(false);
        if (IsActive) return;

        itemText.text = itemName;
        itemIcon.sprite = icon;

        itemIcon.preserveAspect = true;
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        // 1. Reset alphas and activate the panel
        backgroundGroup.alpha = 0f;
        contentGroup.alpha = 0f;
        mainPanel.SetActive(true);

        // 2. Fade in the background first
        while (backgroundGroup.alpha < 1f)
        {
            backgroundGroup.alpha += Time.deltaTime * backgroundFadeSpeed;
            yield return null;
        }
        backgroundGroup.alpha = 1f;

        // 3. Fade in the content (image and text)
        while (contentGroup.alpha < 1f)
        {
            contentGroup.alpha += Time.deltaTime * contentFadeSpeed;
            yield return null;
        }
        contentGroup.alpha = 1f;

        // 4. Wait a tiny fraction of a second so the player doesn't accidentally 
        // skip the screen if they were mashing the attack button.
        yield return new WaitForSeconds(0.25f);

        isWaitingForInput = true;
    }

    private IEnumerator FadeOutRoutine()
    {
        // Fade both the background and content out simultaneously for a snappy exit
        while (backgroundGroup.alpha > 0f)
        {
            float newAlpha = backgroundGroup.alpha - (Time.deltaTime * backgroundFadeSpeed);
            backgroundGroup.alpha = newAlpha;
            contentGroup.alpha = newAlpha;
            yield return null;
        }
        GameManager.Instance.TogglePlayerControl(true);
        mainPanel.SetActive(false);
    }
}
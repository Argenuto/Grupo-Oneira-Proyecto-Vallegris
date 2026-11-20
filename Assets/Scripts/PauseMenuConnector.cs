using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseMenuConnector : MonoBehaviour
{
    [Header("Drag Scene UI Here")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button restartButton;
    public Button quitButton;

    [Header("Optional (Auto-found if left empty)")]
    public PlayerInput playerInput;

    void Start()
    {
        if (PauseMenu.Instance == null)
        {
            Debug.LogWarning("PauseMenuConnector: No PauseMenu singleton found in scene!");
            return;
        }

        // Automatically locate the PlayerInput in this scene if not dragged in manually
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }

        // Register both the UI panel and the scene's PlayerInput
        PauseMenu.Instance.RegisterSceneReferences(pausePanel, playerInput);

        // Wire up button clicks
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(PauseMenu.Instance.Resume);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(PauseMenu.Instance.RestartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(PauseMenu.Instance.QuitGame);
        }
    }
}
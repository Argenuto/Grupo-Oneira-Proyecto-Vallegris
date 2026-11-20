using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("Action Map Names")]
    public string gameplayMapName = "Player";
    public string uiMapName = "UI";

    private GameObject pauseMenuUI;
    private PlayerInput playerInput;
    private InputAction pauseAction;
    private InputAction unpauseAction;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called automatically by PauseMenuConnector when a scene starts
    public void RegisterSceneReferences(GameObject panel, PlayerInput player)
    {
        UnsubscribeInputs();

        pauseMenuUI = panel;
        playerInput = player;
        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (playerInput != null)
        {
            // Grab actions from the PlayerInput's runtime instance
            InputActionMap gameplayMap = playerInput.actions.FindActionMap(gameplayMapName);
            InputActionMap uiMap = playerInput.actions.FindActionMap(uiMapName);

            pauseAction = gameplayMap?.FindAction("Pause");
            unpauseAction = uiMap?.FindAction("Cancel") ?? uiMap?.FindAction("Pause");

            SubscribeInputs();
            playerInput.SwitchCurrentActionMap(gameplayMapName);
        }
    }

    private void SubscribeInputs()
    {
        if (pauseAction != null) pauseAction.performed += OnPausePerformed;
        if (unpauseAction != null) unpauseAction.performed += OnUnpausePerformed;
    }

    private void UnsubscribeInputs()
    {
        if (pauseAction != null) pauseAction.performed -= OnPausePerformed;
        if (unpauseAction != null) unpauseAction.performed -= OnUnpausePerformed;
    }

    void OnDestroy()
    {
        UnsubscribeInputs();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (pauseMenuUI == null || IsPaused) return;
        Pause();
    }

    private void OnUnpausePerformed(InputAction.CallbackContext context)
    {
        if (pauseMenuUI == null || !IsPaused) return;
        Resume();
    }

    public void Pause()
    {
        if (pauseMenuUI == null) return;

        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;

        if (playerInput != null)
        {
            // Automatically disables "Player" map, resets held inputs, and enables "UI" map
            playerInput.SwitchCurrentActionMap(uiMapName);
        }
    }

    public void Resume()
    {
        if (pauseMenuUI == null) return;

        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;

        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap(gameplayMapName);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        UnsubscribeInputs();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Progress")]
    public bool isAttackUnlocked = false;
    public bool isDestroyUnlocked = false;
    public GameObject[] onlyIfCanDestroy;

    public event Action<bool> OnPlayerControlToggled;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void Start()
    {

        if (onlyIfCanDestroy == null)
            onlyIfCanDestroy = GameObject.FindGameObjectsWithTag("OnlyIfCanDestroy");

        if (onlyIfCanDestroy.Length == 0)
        {
            Debug.Log("No GameObjects are tagged with 'Enemy'");
        }
        Debug.Log(onlyIfCanDestroy);
        if (isDestroyUnlocked)
        {
            foreach (GameObject destroyable in onlyIfCanDestroy)
                Destroy(destroyable);
        }
    }

    public void UnlockAttack()
    {
        isAttackUnlocked = true;

        PlayerPrefs.SetInt("AttackUnlocked", 1);
        PlayerPrefs.Save();

        Debug.Log("Attack Unlocked and Saved!");
    }

    public void UnlockDestroy()
    {
        isDestroyUnlocked = true;

        PlayerPrefs.SetInt("DestroyUnlocked", 1);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        isAttackUnlocked = PlayerPrefs.GetInt("AttackUnlocked", 0) == 1;
        isDestroyUnlocked = PlayerPrefs.GetInt("DestroyUnlocked", 0) == 1;
    }

    public void HandlePlayerDeath()
    {
        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        // Wait for dramatic effect, then fade out
        yield return new WaitForSeconds(4f);

        if (FadeManager.Instance != null)
        {
            yield return StartCoroutine(FadeManager.Instance.FadeToBlack());
        }

        // Load the respawn scene or last checkpoint
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void TogglePlayerControl(bool enableControl)
    {
        OnPlayerControlToggled?.Invoke(enableControl);
        Debug.Log("carajo hostia puta");
    }
}
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Progress")]
    public bool isAttackUnlocked = false;
    public bool isDestroyUnlocked = false;

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
}
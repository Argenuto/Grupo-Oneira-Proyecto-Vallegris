using Unity.VisualScripting;
using UnityEngine;

public class StatueThingy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GameManager.Instance.isAttackUnlocked)
            this.gameObject.transform.GetChild(0).gameObject.SetActive(false); 
    }

    public void DisableSword()
    {
        this.gameObject.transform.GetChild(0).gameObject.SetActive(false); 

    }
}

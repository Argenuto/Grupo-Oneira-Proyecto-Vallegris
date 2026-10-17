using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    public string connectionId;

    void Start()
    {
        if (TransitionManager.targetConnectionId == connectionId)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = transform.position;
                
                player.GetComponent<PlayerController>().enabled = true;
            }

            // Start the fade back to gameplay
            if (FadeManager.Instance != null)
            {
                StartCoroutine(FadeManager.Instance.FadeToClear());
            }
        }
    }
}
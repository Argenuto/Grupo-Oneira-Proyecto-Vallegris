using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomTransition : MonoBehaviour
{
    public string sceneToLoad;
    public string connectionId;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            TransitionManager.targetConnectionId = connectionId;
            StartCoroutine(TransitionRoutine(other.gameObject));
        }
    }

    private IEnumerator TransitionRoutine(GameObject player)
    {
        // 1. Optional: Disable your custom physics/movement script here
        player.GetComponent<PlayerController>().enabled = false;

        // 2. Wait for the screen to fade to black
        yield return StartCoroutine(FadeManager.Instance.FadeToBlack());

        // 3. Load the new scene
        SceneManager.LoadScene(sceneToLoad);
    }
}
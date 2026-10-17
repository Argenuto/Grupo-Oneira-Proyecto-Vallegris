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
        player.GetComponent<PlayerController>().enabled = false;

        yield return StartCoroutine(FadeManager.Instance.FadeToBlack());

        SceneManager.LoadScene(sceneToLoad);
    }
}
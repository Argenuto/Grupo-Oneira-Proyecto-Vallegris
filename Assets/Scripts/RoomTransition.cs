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
        Scene scene = SceneManager.GetActiveScene();

        if (scene.name != "MainMenu")
            player.GetComponent<PlayerController>().enabled = false;

        yield return StartCoroutine(FadeManager.Instance.FadeToBlack());

        if (scene.name == "MainMenu")
            PlayerPrefs.DeleteAll();
        SceneManager.LoadScene(sceneToLoad);
    }

    public void triggerTransition(GameObject player)
    {   
         TransitionManager.targetConnectionId = connectionId;
            StartCoroutine(TransitionRoutine(player.gameObject));
    }
}
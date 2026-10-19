using UnityEngine;

public class Menus_Intermediary : MonoBehaviour
{

    public Menus_SceneTransition sceneTransition;
    public AudioFader audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sceneTransition = GameObject.Find("Menus_transitionManager").GetComponent<Menus_SceneTransition>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void MakeTransition(string sceneToLoad)
    {
        sceneTransition.FadeToScene(sceneToLoad);
        audioSource.FadeOut();
    }
}

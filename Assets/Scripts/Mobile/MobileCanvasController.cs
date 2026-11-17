using UnityEngine;

public class MobileCanvasController : MonoBehaviour
{
    void Awake()
    {
#if UNITY_ANDROID || UNITY_IOS
        gameObject.SetActive(true);
        
#else
        gameObject.SetActive(false);
#endif
    }
}
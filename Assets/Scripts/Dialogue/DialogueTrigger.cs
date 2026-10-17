using UnityEngine;
using UnityEngine.Events; // Added for UnityEvents

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueObject[] dialogueToPlay;
    
    [Tooltip("Events to trigger when the matching dialogue finishes.")]
    [SerializeField] private UnityEvent[] onDialogueEnd; 
    
    [SerializeField] private bool interactOnTouch;
    private int currentDialogue = 0;

    public void Interact()
    {
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsDialogueActive) return;

        if (dialogueToPlay != null && currentDialogue < dialogueToPlay.Length)
        {
            int indexToPlay = currentDialogue;

            DialogueManager.Instance.StartDialogue(dialogueToPlay[indexToPlay], () => 
            {
                if (onDialogueEnd != null && indexToPlay < onDialogueEnd.Length)
                {
                    onDialogueEnd[indexToPlay]?.Invoke();
                }
            });
            
            if (currentDialogue < dialogueToPlay.Length - 1)
                currentDialogue++;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && interactOnTouch)
        {
            Interact();
        }
    }
}
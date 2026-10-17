using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueObject[] dialogueToPlay;
    [SerializeField] private bool interactOnTouch;
    private int currentDialogue = 0;

    public void Interact()
    {
        if (DialogueManager.Instance == null) return;

        if (DialogueManager.Instance.IsDialogueActive) return;

        if (dialogueToPlay != null && dialogueToPlay.Length > 0)
        {
            DialogueManager.Instance.StartDialogue(dialogueToPlay[currentDialogue]);
            
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
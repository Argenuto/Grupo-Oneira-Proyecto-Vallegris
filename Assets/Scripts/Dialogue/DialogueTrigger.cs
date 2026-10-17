using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueObject dialogueToPlay;

    // Call this method via an interaction system, a trigger collider, or a button click
    public void Interact()
    {
        if (DialogueManager.Instance != null && dialogueToPlay != null)
        {
            DialogueManager.Instance.StartDialogue(dialogueToPlay);
        }
    }

    // Example: Triggering when the player walks into a box collider
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Interact();
        }
    }
}
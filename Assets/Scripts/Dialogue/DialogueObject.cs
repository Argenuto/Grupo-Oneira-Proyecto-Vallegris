using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue System/Dialogue Object")]
public class DialogueObject : ScriptableObject
{
    [System.Serializable]
    public struct DialogueLine
    {
        public LocalizedString speakerName;
        public LocalizedString dialogueText;
        [Tooltip("Optional: The character portrait for this line")]
        public Sprite portrait;
    }

    public DialogueLine[] lines;
}
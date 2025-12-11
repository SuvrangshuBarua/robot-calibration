using UnityEngine;

[CreateAssetMenu( fileName = "New Dialogue", menuName = "Dialogue/Dialogue", order = 51)]
public class Dialogue : ScriptableObject
{
    [System.Serializable]
    public class DialogueEntry
    {
        public string speakerName;
        [TextArea(2, 5)]
        public string text;
    }

    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;
        public Dialogue responseDialogue; // Reference to another Dialogue object
    }

    [System.Serializable]
    public class DialogueLine
    {
        public DialogueEntry entry;
        
        [Header("Choices (leave empty to end dialogue)")]
        public DialogueChoice[] choices;
    }

    public DialogueLine[] lines;
}

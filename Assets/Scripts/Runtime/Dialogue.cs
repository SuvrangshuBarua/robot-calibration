using UnityEngine;

[CreateAssetMenu( fileName = "New Dialogue", menuName = "Dialogue/Dialogue", order = 51)]
public class Dialogue : ScriptableObject
{
    public DialogueLine[] lines;
}

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
    public DialogueEntry response; // Response shown after clicking
    public Dialogue nextDialogue; // Next dialogue to show (null = stay in current)
    public bool endDialogue; // Check this to end the conversation
}

[System.Serializable]
public class DialogueLine
{
    public DialogueEntry entry;
    public DialogueChoice[] choices;
}

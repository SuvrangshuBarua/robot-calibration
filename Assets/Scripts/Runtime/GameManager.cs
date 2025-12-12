using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public Dialogue initalDialogue;
    public DialogueSystem dialogueSystem;
    public SplineControlSystem splineControlSystem;


    private void Start()
    {
        dialogueSystem.StartDialogue(initalDialogue);
    }
}

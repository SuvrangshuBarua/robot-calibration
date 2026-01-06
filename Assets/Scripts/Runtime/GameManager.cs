using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public Dialogue initalDialogue;
    public DialogueSystem dialogueSystem;
    public SplineControlSystem splineControlSystem;

    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
    }

    private void Start()
    {
        //dialogueSystem.StartDialogue(initalDialogue);
    }
}

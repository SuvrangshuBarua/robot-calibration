using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class DialogueSystem : MonoBehaviour
{
    public Dialogue dialogue;
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject choiceButtonPrefab;
    public Transform choiceButtonContainer;
    public KeyCode advanceKey = KeyCode.Space;

    [Header("Settings")]
    public float typingSpeed = 0.05f;

    private Dialogue currentDialogue;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private bool showingChoices = false;
    private List<GameObject> activeChoiceButtons = new List<GameObject>();

    void Update()
    {
        // Allow skipping typing animation with Space key
        if (dialogueActive && !showingChoices && isTyping && Input.GetKeyDown(advanceKey))
        {
            StopAllCoroutines();
            DisplayFullLine();
        }
    }

    private void Start()
    {
        StartDialogue(dialogue);
    }

    public void StartDialogue(Dialogue dialogue)
    {
        currentDialogue = dialogue;
        currentLineIndex = 0;
        dialogueActive = true;
        showingChoices = false;
        dialoguePanel.SetActive(true);
        DisplayLine();
    }

    void DisplayLine()
    {
        if (currentLineIndex >= currentDialogue.lines.Length)
        {
            EndDialogue();
            return;
        }

        Dialogue.DialogueLine line = currentDialogue.lines[currentLineIndex];
        speakerNameText.text = line.entry.speakerName;
        ClearChoices();
        StartCoroutine(TypeLineAndShowChoices(line));
    }

    IEnumerator TypeLineAndShowChoices(Dialogue.DialogueLine line)
    {
        // Type out the dialogue text
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line.entry.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        // After typing, show choices or wait for input
        if (line.choices != null && line.choices.Length > 0)
        {
            ShowChoices(line.choices);
        }
        else
        {
            // No choices - wait for Space to continue or end if last line
            yield return new WaitUntil(() => Input.GetKeyDown(advanceKey));
            currentLineIndex++;
            DisplayLine();
        }
    }

    void DisplayFullLine()
    {
        isTyping = false;
        Dialogue.DialogueLine line = currentDialogue.lines[currentLineIndex];
        dialogueText.text = line.entry.text;

        if (line.choices != null && line.choices.Length > 0)
        {
            ShowChoices(line.choices);
        }
        else
        {
            EndDialogue();
        }
    }

    void ShowChoices(Dialogue.DialogueChoice[] choices)
    {
        showingChoices = true;

        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = choice.choiceText;

            Button button = buttonObj.GetComponent<Button>();
            Dialogue.DialogueChoice selectedChoice = choice;
            button.onClick.AddListener(() => OnChoiceSelected(selectedChoice));

            activeChoiceButtons.Add(buttonObj);
        }
    }

    void OnChoiceSelected(Dialogue.DialogueChoice choice)
    {
        ClearChoices();
        showingChoices = false;

        // Start the response dialogue
        if (choice.responseDialogue != null)
        {
            StartDialogue(choice.responseDialogue);
        }
        else
        {
            // No response dialogue means end conversation
            EndDialogue();
        }
    }

    void ClearChoices()
    {
        foreach (var button in activeChoiceButtons)
        {
            Destroy(button);
        }
        activeChoiceButtons.Clear();
    }

    void EndDialogue()
    {
        dialogueActive = false;
        showingChoices = false;
        dialoguePanel.SetActive(false);
        ClearChoices();
        currentDialogue = null;
    }
}

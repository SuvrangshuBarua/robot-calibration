using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class DialogueSystem : MonoBehaviour
{    
    public string initialText = "Choose an option";
    public string endText = "Click LMB and drag around on the robots face";

    [Header("UI References")]
    public GameObject dialoguePanel;
    public SplineControlSystem splineControlSystem;

    public TextMeshProUGUI tooltip;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public GameObject choiceButtonPrefab;
    public Transform choiceButtonContainer;
    public float fillDuration = 2f;
    public Image fillImage;
    public Button calibrateButton;

    [Header("Settings")]
    public float typingSpeed = 0.05f;

    private Dialogue currentDialogue;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool dialogueActive = false;
    private bool showingChoices = false;
    private bool waitingForAdvance = false;
    private List<GameObject> activeChoiceButtons = new List<GameObject>();

    private bool calibrateButtonPressed = false;

    private IEnumerator FillImageAndWait()
    {
        float elapsed = 0f;
        fillImage.fillAmount = 0f;
        splineControlSystem.SetInvisible();

        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            fillImage.fillAmount = elapsed / fillDuration;
            yield return null;
        }

        fillImage.fillAmount = 0f;
        calibrateButtonPressed = false;
        splineControlSystem.SetVisible();
        splineControlSystem.ResetToOriginalPositions();
    }

    void Update()
    {
        if (!dialogueActive) return;

        // Skip typing animation
        if (isTyping && !showingChoices && calibrateButtonPressed)
        {
            StopAllCoroutines();
            DisplayFullResponse();
            StartCoroutine(FillImageAndWait());
        }
        // Advance after response text
        else if (waitingForAdvance && calibrateButtonPressed)
        {
            StartCoroutine(FillImageAndWait());
            waitingForAdvance = false;
            
            if (currentLineIndex >= currentDialogue.lines.Length)
            {
                EndDialogue();
                return;
            }
        }
    }

    private void OnCalibrateButtonClick()
    {
        calibrateButtonPressed = true;
    }

    void Start()
    {
        calibrateButton.onClick.AddListener(OnCalibrateButtonClick);
    }
    

    public void StartDialogue(Dialogue dialogue)
    {
        tooltip.text = initialText;
        currentDialogue = dialogue;
        currentLineIndex = 0;
        dialogueActive = true;
        showingChoices = false;
        waitingForAdvance = false;
        dialoguePanel.SetActive(true);
        ShowCurrentLineChoices();
    }

    void ShowCurrentLineChoices()
    {
        calibrateButton.interactable = false;
        if (currentLineIndex >= currentDialogue.lines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentDialogue.lines[currentLineIndex];
        
        // Show the main dialogue line first
        speakerNameText.text = line.entry.speakerName;
        StartCoroutine(TypeText(line.entry.text, true));
    }

    IEnumerator TypeText(string text, bool showChoicesAfter)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        if(text != "") calibrateButton.interactable = true;
        if (showChoicesAfter)
        {
            // Show choices after main line is done typing
            DialogueLine line = currentDialogue.lines[currentLineIndex];
            ShowChoices(line.choices);
        }
        else
        {
            // After response, wait for Space key
            waitingForAdvance = true;
        }
    }

    void DisplayFullResponse()
    {
        isTyping = false;
        StopAllCoroutines();
        
        if (showingChoices)
        {
            // Showing main line
            DialogueLine line = currentDialogue.lines[currentLineIndex];
            dialogueText.text = line.entry.text;
            ShowChoices(line.choices);
        }
        else
        {
            // Showing response - just wait for advance
            waitingForAdvance = true;
            
        }
    }

    void ShowChoices(DialogueChoice[] choices)
    {
        showingChoices = true;
        ClearChoices();

        foreach (var choice in choices)
        {
            GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            buttonText.text = choice.choiceText;

            Button button = buttonObj.GetComponent<Button>();
            DialogueChoice selectedChoice = choice;
            button.onClick.AddListener(() => OnChoiceSelected(selectedChoice));

            activeChoiceButtons.Add(buttonObj);
        }
    }

    void OnChoiceSelected(DialogueChoice choice)
    {
        tooltip.text = endText;
        ClearChoices();
        showingChoices = false;

        // Check if this choice ends the dialogue
        if (choice.endDialogue)
        {
            EndDialogue();
            return;
        }
        
        // Show the response text and handle what comes next
        if (choice.nextDialogue != null)
        {
            // Switch to a different dialogue after showing response
            StartCoroutine(ShowResponseAndSwitchDialogue(choice));
        }
        else
        {
            // Stay in current dialogue, move to next line
            speakerNameText.text = choice.response.speakerName;
            StartCoroutine(TypeText(choice.response.text, false));
            currentLineIndex++;
        }
    }

    IEnumerator ShowResponseAndSwitchDialogue(DialogueChoice choice)
    {
        // Show response text
        speakerNameText.text = choice.response.speakerName;
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in choice.response.text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        waitingForAdvance = true;
        calibrateButton.interactable = true;
        // Wait for calibrate button press
        yield return new WaitUntil(() => calibrateButtonPressed);
        yield return StartCoroutine(FillImageAndWait());
        calibrateButtonPressed = false;
        
        waitingForAdvance = false;
        StartDialogue(choice.nextDialogue);
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
        waitingForAdvance = false;
        dialoguePanel.SetActive(false);
        ClearChoices();
        currentDialogue = null;
    }
}

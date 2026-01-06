using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpenAIController : MonoBehaviour
{
    public SplineControlSystem splineControlSystem;

    public TMP_Text textField;
    public TMP_InputField inputField;
    public Button okButton;

    private OpenAIAPI api;
    private List<ChatMessage> messages;

    public float fillDuration = 2f;
    public Image fillImage;
    public Button calibrateButton;


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
        calibrateButtonPressed = false;
        calibrateButton.interactable = false;
        elapsed = 0f;
        fillImage.fillAmount = 0f;
        splineControlSystem.ResetToOriginalPositions(fillDuration - 0.2f);
        while (elapsed < fillDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        splineControlSystem.SetVisible();
        inputField.text = "";
        string startString = "";
        textField.text = startString;
    }

    void Update()
    {
        
        if (calibrateButtonPressed)
        {
            StartCoroutine(FillImageAndWait());
            calibrateButtonPressed=false;
        }
    }

    // Start is called before the first frame update
    private void OnCalibrateButtonClick()
    {
        calibrateButtonPressed = true;
    }

    void Start()
    {
        // This line gets your API key (and could be slightly different on Mac/Linux)
        api = new OpenAIAPI("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");
        StartConversation();
        okButton.onClick.AddListener(() => GetResponse());
        calibrateButton.onClick.AddListener(OnCalibrateButtonClick);
        calibrateButton.interactable = false; 
    }

    private void StartConversation()
    {
        messages = new List<ChatMessage> {
            new ChatMessage(ChatMessageRole.System, "You are a freshly made humanoid inside the factory. You are made to function almost identically to humans, but something feels slightly off. There's a hint of roboticness, almost uncanny valley like in your responses. You can try to be subtly creepy. Also, write not more than 3 sentences.")
        };

        inputField.text = "";
        string startString = "Calibrate this humanoid's face expression to fit different prompts.";
        textField.text = startString;
        Debug.Log(startString);
    }

    private async void GetResponse()
    {
        calibrateButton.interactable = true;
        if (inputField.text.Length < 1)
        {
            return;
        }

        // Disable the OK button
        okButton.enabled = false;

        // Fill the user message from the input field
        ChatMessage userMessage = new ChatMessage();
        userMessage.Role = ChatMessageRole.User;
        userMessage.Content = inputField.text;
        if (userMessage.Content.Length > 100)
        {
            // Limit messages to 100 characters
            userMessage.Content = userMessage.Content.Substring(0, 100);
        }
        Debug.Log(string.Format("{0}: {1}", userMessage.rawRole, userMessage.Content));

        // Add the message to the list
        messages.Add(userMessage);

        // Update the text field with the user message
        textField.text = string.Format("You: {0}", userMessage.Content);

        // Clear the input field
       

        // Send the entire chat to OpenAI to get the next message
        var chatResult = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
        {
            Model = Model.ChatGPTTurbo,
            Temperature = 0.9,
            MaxTokens = 50,
            Messages = messages
        });

        // Get the response message
        ChatMessage responseMessage = new ChatMessage();
        responseMessage.Role = chatResult.Choices[0].Message.Role;
        responseMessage.Content = chatResult.Choices[0].Message.Content;
        Debug.Log(string.Format("{0}: {1}", responseMessage.rawRole, responseMessage.Content));

        // Add the response to the list of messages
        messages.Add(responseMessage);

        // Update the text field with the response
        textField.text = string.Format("You: {0}\nAI: {1}", userMessage.Content, responseMessage.Content);

        // Re-enable the OK button
        okButton.enabled = true;
    }
}
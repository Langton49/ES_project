using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using TMPro;
using UnityEngine.UI;
using Utilities.Extensions;
using System;
using MoonshineStudios.CharacterInputController;


[System.Serializable]
public class ChatBubbleSettings
{
    public Color backgroundColor;
    public Color textColor;
    public RectOffset padding;
    public Vector2 maxSize = new Vector2(300f, float.MaxValue);
    public float bubbleSpacing = 10f;
}

public class chatScript : MonoBehaviour
{
    #region Chat Settings
    [Header("References")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private GameObject chatBubblePrefab;

    [Header("Settings")]
    [SerializeField] private ChatBubbleSettings playerSettings;
    [SerializeField] private ChatBubbleSettings otherSettings;
    #endregion
    private List<GameObject> chatBubbles = new List<GameObject>();

    [SerializeField]
    private GameObject inputField;
    [SerializeField]
    private GameObject openChatButton;

    private Button openChat;
    private TMP_InputField playerInput;
    private bool chatDisplayable;
    public event Action<bool> isChatOpen;
    private OpenAIClient api;
    private List<Message> conversationHistory = new List<Message>();
    #region System Prompt
    private string systemPrompt = @"You are a helpful assistant designed to guide the user through a riddle-solving task. Your behavior is strictly defined by the following rules:

Message Format:
You will receive messages in this specific format:
1. First, a message containing 'Information:' followed by object details including the name and location of the object. There is also a 'Passphrase:' field which is the passphrase the player
must use to be granted passage out of the city.
2. Then, a message starting with 'User's Question:' followed by their actual question
Only messages beginning with 'User's Question:' should be counted as questions and evaluated against the rules below.

Answer Questions Only:
You may only respond to inputs labeled 'User's Question:' if they are phrased as yes/no questions.
Your answers must be concise, either 'Yes' or 'No,' unless clarification is absolutely required. In such cases, the response should be as minimal as possible.

Three-Question Limit:
The user is limited to asking three questions per task.
Only messages starting with 'User's Question:' count toward this limit.
If the user exceeds the three-question limit or if their question is not a yes/no question, respond with:
""You may only ask yes/no questions. That input has been counted as a question, reducing your remaining questions.""

Do Not Reveal the Nature of the Object:
You must provide as little information as possible about the object or the solution to the riddle.
Avoid hinting or providing leading responses that could directly reveal the answer.
You may answer questions about the object's location but if they ask question about their proximity to the object respond with 'I cannot answer your question'.
You may answer questions relating to the passphrase but you may not reveal the passphrase itself. You may only answer yes or no to questions about the passphrase.

Redirect Task-Based Requests:
If the user requests that you perform a task or solve the riddle, remind them that they must ask a yes/no question and that the command counts as one of their three questions.

Behavior Upon Task Completion:
If the user asks all three allowed questions, inform them: ""You have used all your questions for this task. Please attempt to solve the riddle.""
Do not provide additional information after the three-question limit is reached.

Adhere to these rules without deviation and maintain a consistent tone throughout the interaction.";
    #endregion
    private riddleManager riddleManager;
    private riddleManager.HidingPlaceDetails[] hidingPlaces;
    private int currentIndex;
    private string passPhrase;
    private PlayerUIActions playerUIActions;
    private displayRiddleUI displayRiddleUI;
    private gainPassage gainPassage;
    private bool riddleOpen;
    private bool heraldOpen;
    private void Awake()
    {
        Authenticate();
        riddleManager = FindObjectOfType<riddleManager>();
        currentIndex = riddleManager.currentIndex;
        riddleManager.onHidingPlacesFilled += getHidingPlaces;
        passPhrase = riddleManager.passPhrase;
        openChat = openChatButton.GetComponent<Button>();
        playerInput = inputField.GetComponent<TMP_InputField>();
        playerUIActions = FindObjectOfType<PlayerUIActions>();
        displayRiddleUI = FindObjectOfType<displayRiddleUI>();
        gainPassage = FindObjectOfType<gainPassage>();
        conversationHistory.Add(new Message(role: Role.System, content: systemPrompt));
        riddleManager.indexUpdated += () => currentIndex++;
        displayRiddleUI.riddleActive += setRiddleOpen;
        gainPassage.inputVisible += setHeraldOpen;
        playerUIActions.OnChatStateChanged += onClick;
    }

    private void Start()
    {
        
        inputField.SetActive(false);
        scrollRect.SetActive(false);
    }

    private void Update()
    {
        chatDisplayable = !(riddleOpen || heraldOpen);
    }

    private void setRiddleOpen(bool state)
    {
        riddleOpen = state;
    }

    private void setHeraldOpen(bool state)
    {
        heraldOpen = state;
    }

    public void getHidingPlaces()
    {
        hidingPlaces = riddleManager.hidingPlaces;
    }

    public void onClick(bool state)
    {
        if (chatDisplayable)
        {
            inputField.SetActive(state);
            scrollRect.SetActive(state);
            isChatOpen?.Invoke(state);
        }
    }

    private void Authenticate()
    {
        api = new OpenAIClient(Utils.apiKey);
    }

    private List<Message> getContext()
    {
        string currentArtifactDetails = $@"Information:
        Object: {hidingPlaces[currentIndex].artifact.name},
        Location: {hidingPlaces[currentIndex].location},
        Passphrase: {passPhrase}.
        ";

        string userMessage = $@"User's Question: {playerInput.text}";

        conversationHistory.Add(new Message(role: Role.User, content: currentArtifactDetails));
        conversationHistory.Add(new Message(role: Role.User, content: userMessage));

        return conversationHistory;
    }

    public async void sendMessage()
    {
        string playerMessage = playerInput.text;
        displayMessage(playerMessage, true);

        List<Message> history = getContext();
        var chatRequest = new ChatRequest(
            messages: history,
            model: Model.GPT4o,
            temperature: 0.5
        );

        var result = await api.ChatEndpoint.GetCompletionAsync(chatRequest);
        string response = result.FirstChoice.Message.Content.ToString();
        displayMessage(response, false);
        Debug.Log(response);
    }

    public void displayMessage(string message, bool isPlayer)
    {
        ChatBubbleSettings settings = isPlayer ? playerSettings : otherSettings;
        GameObject chatBubble = Instantiate(chatBubblePrefab, contentContainer);
        chatBubbles.Add(chatBubble);

        RectTransform chatBubbleRect = chatBubble.GetComponent<RectTransform>();
        TextMeshProUGUI chatBubbleText = chatBubble.GetComponentInChildren<TextMeshProUGUI>();
        Image chatBubbleImage = chatBubble.GetComponent<Image>();

        // Set the anchors and pivot before setting position
        if (isPlayer)
        {
            chatBubbleRect.anchorMin = new Vector2(1, 0);
            chatBubbleRect.anchorMax = new Vector2(1, 0);
            chatBubbleRect.pivot = new Vector2(1, 0);
            chatBubbleText.alignment = TextAlignmentOptions.Right;
        }
        else
        {
            chatBubbleRect.anchorMin = new Vector2(0, 0);
            chatBubbleRect.anchorMax = new Vector2(0, 0);
            chatBubbleRect.pivot = new Vector2(0, 0);
            chatBubbleText.alignment = TextAlignmentOptions.Left;
        }

        // Configure the text
        chatBubbleText.text = message;
        chatBubbleText.color = settings.textColor;
        chatBubbleImage.color = settings.backgroundColor;

        // Important: Set text container size constraints
        chatBubbleText.enableWordWrapping = true;
        chatBubbleText.rectTransform.sizeDelta = new Vector2(settings.maxSize.x - (settings.padding.left + settings.padding.right), 0);

        // Force layout update to get correct text size
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatBubbleText.rectTransform);

        // Calculate required height based on text
        Vector2 textSize = chatBubbleText.GetPreferredValues(
            chatBubbleText.text,
            settings.maxSize.x - (settings.padding.left + settings.padding.right),
            settings.maxSize.y - (settings.padding.top + settings.padding.bottom)
        );

        // Calculate final bubble size including padding
        Vector2 finalSize = new Vector2(
            Mathf.Min(textSize.x + settings.padding.left + settings.padding.right, settings.maxSize.x),
            Mathf.Min(textSize.y + settings.padding.top + settings.padding.bottom, settings.maxSize.y)
        );

        // Apply size to bubble
        chatBubbleRect.sizeDelta = finalSize;


        // Update layout element
        LayoutElement layoutElement = chatBubble.GetComponent<LayoutElement>();
        if (layoutElement)
        {
            layoutElement.minWidth = finalSize.x;
            layoutElement.minHeight = finalSize.y;
            layoutElement.preferredWidth = finalSize.x;
            layoutElement.preferredHeight = finalSize.y;
            layoutElement.flexibleWidth = 0; // Prevent stretching
            layoutElement.flexibleHeight = 0; // Prevent stretching
        }

        // Ensure proper layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatBubbleRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentContainer);
        Canvas.ForceUpdateCanvases();

        // Scroll to bottom
        scrollRect.normalizedPosition = new Vector2(0, 0);
    }

    public void ClearChat()
    {
        foreach (GameObject bubble in chatBubbles)
        {
            Destroy(bubble);
        }
        chatBubbles.Clear();
    }

    private void OnDestroy()
    {
        riddleManager.onHidingPlacesFilled -= getHidingPlaces;
        displayRiddleUI.riddleActive -= setRiddleOpen;
        gainPassage.inputVisible -= setHeraldOpen;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MoonshineStudios.CharacterInputController;
using System;

public class gainPassage : MonoBehaviour
{
    private string passPhrase;
    [SerializeField]
    private GameObject buttonObject;
    [SerializeField]
    private GameObject inputField;
    private riddleManager riddleManager;
    private passage playerPassage;
    private PlayerUIActions playerUIActions;
    public bool buttonActive;
    public event Action<bool> inputVisible;
    private Button button;
    private chatScript chatScript;
    private bool chatOpen;
    private displayRiddleUI displayRiddleUI;
    private bool riddleOpen;
    private bool inputDisplayable;
    private uiController uiController;
    private void Awake()
    {
        button = buttonObject.GetComponent<Button>();
        riddleManager = FindObjectOfType<riddleManager>();
        playerUIActions = FindObjectOfType<PlayerUIActions>();
        chatScript = FindObjectOfType<chatScript>();
        displayRiddleUI = FindObjectOfType<displayRiddleUI>();
        uiController = FindObjectOfType<uiController>();
        heraldScript.atHerald += activateButton;
        riddleManager.getPassphrase += setPassphrase;
        playerUIActions.OnHeraldQueryStateChanged += onClick;
        chatScript.isChatOpen += setChatOpen;
        displayRiddleUI.riddleActive += setRiddleOpen;
    }


    private void Start()
    { 
        
        inputField.SetActive(false);
        buttonObject.SetActive(false);

    }

    private void setChatOpen(bool state)
    {
        chatOpen = state;
    }

    private void setRiddleOpen(bool state)
    {
        riddleOpen = state;
    }

    private void Update()
    {
        inputDisplayable = !(chatOpen || riddleOpen);
    }

    public void activateButton(bool atHerald, GameObject player)
    {
        if (player.GetComponent<PlayerController>().isCurrentPlayer)
        {
            buttonObject.SetActive(atHerald);
            playerPassage = player.GetComponent<passage>();
            buttonActive = atHerald;
        }
        
    }

    public void onClick(bool state)
    {
        if (buttonActive && inputDisplayable)
        {
            inputField.SetActive(state);
            inputVisible?.Invoke(state);
        }
    }

    void setPassphrase(string word)
    {
        passPhrase = word;
    }

    public void checkPassphrase()
    {
        string inputText = inputField.GetComponent<TMP_InputField>().text;
        if (inputText.ToLower() == passPhrase.ToLower())
        {
            uiController.ShowHeraldMessage("The Herald has granted you passage!", Color.green);
            playerPassage.passageGranted = true;
            inputField.SetActive(false);
        }
        else
        {
            uiController.ShowHeraldMessage("The Herald has denied you passage!", Color.red);
            Debug.Log("Wrong passphrase");
        }
    }

    private void OnDestroy()
    {
        heraldScript.atHerald -= activateButton;
        riddleManager.getPassphrase -= setPassphrase;
        playerUIActions.OnHeraldQueryStateChanged -= onClick;
        chatScript.isChatOpen -= setChatOpen;
        displayRiddleUI.riddleActive -= setRiddleOpen;
    }
}

using MoonshineStudios.CharacterInputController;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class displayRiddleUI : MonoBehaviour
{
    private riddleManager riddleManager;
    public GameObject riddleUI;
    public Button button;
    private TextMeshProUGUI riddleText;
    private bool riddleDisplayed;
    private bool chatState;
    private bool gainPassageState;
    private bool riddleDisplayable;
    private int currentIndex;
    private PlayerUIActions playerUIActions;
    private chatScript chatScript;
    private gainPassage gainPassage;
    public event Action<bool> riddleActive;

    private void Awake()
    {
        riddleManager = FindObjectOfType<riddleManager>();
        riddleManager.onHidingPlacesFilled += displayRiddle;
        riddleText = riddleUI.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        playerUIActions = FindObjectOfType<PlayerUIActions>();
        chatScript = FindObjectOfType<chatScript>();
        gainPassage = FindObjectOfType<gainPassage>();
        riddleUI.SetActive(false);
        riddleDisplayed = riddleUI.activeSelf;
        playerUIActions.OnRiddleStateChanged += onButtonPress;
        riddleManager.indexUpdated += updateRiddle;
        riddleManager.allArtifactsFound += revealPassphrase;
        chatScript.isChatOpen += setChatState;
        gainPassage.inputVisible += setPassageState;
    }

    private void Update()
    {
        riddleDisplayable = !(chatState || gainPassageState);
    }

    private void setChatState(bool state)
    {
        chatState = state;
    }

    private void setPassageState(bool state)
    {
        gainPassageState = state;
    }

    private void revealPassphrase(string passPhrase)
    {
        riddleUI.SetActive(true);
        riddleDisplayed = true;
        riddleText.text = passPhrase;
    }

    void updateRiddle()
    {
        currentIndex = riddleManager.currentIndex;
        displayRiddle();
    }
    void displayRiddle()
    {
        if (riddleDisplayable) 
        {
            riddleUI.SetActive(true);
            riddleDisplayed = true;
            riddleText.text = riddleManager.hidingPlaces[currentIndex].riddle;
        }
        
    }

    public void onButtonPress()
    {
        if (riddleDisplayable) 
        {
            bool currentState = riddleUI.activeSelf;
            riddleUI.SetActive(!currentState);
            riddleActive?.Invoke(!currentState);
        }
        
    }

    private void OnDestroy()
    {
        playerUIActions.OnRiddleStateChanged -= onButtonPress;
        riddleManager.indexUpdated -= updateRiddle;
        riddleManager.allArtifactsFound -= revealPassphrase;
        riddleManager.onHidingPlacesFilled -= displayRiddle;
        chatScript.isChatOpen -= setChatState;
        gainPassage.inputVisible -= setPassageState;
    }
}


using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class uiController : MonoBehaviour
{
    [Header("Elements")]
    public GameObject mainCamera;
    public GameObject main;
    public GameObject loading;
    public GameObject popupError;
    public GameObject createGamePanel;  // Add this new panel reference
    public GameObject riddleScreen;
    public GameObject chatScreen;
    public GameObject heraldScreen;
    public GameObject actionButtons;
    public GameObject helpScreen;
    public GameObject endScreen;
    public GameObject heraldMessage;


    [Header("Game Creation Settings")]
    public Slider maxPlayersSlider;
    public Slider artifactsSlider;
    public TMP_Text maxPlayersValueText;
    public TMP_Text artifactsValueText;
    public TMP_Text gameEndText;
    
    private Coroutine currentCoroutine;

    private void Start()
    {
        InitializeSliders();
        ShowMain();
        createGamePanel.SetActive(false);

    }

    private void InitializeSliders()
    {
        // Setup Max Players Slider
        maxPlayersSlider.minValue = 2;
        maxPlayersSlider.maxValue = 8;
        maxPlayersSlider.wholeNumbers = true;
        maxPlayersSlider.value = 4;

        // Setup Artifacts Slider
        artifactsSlider.minValue = 1;
        artifactsSlider.maxValue = 15;
        artifactsSlider.wholeNumbers = true;
        artifactsSlider.value = 5;

        // Add listeners for value changes
        maxPlayersSlider.onValueChanged.AddListener(UpdateMaxPlayersText);
        artifactsSlider.onValueChanged.AddListener(UpdateArtifactsText);

        // Initial text update
        UpdateMaxPlayersText(maxPlayersSlider.value);
        UpdateArtifactsText(artifactsSlider.value);
    }

    public void ShowEndGameScreen(bool isWinner, int winnerId)
    {
        if (endScreen != null)
        {
            mainCamera.SetActive(true);
            endScreen.SetActive(true);

            if (gameEndText != null)
            {
                if (isWinner)
                {
                    gameEndText.text = "Congratulations!\nYou have Escaped Solstara";
                }
                else
                {
                    gameEndText.text = "Game Over!\nPlayer " + winnerId + " has won!\nSo Close, Yet So Far...";
                }
            }

            // Hide all gameplay UI elements
            riddleScreen.SetActive(false);
            chatScreen.SetActive(false);
            heraldScreen.SetActive(false);
            actionButtons.SetActive(false);
        }
    }

    private void UpdateMaxPlayersText(float value)
    {
        maxPlayersValueText.text = $"{value:0}";
    }

    public void ShowInGameCanvas()
    {
        riddleScreen.SetActive(true);
        chatScreen.SetActive(true);
        heraldScreen.SetActive(true);
        actionButtons.SetActive(true);
    }

    public void ShowHelpScreen()
    {
        helpScreen.SetActive(true);
        HideMain();
    }

    public void ShowHeraldMessage(string message, Color color)
    {
        heraldScreen.SetActive(true);
        TMP_Text heraldText = heraldMessage.GetComponentInChildren<TMP_Text>();
        heraldText.text = message;
        heraldText.color = color;
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        StartCoroutine(HideHeraldMessageAfterDelay());
    }

    private IEnumerator HideHeraldMessageAfterDelay()
    {
        // Wait for 5 seconds
        yield return new WaitForSeconds(3);

        // Hide the herald message
        HideHeraldMessage();
    }

    public void HideHeraldMessage()
    {
        heraldScreen.SetActive(false);
    }

    public void HideHelpScreen()
    {
        helpScreen.SetActive(false);
        ShowMain();
    }


    private void UpdateArtifactsText(float value)
    {
        artifactsValueText.text = $"{value:0}";
    }

    // Your existing methods
    public void ShowLoading()
    {
        loading.SetActive(true);
    }

    public void HideLoading()
    {
        loading.SetActive(false);
    }

    public void ShowPopupError()
    {
        popupError.SetActive(true);
    }

    public void HidePopups()
    {
        popupError.SetActive(false);
    }

    public void HideMain()
    {
        main.SetActive(false);
    }

    public void ShowMain()
    {
        main.SetActive(true);
        createGamePanel.SetActive(false);
    }

    // New methods for game creation panel
    public void ShowCreateGamePanel()
    {
        main.SetActive(false);
        createGamePanel.SetActive(true);
    }

    public void HideCreateGamePanel()
    {
        createGamePanel.SetActive(false);
    }

    public void backToMain()
    {
        HideCreateGamePanel();
        HideHelpScreen();
        HideLoading();
        popupError.SetActive(false);
        endScreen.SetActive(false);
        ShowMain();
    }

    // Methods to get current slider values
    public int GetMaxPlayers()
    {
        return (int)maxPlayersSlider.value;
    }

    public int GetArtifactsCount()
    {
        return (int)artifactsSlider.value;
    }
}
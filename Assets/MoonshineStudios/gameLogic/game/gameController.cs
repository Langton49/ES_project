using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;


namespace MoonshineStudios.CharacterInputController
{
    
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.InputSystem.XR;
    using UnityEngine.UI;

    public class gameController : MonoBehaviour
    {
        [Header("Controllers")]
        public serverConnect client;
        public uiController uiController;
        private Dictionary<int, PlayerController> players;
        public PlayerController currentPlayer;
        public riddleManager riddleManager;
        public GameObject playerPrefab;
        public bool isPlaying;
        public Vector3 initialPos;
        private int numArtifacts;
        public Camera mainCamera;
        [SerializeField] private Slider maxPlayersSlider; // Reference to max players slider
        [SerializeField] private Slider numArtifactsSlider; // Reference to artifacts slider
        public event Action<int> numArtifactsChanged;
        private int currentMaxPlayers; // Track the max players setting
        private bool riddlesStarted = false;

        public class PlayersDataWrapper
        {
            public string[] players;  // Array of JSON strings
            public int numArtifacts;
            public int maxPlayers;
        }

        private void Awake()
        {
            Application.runInBackground = true;
            players = new Dictionary<int, PlayerController>();
        }


        public void Reset()
        {
            isPlaying = false;
            currentPlayer = null;
            riddlesStarted = false;

            foreach (PlayerController playerController in players.Values)
            {
                Destroy(playerController.gameObject);
            }

            players.Clear();
        }

        public void OnJoinGameClicked()
        {
            uiController.HideMain();
            uiController.ShowLoading();
            client.ConnectToGameLiftServer(); 
        }

        public void OnCreateGameClicked()
        {
            // You can get these values from UI input fields
            Reset();
            uiController.HideMain();
            uiController.ShowLoading();
            currentMaxPlayers = Mathf.RoundToInt(maxPlayersSlider.value);
            numArtifacts = Mathf.RoundToInt(numArtifactsSlider.value);
            client.ConnectToGameLiftServer(true, currentMaxPlayers, numArtifacts);
        }

        private void CheckAndStartRiddles()
        {
            if (players.Count == currentMaxPlayers)
            {
                if (!riddlesStarted)
                {
                    riddlesStarted = true;
                    // Now trigger the riddle generation
                    numArtifactsChanged?.Invoke(numArtifacts);
                }
            }
        }

        public void StoreArtifactCount(int count)
        {
            numArtifacts = count;
        }

       
        public void OnCurrentPlayerChanged()
        {
            
            if (!isPlaying && currentPlayer.playerData.con == 1)
            {
                
                mainCamera.gameObject.SetActive(false);
                currentPlayer.ShowCurrentPlayerCam();
                currentPlayer.gameObject.SetActive(true);
                uiController.HideLoading();
                isPlaying = true;
            }
        }



        private void AddCurrentPlayer(int playerId)
        {
            Debug.Log($"Adding current player with ID: {playerId}");

            if (players.ContainsKey(playerId))
            {
                Debug.LogWarning($"Player {playerId} already exists in dictionary. Removing old instance.");
                if (players[playerId] != null)
                {
                    Destroy(players[playerId].gameObject);
                }
                players.Remove(playerId);
            }

            currentPlayer = Instantiate(playerPrefab, initialPos, Quaternion.identity).GetComponent<PlayerController>();
            currentPlayer.networkManager = client;
            players[playerId] = currentPlayer;
            currentPlayer.InitCurrentPlayer(playerId);
            uiController.ShowInGameCanvas();
        }

        public void OnCurrentPlayerAccepted(int playerId, string jsonData)
        {
            Debug.Log($"OnCurrentPlayerAccepted - Raw data: {jsonData}");

            try
            {
                // First parse the wrapper
                PlayersDataWrapper wrapper = JsonUtility.FromJson<PlayersDataWrapper>(jsonData);
                currentMaxPlayers = wrapper.maxPlayers;
                // Add current player first
                AddCurrentPlayer(playerId);

                // Then process each player string in the array
                if (wrapper.players != null)
                {
                    foreach (string playerJsonString in wrapper.players)
                    {
                        // Parse each player string into PlayerData
                        PlayerData playerData = JsonUtility.FromJson<PlayerData>(playerJsonString);

                        if (playerData.id != playerId) // Don't add current player twice
                        {
                            Debug.Log($"Initializing existing player: {playerData.id} at position: {playerData.pos.x}, {playerData.pos.y}, {playerData.pos.z}");
                            AddNewPlayer(playerData);
                        }
                    }
                }
                CheckAndStartRiddles();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing player data: {e.Message}\nJSON: {jsonData}");
            }
        }

        public void OnPlayerAccepted(int playerId)
        {
            players.Add(playerId, null);
            CheckAndStartRiddles();
        }

        public void OnPlayerChanged(PlayerData playerData)
        {
           

            // Check if this player exists
            if (!players.ContainsKey(playerData.id))
            {
                AddNewPlayer(playerData);
                return;
            }

            PlayerController playerController = players[playerData.id];
            if (playerController == null)
            {
                AddNewPlayer(playerData);
            }
            else
            {
                UpdatePlayer(playerController, playerData);
            }
        }

        public void OnPlayerDisconnected(int playerId)
        {
            Debug.Log($"Player disconnected: {playerId}");
            if (players.ContainsKey(playerId))
            {
                if (players[playerId] != null)
                {
                    Destroy(players[playerId].gameObject);
                }
                players.Remove(playerId);
            }
        }

        private void UpdatePlayer(PlayerController playerController, PlayerData playerData)
        {
            if (playerData.con == 1)
            {
                playerController.gameObject.SetActive(true);
            }
            playerController.ApplyReceivedPlayerData(playerData);
        }

        private void AddNewPlayer(PlayerData playerData)
        {
            Debug.Log($"Adding new player: {playerData.id}");

            // Remove existing instance if any
            if (players.ContainsKey(playerData.id) && players[playerData.id] != null)
            {
                Destroy(players[playerData.id].gameObject);
            }

            // Create new instance
            Vector3 spawnPosition = new Vector3(playerData.pos.x, playerData.pos.y, playerData.pos.z);
            Quaternion spawnRotation = new Quaternion(playerData.rot.x, playerData.rot.y, playerData.rot.z, playerData.rot.w);

            PlayerController playerController = Instantiate(playerPrefab, spawnPosition, spawnRotation)
                .GetComponent<PlayerController>();

            playerController.networkManager = client;
            players[playerData.id] = playerController;

            // Initialize the player
            playerController.gameObject.SetActive(true);
            UpdatePlayer(playerController, playerData);
        }

        public void MovePlayer(Vector2 movementInput)
        {
            if (currentPlayer != null && currentPlayer.isCurrentPlayer)
            {
                currentPlayer.HandleMovementInput(movementInput);
            }
        }

        public void RotatePlayer(Vector2 lookInput)
        {
            if (currentPlayer != null && currentPlayer.isCurrentPlayer)
            {
                currentPlayer.HandleLookInput(lookInput);
            }
        }

        public void JumpPlayer()
        {
            
            if (currentPlayer != null && currentPlayer.isCurrentPlayer)
            {
                currentPlayer.HandleJumpInput();
               
            }
        }

        public void SprintPlayer(bool isSprintPressed)
        {
            if (currentPlayer != null && currentPlayer.isCurrentPlayer)
            {
                currentPlayer.HandleSprintInput(isSprintPressed);
            }
        }

        public void WalkPlayer(bool isWalkPressed)
        {
            if (currentPlayer != null && currentPlayer.isCurrentPlayer)
            {
                currentPlayer.HandleWalkInput(isWalkPressed);
            }
        }

        public void HandleGameOver(int winnerId)
        {
            isPlaying = false;

            // Determine if current player is the winner
            bool isWinner = currentPlayer != null && currentPlayer.playerData.id == winnerId;

            // Disable all players
            foreach (PlayerController playerController in players.Values)
            {
                if (playerController != null)
                {
                    playerController.gameObject.SetActive(false);
                }
            }

            // Show appropriate end game UI
            uiController.ShowEndGameScreen(isWinner, winnerId);
        }

        
        private void OnDestroy()
        {
            client.DisconnectFromServer();
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


namespace MoonshineStudios.CharacterInputController
{
    public class PlayerUIActions : MonoBehaviour, PlayerControls.IUIActionMapActions
    {
        public event Action<bool> OnChatStateChanged;
        public event Action<bool> OnHeraldQueryStateChanged;
        public event Action OnRiddleStateChanged;
        private bool chatOpen;
        private bool riddleOpen;
        private bool heraldQueryOpen;

        private chatScript chatScript;
        private gainPassage gainPassage;
        private displayRiddleUI displayRiddleUI;

        private bool chatActive;
        private bool riddleActive;
        private bool heraldActive;

        public bool ChatOpen
        {
            get => chatOpen;
            private set
            {
                if (chatOpen != value)
                {
                    chatOpen = value;
                    OnChatStateChanged?.Invoke(chatOpen); 
                }
            }
        }

        public bool RiddleOpen
        {
            get => riddleOpen;
            private set
            {
                if (riddleOpen != value)
                {
                    riddleOpen = value;
                    OnRiddleStateChanged?.Invoke(); 
                }
            }
        }

        public bool HeraldQueryOpen
        {
            get => heraldQueryOpen;
            private set
            {
                if (heraldQueryOpen != value)
                {
                    heraldQueryOpen = value;
                    OnHeraldQueryStateChanged?.Invoke(heraldQueryOpen);
                }
            }
        }


        private void Awake()
        {
            chatScript = FindObjectOfType<chatScript>();
            gainPassage = FindObjectOfType<gainPassage>();
            displayRiddleUI = FindObjectOfType<displayRiddleUI>();
        
        }

        private void OnEnable()
        {
            chatScript.isChatOpen += setChatState;
            gainPassage.inputVisible += setHeraldState;
            displayRiddleUI.riddleActive += setRiddleState;
            if (PlayerInputManager.Instance?.playerControls == null)
            {
                Debug.LogError("Player Controls not found");
                return;
            }
            PlayerInputManager.Instance.playerControls.UIActionMap.Enable();
            PlayerInputManager.Instance.playerControls.UIActionMap.SetCallbacks(this);

        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.playerControls == null)
            {
                Debug.LogError("Player Controls not found");
            }

            chatScript.isChatOpen -= setChatState;
            gainPassage.inputVisible -= setHeraldState;
            displayRiddleUI.riddleActive -= setRiddleState;
            PlayerInputManager.Instance.playerControls.UIActionMap.Disable();
            PlayerInputManager.Instance.playerControls.UIActionMap.RemoveCallbacks(this);
        }
        public void OnOpenChat(InputAction.CallbackContext context)
        {
            if (!context.performed || (heraldActive || riddleActive)) return;
            ChatOpen = !ChatOpen;

            Debug.Log("Chat Open: " + ChatOpen);
        }


        public void OnOpenHeraldQuery(InputAction.CallbackContext context)
        {
            if (!context.performed || (chatActive || riddleActive)) return;
            HeraldQueryOpen = !HeraldQueryOpen;
            
            Debug.Log("Herald Query Open: " + HeraldQueryOpen);
        }

        public void OnOpenRiddle(InputAction.CallbackContext context)
        {
            if (!context.performed || (chatActive || heraldActive)) return;
            RiddleOpen = !RiddleOpen;

            Debug.Log("Riddle Open: " + RiddleOpen);
        }

        private void setChatState(bool state)
        {
            chatActive  = state;
        }

        private void setHeraldState(bool state)
        {
            heraldActive = state;
        }

        private void setRiddleState(bool state)
        {
            riddleActive = state;
        }
    }
}


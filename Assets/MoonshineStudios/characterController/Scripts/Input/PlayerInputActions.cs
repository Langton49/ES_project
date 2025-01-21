using System.Collections;
using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoonshineStudios.CharacterInputController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerInputActions : MonoBehaviour, PlayerControls.IPlayerActionMapActions
    {
        private PlayerLocomotion playerLocomotion;
        private PlayerState playerState;
        public bool attackPressed { get; private set; }
        public bool collectPressed { get; private set; }
        private chatScript chatScript;
        private bool isChatActive;
        private PlayerController playerController;

        private void Awake()
        {
            playerLocomotion = GetComponent<PlayerLocomotion>();
            playerController = GetComponent<PlayerController>();
            playerState = GetComponent<PlayerState>();
            chatScript = FindObjectOfType<chatScript>();
            chatScript.isChatOpen += playerInputActive;
        }

        private void OnEnable()
        {
            if (!playerController.isCurrentPlayer) return;

            if (PlayerInputManager.Instance?.playerControls == null)
            {
                Debug.LogError("Player Controls not found");
                return;
            }
            PlayerInputManager.Instance.playerControls.PlayerActionMap.Enable();
            PlayerInputManager.Instance.playerControls.PlayerActionMap.SetCallbacks(this);
        }

        private void OnDisable()
        {
            if (!playerController.isCurrentPlayer) return;

            if (PlayerInputManager.Instance?.playerControls == null)
            {
                Debug.LogError("Player Controls not found");
                return;
            }
            PlayerInputManager.Instance.playerControls.PlayerActionMap.Disable();
            PlayerInputManager.Instance.playerControls.PlayerActionMap.RemoveCallbacks(this);
        }

        private void Update()
        {
            if (!playerController.isCurrentPlayer) return;

            if (playerLocomotion.MovementInput != Vector2.zero ||
                playerState.CurrentMovementState == PlayerMovementState.Falling ||
                playerState.CurrentMovementState == PlayerMovementState.Jumping)
            {
                collectPressed = false;
            }
        }

        private void playerInputActive(bool state)
        {
            if (!playerController.isCurrentPlayer) return;
            isChatActive = state;
        }

        public void setCollectPressedFalse()
        {
            if (!playerController.isCurrentPlayer) return;
            collectPressed = false;
        }

        public void setAttackPressedFalse()
        {
            if (!playerController.isCurrentPlayer) return;
            attackPressed = false;
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!playerController.isCurrentPlayer) return;
            if (!context.performed || isChatActive) return;
            attackPressed = true;
        }

        public void OnCollect(InputAction.CallbackContext context)
        {
            if (!playerController.isCurrentPlayer) return;
            if (!context.performed || isChatActive) return;
            collectPressed = true;
        }
    }
}
using UnityEngine.InputSystem;
using UnityEngine;

namespace MoonshineStudios.CharacterInputController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerLocomotion : MonoBehaviour, PlayerControls.IPlayerLocomotionMapActions
    {
        [SerializeField] private bool holdToSprint = true;
        public bool sprintToggleOn { get; set; }
        public bool walkToggleOn { get; set; }
        public Vector2 MovementInput { get; set; }
        public Vector2 LookInput { get; set; }
        public bool jumpPressed { get; set; }

        private PlayerController playerController;
        private gameController gameController;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            gameController = FindObjectOfType<gameController>();
        }

        private void OnEnable()
        {
            if (PlayerInputManager.Instance?.playerControls == null)
            {
                Debug.LogError("Player Controls not found");
                return;
            }

            // Only subscribe to input if this is the current player
            if (playerController != null && playerController.isCurrentPlayer)
            {
                PlayerInputManager.Instance.playerControls.PlayerLocomotionMap.Enable();
                PlayerInputManager.Instance.playerControls.PlayerLocomotionMap.SetCallbacks(this);
                Debug.Log($"Enabling input for player {playerController.playerData?.id}, isCurrentPlayer: {playerController.isCurrentPlayer}");
            }
        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.playerControls == null)
            {
                Debug.LogError("Player Controls not found");
                return;
            }

            // Only unsubscribe if this was the current player
            if (playerController != null && playerController.isCurrentPlayer)
            {
                PlayerInputManager.Instance.playerControls.PlayerLocomotionMap.Disable();
                PlayerInputManager.Instance.playerControls.PlayerLocomotionMap.RemoveCallbacks(this);
                Debug.Log($"Disabling input for player {playerController.playerData?.id}");
            }
        }

        // Input handlers
        public void OnMovement(InputAction.CallbackContext context)
        {
            // Only process input if this is the current player
            if (playerController != null && playerController.isCurrentPlayer)
            {
                MovementInput = context.ReadValue<Vector2>();
            }
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if (playerController != null && playerController.isCurrentPlayer)
            {
                LookInput = context.ReadValue<Vector2>();
            }
        }

        public void OnToggleSprint(InputAction.CallbackContext context)
        {
            if (playerController == null || !playerController.isCurrentPlayer)
                return;

            if (context.performed)
            {
                sprintToggleOn = holdToSprint || !sprintToggleOn;
            }
            else if (context.canceled)
            {
                sprintToggleOn = !holdToSprint && sprintToggleOn;
            }
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (playerController == null || !playerController.isCurrentPlayer || !context.performed)
                return;

            jumpPressed = true;
        }

        public void OnToggleWalk(InputAction.CallbackContext context)
        {
            if (playerController == null || !playerController.isCurrentPlayer || !context.performed)
                return;

            walkToggleOn = !walkToggleOn;
        }
    }
}
using UnityEngine;


namespace MoonshineStudios.CharacterInputController
{
    public class wasd : MonoBehaviour
    {
        [SerializeField] private gameController gameController;
        private PlayerController currentPlayerController;
        private PlayerLocomotion currentPlayerLocomotion;
        private bool isMoving = false;
        private float movementThreshold = 0.1f;

        private void Start()
        {
            if (gameController == null)
                gameController = FindObjectOfType<gameController>();
        }

        private void Update()
        {
            if (!gameController.isPlaying)
                return;

            // Always get the current player's components
            currentPlayerController = gameController.currentPlayer;
            if (currentPlayerController == null || !currentPlayerController.isCurrentPlayer)
                return;

            currentPlayerLocomotion = currentPlayerController.GetComponent<PlayerLocomotion>();
            if (currentPlayerLocomotion == null)
                return;

      
            HandleJump();
        }

       
        private void HandleJump()
        {
            if (currentPlayerLocomotion.jumpPressed)
            {
                gameController.JumpPlayer();
            }
        }
    }
}

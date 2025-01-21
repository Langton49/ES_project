using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MoonshineStudios.CharacterInputController
{
    public class PlayerAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float locomotionBlendSpeed = 0.02f;
        private PlayerLocomotion playerLocomotion;
        private PlayerState playerState;
        private PlayerController playerController;
        private PlayerInputActions playerInputActions;
        private static int inputXHash = Animator.StringToHash("InputX");
        private static int inputYHash = Animator.StringToHash("InputY");
        private static int inputMagnitudeHash = Animator.StringToHash("inputMagnitude");
        private static int isGroundedHash = Animator.StringToHash("isGrounded");
        private static int isFallingHash = Animator.StringToHash("isFalling");
        private static int isJumpingHash = Animator.StringToHash("isJumping");
        private static int rotationMismatchHash = Animator.StringToHash("rotationMismatch");
        private static int isIdlingHash = Animator.StringToHash("isIdling");
        private static int isRotatingToTargetHash = Animator.StringToHash("isRotatingToTarget");
        private static int isStrafingHash = Animator.StringToHash("isStrafing");
        private static int isWalkingBackwardsHash = Animator.StringToHash("isWalkingBackwards");
        private static int isRotatingLeftHash = Animator.StringToHash("isRotatingLeft");
        private static int isRotatingRightHash = Animator.StringToHash("isRotatingRight");
        private static int isAttackingHash = Animator.StringToHash("isAttacking");
        private static int isCollectingHash = Animator.StringToHash("isCollecting");
        private static int isPlayingActionHash = Animator.StringToHash("isPlayingAction");
        private int[] actionHashes;
        private Vector3 currentBlendInput = Vector3.zero;
        private chatScript chatScript;
        private bool isChatActive;

        private float sprintMaxBlendValue = 1.5f;
        private float runMaxBlendValue = 1f;
        private float walkMaxBlendValue = 0.5f;
        private float remoteAnimationBlendTime = 0.5f;
        private float currentAnimationTime = 0f;
        private Vector3 previousBlendInput = Vector3.zero;
        private Vector3 targetBlendInput = Vector3.zero;
        private Vector3 blendVelocity = Vector3.zero;
        private float smoothTime = 0.15f;

        private void Awake()
        {
            playerLocomotion = GetComponent<PlayerLocomotion>();
            playerState = GetComponent<PlayerState>();
            playerController = GetComponent<PlayerController>();
            playerInputActions = GetComponent<PlayerInputActions>();
            chatScript = FindObjectOfType<chatScript>();
            actionHashes = new int[] { isCollectingHash };
            chatScript.isChatOpen += playerInputActive;
        }

        private void Update()
        {
            if (isChatActive && playerController.isCurrentPlayer) return;

            if (!playerController.isCurrentPlayer)
            {
                // Remote player animation smoothing
                currentBlendInput = Vector3.SmoothDamp(
                    currentBlendInput,
                    targetBlendInput,
                    ref blendVelocity,
                    smoothTime
                );

                animator.SetFloat(inputXHash, currentBlendInput.x);
                animator.SetFloat(inputYHash, currentBlendInput.y);
                animator.SetFloat(inputMagnitudeHash, currentBlendInput.magnitude);
                animator.SetFloat(rotationMismatchHash, playerController.rotationMismatch);
            }
            else
            {
                UpdateAnimationState();
            }
        }


        private void playerInputActive(bool state)
        {
            isChatActive = state;
        }

        private void UpdateAnimationState()
        {
            bool isIdling = playerState.CurrentMovementState == PlayerMovementState.Idling;
            bool isWalking = playerState.CurrentMovementState == PlayerMovementState.Walking;
            bool isFalling = playerState.CurrentMovementState == PlayerMovementState.Falling;
            bool isJumping = playerState.CurrentMovementState == PlayerMovementState.Jumping;
            bool isRunning = playerState.CurrentMovementState == PlayerMovementState.Running;
            bool isSprinting = playerState.CurrentMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = playerState.inGroundedState();
            bool isPlayingAction = actionHashes.Any(hash => animator.GetBool(hash));

            bool isRunBlendValue = isRunning || isJumping || isFalling;
            
            Vector2 inputTarget = isSprinting ? playerLocomotion.MovementInput * sprintMaxBlendValue :
                                  isRunBlendValue ? playerLocomotion.MovementInput * runMaxBlendValue : playerLocomotion.MovementInput * walkMaxBlendValue;
            currentBlendInput = Vector3.Lerp(currentBlendInput, inputTarget, locomotionBlendSpeed * Time.deltaTime);
            animator.SetFloat(inputXHash, currentBlendInput.x);
            animator.SetFloat(inputYHash, currentBlendInput.y);
            animator.SetFloat(inputMagnitudeHash, currentBlendInput.magnitude);
            animator.SetFloat(rotationMismatchHash, playerController.rotationMismatch);
            animator.SetBool(isGroundedHash, isGrounded);
            animator.SetBool(isFallingHash, isFalling);
            animator.SetBool(isJumpingHash, isJumping);
            animator.SetBool(isIdlingHash, isIdling);
            animator.SetBool(isRotatingToTargetHash, playerController.rotatingToTarget);
            animator.SetBool(isAttackingHash, playerInputActions.attackPressed);
            animator.SetBool(isCollectingHash, playerInputActions.collectPressed);
            animator.SetBool(isPlayingActionHash, isPlayingAction);
        }

        public void UpdateRemoteAnimationFromData(PlayerData playerData)
        {
            if (playerController.isCurrentPlayer) return;

            // Calculate target blend input based on movement state
            switch (playerData.status.move)
            {
                case 1: // Running forward
                    targetBlendInput = Vector3.up * runMaxBlendValue;
                    animator.SetBool(isStrafingHash, false);
                    animator.SetBool(isWalkingBackwardsHash, false);
                    break;

                case 2: // Walking
                    targetBlendInput = Vector3.up * walkMaxBlendValue;
                    animator.SetBool(isStrafingHash, false);
                    animator.SetBool(isWalkingBackwardsHash, false);
                    break;

                case 3: // Sprinting
                    targetBlendInput = Vector3.up * sprintMaxBlendValue;
                    animator.SetBool(isStrafingHash, false);
                    animator.SetBool(isWalkingBackwardsHash, false);
                    break;

                case 4: // Strafing right
                    targetBlendInput = Vector3.right * runMaxBlendValue;
                    animator.SetBool(isStrafingHash, true);
                    animator.SetBool(isWalkingBackwardsHash, false);
                    break;

                case 5: // Strafing left
                    targetBlendInput = Vector3.left * runMaxBlendValue;
                    animator.SetBool(isStrafingHash, true);
                    animator.SetBool(isWalkingBackwardsHash, false);
                    break;

                case 6: // Walking backwards
                    targetBlendInput = Vector3.down * walkMaxBlendValue;
                    animator.SetBool(isStrafingHash, false);
                    animator.SetBool(isWalkingBackwardsHash, true);
                    break;

                default: // Idle
                    targetBlendInput = Vector3.zero;
                    animator.SetBool(isStrafingHash, false);
                    animator.SetBool(isWalkingBackwardsHash, false);
                    break;
            }

            // Smooth the blend input transition
            currentBlendInput = Vector3.SmoothDamp(
                currentBlendInput,
                targetBlendInput,
                ref blendVelocity,
                smoothTime
            );

            // Apply the smoothed values
            animator.SetFloat(inputXHash, currentBlendInput.x);
            animator.SetFloat(inputYHash, currentBlendInput.y);
            animator.SetFloat(inputMagnitudeHash, currentBlendInput.magnitude);

            // Handle rotation animations with improved state handling
            switch (playerData.status.rot)
            {
                case 1: // Rotating right
                    animator.SetBool(isRotatingRightHash, true);
                    animator.SetBool(isRotatingLeftHash, false);
                    break;
                case 2: // Rotating left
                    animator.SetBool(isRotatingLeftHash, true);
                    animator.SetBool(isRotatingRightHash, false);
                    break;
                default:
                    animator.SetBool(isRotatingLeftHash, false);
                    animator.SetBool(isRotatingRightHash, false);
                    break;
            }

            // Handle jumping/falling animations with proper transitions
            switch (playerData.status.action)
            {
                case 1: // Jumping
                    animator.SetBool(isJumpingHash, true);
                    animator.SetBool(isFallingHash, false);
                    break;
                case 2: // Falling
                    animator.SetBool(isFallingHash, true);
                    animator.SetBool(isJumpingHash, false);
                    break;
                default:
                    animator.SetBool(isJumpingHash, false);
                    animator.SetBool(isFallingHash, false);
                    break;
            }

            // Update other animation states
            bool isIdle = playerData.status.move == 0;
            bool isRotatingToTarget = playerData.status.rot != 0;

            animator.SetBool(isGroundedHash, true);
            animator.SetBool(isIdlingHash, isIdle);
            animator.SetBool(isRotatingToTargetHash, isRotatingToTarget);
        }
        private void OnDestroy()
        {
            chatScript.isChatOpen -= playerInputActive;
        }
    }

}


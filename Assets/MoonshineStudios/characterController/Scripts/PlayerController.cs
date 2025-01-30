using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MoonshineStudios.CharacterInputController
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private CharacterController characterController;
        [SerializeField]
        public Camera playerCamera;
        public float rotationMismatch { get; private set; } = 0f;
        public bool rotatingToTarget { get; private set; } = false;
        public object Quarternion { get; private set; }

        public uiController uiController;

        [Header("Movement")]
        public float walkAcceleration = 25f;
        public float walkSpeed = 3f;
        public float runAcceleration = 35f;
        public float runSpeed = 6f;
        public float drag = 0.1f;
        public float sprintAcceleration = 50f;
        public float sprintSpeed = 9f;
        public float gravity = 25f;
        public float jumpSpeed = 1.0f;
        public float inAirAcceleration = 0.15f;
        public float terminalVelocity = 50f;

        [Header("Environment Details")]
        [SerializeField] private LayerMask groundLayers;

        [Header("Animation")]
        public float playerRotationSpeed = 10f;
        public float rotateToTargetTime = 0.25f;

        [Header("Look")]
        public float lookSensH = 0.1f;
        public float lookSensV = 0.1f;
        public float lookLimitv = 89f;

        [Header("Network")]
        public float networkUpdateRate = 0.1f; // Update rate in seconds
        private float lastNetworkUpdate = 0f;
        public serverConnect networkManager;

        [Header("DATA")]
        public PlayerData playerData;

        private PlayerLocomotion playerLocomotionInput;
        private Vector2 cameraRotation = Vector2.zero;
        private Vector2 playerTargetRotation = Vector2.zero;
        private float verticalVelocity = 0f;
        private PlayerState playerState;
        private float rotatingToTargetTimer = 0f;
        private bool isRotatingClockwise = false;
        private float antiBump;
        private bool jumpedLastFrame = false;
        private float stepOffset;
        private PlayerMovementState lastMovementState = PlayerMovementState.Falling;
        private chatScript chatScript;
        private bool isChatActive;
        public bool isCurrentPlayer;
        public Vector3 initialPos;
        private bool isMovingContinuously = false;
        private bool isRotatingContinuously = false;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float rotationInterpolationSpeed = 5f;



        private void Awake()
        {
            playerLocomotionInput = GetComponent<PlayerLocomotion>();
            playerState = GetComponent<PlayerState>();
            chatScript = FindObjectOfType<chatScript>();
            antiBump = sprintSpeed;
            stepOffset = characterController.stepOffset;
            initialPos = new Vector3();
            chatScript.isChatOpen += playerInputActive;
        }

        public void InitCurrentPlayer(int playerId)
        {
            isCurrentPlayer = true;
            if (playerLocomotionInput != null)
            {
                playerLocomotionInput.enabled = true;
            }

            try
            {
                playerData = new PlayerData
                {
                    id = playerId,
                    charId = 0, // Set appropriate character ID if needed
                    name = "Player " + playerId,
                    life = 100,
                    con = 1,
                    pos = new PlayerCoord
                    {
                        x = transform.position.x,
                        y = transform.position.y,
                        z = transform.position.z
                    },
                    rot = new PlayerCoord
                    {
                        x = transform.rotation.x,
                        y = transform.rotation.y,
                        z = transform.rotation.z,
                        w = transform.rotation.w
                    },
                    status = new PlayerStatus
                    {
                        move = 0,
                        rot = 0,
                        action = 0
                    }
                };
                networkManager.ChangePlayer(playerData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing PlayerData: {e.Message}");
            }
        }

        private void Update()
        {
            if (isChatActive) return;

            updateMovementState();
            HandleVerticalMovement();
            HandleLateralMovement();

            if (isMovingContinuously || isRotatingContinuously)
            {
                if (Time.time - lastNetworkUpdate >= networkUpdateRate)
                {
                    SendCurrentDataToServer();
                    lastNetworkUpdate = Time.time;
                }
            }

            
        }

        public void ShowCurrentPlayerCam()
        {
            playerCamera.gameObject.SetActive(true);
        }

        private void playerInputActive(bool state)
        {
            isChatActive = state;
        }

        public void updateMovementState()
        {
            lastMovementState = playerState.CurrentMovementState;
            PlayerMovementState oldState = playerState.CurrentMovementState;
            bool canRun = CanRun();
            bool isMoving = playerLocomotionInput.MovementInput != Vector2.zero;
            bool isMovingLaterally = IsMovingLaterally();
            bool isSprinting = playerLocomotionInput.sprintToggleOn && isMovingLaterally;
            bool isWalking = (isMovingLaterally && !canRun) || playerLocomotionInput.walkToggleOn;
            bool isGrounded = IsGrounded();


            PlayerMovementState lateralState = isWalking ? PlayerMovementState.Walking :
                                               isSprinting ? PlayerMovementState.Sprinting :
                                               isMovingLaterally || isMoving ? PlayerMovementState.Running : PlayerMovementState.Idling;

            playerState.SetPlayerMovementState(lateralState);

            if ((!isGrounded || jumpedLastFrame) && characterController.velocity.y > 0f)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                jumpedLastFrame = false;
                characterController.stepOffset = 0f;
            }
            else if ((!isGrounded || jumpedLastFrame) && characterController.velocity.y < 0f)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Falling);
                characterController.stepOffset = 0f;
            }
            else
            {
                characterController.stepOffset = stepOffset;
            }

            if(oldState != playerState.CurrentMovementState && isCurrentPlayer)
            {
                SendCurrentDataToServer();
            }
        }
        private void HandleLateralMovement()
        {
            Vector3 oldPosition = transform.position;
            bool isSprinting = playerState.CurrentMovementState == PlayerMovementState.Sprinting;
            bool isGrounded = playerState.inGroundedState();
            bool isWalking = playerState.CurrentMovementState == PlayerMovementState.Walking;

            float lateralAcceleration = !isGrounded ? inAirAcceleration :
                                        isWalking ? walkAcceleration :
                                        isSprinting ? sprintAcceleration : runAcceleration;
            float clampedLateralMagnitude = !isGrounded ? sprintSpeed :
                                            isWalking ? walkSpeed :
                                            isSprinting ? sprintSpeed : runSpeed;

            Vector3 cameraForwardXZ = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
            Vector3 cameraRightXZ = new Vector3(playerCamera.transform.right.x, 0f, playerCamera.transform.right.z).normalized;
            Vector3 movementDir = cameraRightXZ * playerLocomotionInput.MovementInput.x + cameraForwardXZ * playerLocomotionInput.MovementInput.y;
            Vector3 movementDelta = movementDir * lateralAcceleration * Time.deltaTime;
            Vector3 newVelocity = characterController.velocity + movementDelta;

            Vector3 currentDrag = newVelocity.normalized * drag * Time.deltaTime;
            newVelocity = (newVelocity.magnitude > drag * Time.deltaTime) ? newVelocity - currentDrag : Vector3.zero;
            newVelocity = Vector3.ClampMagnitude(new Vector3(newVelocity.x, 0f, newVelocity.z), clampedLateralMagnitude);
            newVelocity.y += verticalVelocity;
            newVelocity = !isGrounded ? HandleSteepWalls(newVelocity) : newVelocity;
            characterController.Move(newVelocity * Time.deltaTime);

            if (isCurrentPlayer && Vector3.Distance(oldPosition, transform.position) > 0.0001f)
            {
                isMovingContinuously = true;
            }
            else
            {
                isMovingContinuously = false;
            }
        }

        private Vector3 HandleSteepWalls(Vector3 velocity)
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= characterController.slopeLimit;

            if (!validAngle && verticalVelocity < 0f)
            {
                velocity = Vector3.ProjectOnPlane(velocity, normal);
            }

            return velocity;
        }

        private void HandleVerticalMovement()
        {
            if (isChatActive) return;
            bool isGrounded = playerState.inGroundedState();

            verticalVelocity -= gravity * Time.deltaTime;

            if (isGrounded && verticalVelocity < 0f)
            {
               
                verticalVelocity = -antiBump;
            }

            if (playerState.isStateGroundedState(lastMovementState) && !isGrounded)
            {
                verticalVelocity += antiBump;
            }

            if (Mathf.Abs(verticalVelocity) > Mathf.Abs(terminalVelocity))
            {
                verticalVelocity = Mathf.Sign(verticalVelocity) * terminalVelocity;
            }

            if (playerLocomotionInput.jumpPressed && isGrounded)
            {
                verticalVelocity = jumpSpeed;
                jumpedLastFrame = true;

                if (isCurrentPlayer)
                {
                    SendCurrentDataToServer();
                }
            }
        }

        private void LateUpdate()
        {
            if (isChatActive) return;

            if (isCurrentPlayer)
            {
                UpdateCameraRotation();
            }
        }

        private void UpdateCameraRotation()
        {
            Quaternion oldRotation = transform.rotation;
            Vector2 oldCameraRotation = cameraRotation;
            cameraRotation.x += lookSensH * playerLocomotionInput.LookInput.x;
            cameraRotation.y = Mathf.Clamp(cameraRotation.y - lookSensV * playerLocomotionInput.LookInput.y, -lookLimitv, lookLimitv);

            playerTargetRotation.x += transform.eulerAngles.x + lookSensH * playerLocomotionInput.LookInput.x;

            float rotationTolerance = 90f;
            bool isIdling = playerState.CurrentMovementState == PlayerMovementState.Idling;
            rotatingToTarget = rotatingToTargetTimer > 0;

            if (!isIdling)
            {
                RotatePlayerToTarget();
            }
            else if (Mathf.Abs(rotationMismatch) > rotationTolerance || rotatingToTarget)
            {
                UpdateIdleRotation(rotationTolerance);
            }

            playerCamera.transform.rotation = Quaternion.Euler(cameraRotation.y, cameraRotation.x, 0f);

            Vector3 cameraForwardProjectedXZ = new Vector3(playerCamera.transform.forward.x, 0f, playerCamera.transform.forward.z).normalized;
            Vector3 crossProduct = Vector3.Cross(transform.forward, cameraForwardProjectedXZ);
            float sign = Mathf.Sign(Vector3.Dot(crossProduct, transform.up));
            rotationMismatch = sign * Vector3.Angle(transform.forward, cameraForwardProjectedXZ);

            if (isCurrentPlayer &&
            (Quaternion.Angle(oldRotation, transform.rotation) > 20f))
            {
                isRotatingContinuously = true;
            }
            else
            {
                isRotatingContinuously = false;
            }
        }

        private void UpdateIdleRotation(float rotationTolerance)
        {
            if (Mathf.Abs(rotationMismatch) > rotationTolerance)
            {
                rotatingToTargetTimer = rotateToTargetTime;
                isRotatingClockwise = rotationMismatch > rotationTolerance;
            }

            rotatingToTargetTimer -= Time.deltaTime;

            if (isRotatingClockwise && rotationMismatch > 0f ||
                !isRotatingClockwise && rotationMismatch < 0f)
            {
                RotatePlayerToTarget();
            }

            if (isCurrentPlayer)
            {
                SendCurrentDataToServer();
            }
        }


        private void RotatePlayerToTarget()
        {
            Quaternion targetRotationX = Quaternion.Euler(0f, playerTargetRotation.x, 0f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotationX, playerRotationSpeed * Time.deltaTime);
        }

        private bool IsMovingLaterally()
        {
            Vector3 lateralVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.y);
            return lateralVelocity.magnitude > 0.01f;
        }

        private bool IsGrounded()
        {
            bool grounded = playerState.inGroundedState() ? isGroundedWhileGrounded() : isGroundedWhileAirbourne();
            return grounded;
        }

        private bool isGroundedWhileGrounded()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - characterController.radius, transform.position.z);

            bool grounded = Physics.CheckSphere(spherePosition, characterController.radius, groundLayers, QueryTriggerInteraction.Ignore);

            return grounded;
        }

        private bool isGroundedWhileAirbourne()
        {
            Vector3 normal = CharacterControllerUtils.GetNormalWithSphereCast(characterController, groundLayers);
            float angle = Vector3.Angle(normal, Vector3.up);
            bool validAngle = angle <= characterController.slopeLimit;

            return characterController.isGrounded && validAngle;
        }

        public bool CanRun()
        {
            return playerLocomotionInput.MovementInput.y >= Mathf.Abs(playerLocomotionInput.MovementInput.x);
        }
        // Updates to the UpdatePlayerData method in PlayerController.cs

        public void UpdatePlayerData()
        {
            // Update position and rotation
            playerData.pos.x = transform.position.x;
            playerData.pos.y = transform.position.y;
            playerData.pos.z = transform.position.z;

            playerData.rot.x = transform.rotation.x;
            playerData.rot.y = transform.rotation.y;
            playerData.rot.z = transform.rotation.z;
            playerData.rot.w = transform.rotation.w;

            // Update movement status with proper movement detection
            if (playerState.CurrentMovementState == PlayerMovementState.Idling)
            {
                playerData.status.move = 0;
            }
            else if (playerState.CurrentMovementState == PlayerMovementState.Running)
            {
                // Check movement direction for strafing
                float horizontalInput = playerLocomotionInput.MovementInput.x;
                float verticalInput = playerLocomotionInput.MovementInput.y;

                if (Mathf.Abs(horizontalInput) > 0.5f)
                {
                    playerData.status.move = horizontalInput > 0 ? 4 : 5; // Strafe right or left
                }
                else if (verticalInput < -0.5f)
                {
                    playerData.status.move = 6; // Walking backwards
                }
                else
                {
                    playerData.status.move = 1; // Running forward
                }
            }
            else if (playerState.CurrentMovementState == PlayerMovementState.Walking)
            {
                playerData.status.move = 2;
            }
            else if (playerState.CurrentMovementState == PlayerMovementState.Sprinting)
            {
                playerData.status.move = 3;
            }

            // Update rotation status with more accurate rotation detection
            if (isRotatingContinuously)
            {
                if (Mathf.Abs(rotationMismatch) > 5f) // More sensitive threshold
                {
                    playerData.status.rot = rotationMismatch > 0 ? 1 : 2; // 1 for right, 2 for left
                }
                else
                {
                    playerData.status.rot = 0;
                }
            }
            else
            {
                playerData.status.rot = 0;
            }

            // Update action status with proper state handling
            if (playerState.CurrentMovementState == PlayerMovementState.Jumping)
            {
                playerData.status.action = 1;
            }
            else if (playerState.CurrentMovementState == PlayerMovementState.Falling)
            {
                playerData.status.action = 2;
            }
            else
            {
                playerData.status.action = 0;
            }
        }
        public void SendCurrentDataToServer()
        {
            if (isCurrentPlayer)
            {
                UpdatePlayerData();
                networkManager.ChangePlayer(playerData);
            }
        }

        public void ApplyReceivedPlayerData(PlayerData receivedData)
        {
            if (!isCurrentPlayer)
            {
                // Completely disable camera rotation updates for non-current players
                if (playerCamera != null && !isCurrentPlayer)
                {
                    playerCamera.transform.rotation = Quaternion.LookRotation(
                        transform.forward,
                        Vector3.up
                    );
                }

                // Update position and rotation
                targetPosition = new Vector3(receivedData.pos.x, receivedData.pos.y, receivedData.pos.z);
                targetRotation = new Quaternion(receivedData.rot.x, receivedData.rot.y,
                                              receivedData.rot.z, receivedData.rot.w);

                // Smoothly interpolate position and rotation
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

                // Update movement state based on all possible movement types
                switch (receivedData.status.move)
                {
                    case 1: // Running forward
                        playerState.SetPlayerMovementState(PlayerMovementState.Running);
                        if (playerLocomotionInput != null)
                        {
                            playerLocomotionInput.MovementInput = new Vector2(0, 1); // Forward
                        }
                        break;

                    case 2: // Walking
                        playerState.SetPlayerMovementState(PlayerMovementState.Walking);
                        if (playerLocomotionInput != null)
                        {
                            playerLocomotionInput.MovementInput = new Vector2(0, 0.5f); // Forward at walking speed
                        }
                        break;

                    case 3: // Sprinting
                        playerState.SetPlayerMovementState(PlayerMovementState.Sprinting);
                        if (playerLocomotionInput != null)
                        {
                            playerLocomotionInput.MovementInput = new Vector2(0, 1); // Forward at sprint speed
                        }
                        break;

                    case 4: // Strafing right
                        playerState.SetPlayerMovementState(PlayerMovementState.Running);
                        if (playerLocomotionInput != null)
                        {
                            playerLocomotionInput.MovementInput = new Vector2(1, 0); // Right
                        }
                        break;

                    case 5: // Strafing left
                        playerState.SetPlayerMovementState(PlayerMovementState.Running);
                        if (playerLocomotionInput != null)
                        {
                            playerLocomotionInput.MovementInput = new Vector2(-1, 0); // Left
                        }
                        break;

                    case 6: // Walking backwards
                        playerState.SetPlayerMovementState(PlayerMovementState.Walking);
                        if (playerLocomotionInput != null)
                        {
                            playerLocomotionInput.MovementInput = new Vector2(0, -0.5f); // Backward
                        }
                        break;

                    default: // Idle
                        playerState.SetPlayerMovementState(PlayerMovementState.Idling);
                        if (playerLocomotionInput != null)
                        {
                            playerLocomotionInput.MovementInput = Vector2.zero;
                        }
                        break;
                }

                // Update action state
                switch (receivedData.status.action)
                {
                    case 1:
                        playerState.SetPlayerMovementState(PlayerMovementState.Jumping);
                        break;
                    case 2:
                        playerState.SetPlayerMovementState(PlayerMovementState.Falling);
                        break;
                }

                // Get and update the animation component
                PlayerAnimation remoteAnimator = GetComponent<PlayerAnimation>();
                if (remoteAnimator != null)
                {
                    remoteAnimator.UpdateRemoteAnimationFromData(receivedData);
                }
                else
                {
                    Debug.LogError("PlayerAnimation component not found on the player object.");
                }

                // Ensure player is visible if connected
                if (receivedData.con == 1 && !gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }
            }
        }
        public void HandleMovementInput(Vector2 input)
        {
            if (!isCurrentPlayer) return;

            updateMovementState();
            HandleLateralMovement();
        }

        public void HandleLookInput(Vector2 input)
        {
            if (!isCurrentPlayer) return;

            playerLocomotionInput.LookInput = input;
            UpdateCameraRotation();
        }

        public void HandleJumpInput()
        {
            if (!isCurrentPlayer) return;

            playerLocomotionInput.jumpPressed = true;
            Debug.Log($"Jump input received: {playerLocomotionInput.jumpPressed}");
            HandleVerticalMovement();
            playerLocomotionInput.jumpPressed = false;
        }

        public void HandleSprintInput(bool isSprintPressed)
        {
            if (!isCurrentPlayer) return;

            bool oldSprint = playerLocomotionInput.sprintToggleOn;
            playerLocomotionInput.sprintToggleOn = isSprintPressed;

            if (oldSprint != isSprintPressed)
            {
                SendCurrentDataToServer();
            }

            updateMovementState();
        }

        public void HandleWalkInput(bool isWalkPressed)
        {
            if (!isCurrentPlayer) return;

            playerLocomotionInput.walkToggleOn = isWalkPressed;
            updateMovementState();
        }

        private void OnDestroy()
        {
            chatScript.isChatOpen -= playerInputActive;
        }
    }

}


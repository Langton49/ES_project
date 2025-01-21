using System.Collections;
using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoonshineStudios.CharacterInputController
{
    [DefaultExecutionOrder(-2)]
    public class ThirdPersonInput : MonoBehaviour, PlayerControls.IThirdPersonMapActions
    {
        public Vector2 ScrollInput { get; private set; }

        [SerializeField] private CinemachineVirtualCamera cinemachineVirtualCamera;
        [SerializeField] private float cameraZoomSpeed = 0.1f;
        [SerializeField] private float cameraMaxZoom = 5f;
        [SerializeField] private float cameraMinZoom = 1f;

        private Cinemachine3rdPersonFollow thirdPersonFollow;

        private void Awake()
        {
            thirdPersonFollow = cinemachineVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        }

        private void Update()
        {
            thirdPersonFollow.CameraDistance = Mathf.Clamp(thirdPersonFollow.CameraDistance + ScrollInput.y, cameraMinZoom, cameraMaxZoom);
        }

        private void LateUpdate()
        {
            ScrollInput = Vector2.zero;
        }

        private void OnEnable()
        {
            if (PlayerInputManager.Instance?.playerControls == null)
            {
                Debug.LogError("Player Controls not found");
                return;
            }
            PlayerInputManager.Instance.playerControls.ThirdPersonMap.Enable();
            PlayerInputManager.Instance.playerControls.ThirdPersonMap.SetCallbacks(this);

        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance?.playerControls == null)
            {
                Debug.LogError("Player Controls not found");
            }
            PlayerInputManager.Instance.playerControls.ThirdPersonMap.Disable();
            PlayerInputManager.Instance.playerControls.ThirdPersonMap.RemoveCallbacks(this);
        }

        public void OnScrollCamera(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Vector2 scrollInput = context.ReadValue<Vector2>();
            ScrollInput = -1f * scrollInput.normalized * cameraZoomSpeed;
        }
    }
}


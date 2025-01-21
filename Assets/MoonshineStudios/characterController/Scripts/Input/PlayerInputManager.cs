using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoonshineStudios.CharacterInputController
{
    [DefaultExecutionOrder(-3)]
    public class PlayerInputManager : MonoBehaviour
    {
        public static PlayerInputManager Instance;
        public PlayerControls playerControls { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            playerControls = new PlayerControls();
            playerControls.Enable();
        }

        private void OnDisable() 
        {
            playerControls.Disable();
        } 
        
    }

}

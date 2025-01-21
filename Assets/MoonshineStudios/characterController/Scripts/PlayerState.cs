using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoonshineStudios.CharacterInputController
{
    public class PlayerState : MonoBehaviour
    {
        [field: SerializeField] public PlayerMovementState CurrentMovementState { get; private set; } = PlayerMovementState.Idling;

        public void SetPlayerMovementState(PlayerMovementState playerMovementState)
        {
            CurrentMovementState = playerMovementState;
        }

        public bool inGroundedState()
        {
            return isStateGroundedState(CurrentMovementState);
        }

        public bool isStateGroundedState(PlayerMovementState state)
        {
            return state == PlayerMovementState.Idling ||
                   state == PlayerMovementState.Running ||
                   state == PlayerMovementState.Sprinting ||
                   state == PlayerMovementState.Walking ||
                   state == PlayerMovementState.StrafingLeft ||
                   state == PlayerMovementState.StrafingRight ||
                   state == PlayerMovementState.WalkingBackwards;
        }

        public bool IsMovingLaterally()
        {
            return CurrentMovementState == PlayerMovementState.Running ||
                   CurrentMovementState == PlayerMovementState.Sprinting ||
                   CurrentMovementState == PlayerMovementState.Walking ||
                   CurrentMovementState == PlayerMovementState.StrafingLeft ||
                   CurrentMovementState == PlayerMovementState.StrafingRight ||
                   CurrentMovementState == PlayerMovementState.WalkingBackwards;
        }

        public bool IsStrafing()
        {
            return CurrentMovementState == PlayerMovementState.StrafingLeft ||
                   CurrentMovementState == PlayerMovementState.StrafingRight;
        }

        public bool IsRotating()
        {
            return CurrentMovementState == PlayerMovementState.RotatingLeft ||
                   CurrentMovementState == PlayerMovementState.RotatingRight;
        }
    }

    public enum PlayerMovementState
    {
        Idling = 0,
        Running = 1,
        Sprinting = 2,
        Jumping = 3,
        Falling = 4,
        Walking = 5,
        StrafingLeft = 6,
        StrafingRight = 7,
        WalkingBackwards = 8,
        RotatingLeft = 9,
        RotatingRight = 10
    }
}
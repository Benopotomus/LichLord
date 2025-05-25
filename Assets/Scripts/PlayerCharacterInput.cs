using UnityEngine;
using UnityEngine.InputSystem;

namespace LichLord
{
    public struct GameplayInput
    {
        public Vector2 LookRotation;
        public Vector2 MoveDirection;
        public bool Jump;
        public bool JumpHeld;
        public bool Crouch;
        public bool CrouchHeld;
        public bool Fire; // Triggers selected action (e.g., melee or spell)
        public bool Sprint;
        public bool ToggleCameraView;
        public int ActionSelection; // Index of selected action (0 for none, 1-9 for actions)
    }

    public class PlayerCharacterInput : MonoBehaviour
    {
        public GameplayInput CurrentInput => _input;
        private GameplayInput _input;

        private PlayerControls _controls;

        private void Awake()
        {
            _controls = new PlayerControls();
            _controls.Gameplay.Action1.performed += _ => _input.ActionSelection = 1;
            _controls.Gameplay.Action2.performed += _ => _input.ActionSelection = 2;
            _controls.Gameplay.Action3.performed += _ => _input.ActionSelection = 3;
            _controls.Gameplay.Action4.performed += _ => _input.ActionSelection = 4;
            _controls.Gameplay.Action5.performed += _ => _input.ActionSelection = 5;
            _controls.Gameplay.Action6.performed += _ => _input.ActionSelection = 6;
            _controls.Gameplay.Action7.performed += _ => _input.ActionSelection = 7;
            _controls.Gameplay.Action8.performed += _ => _input.ActionSelection = 8;
            _controls.Gameplay.Action9.performed += _ => _input.ActionSelection = 9;
        }

        private void OnEnable() => _controls.Enable();
        private void OnDisable() => _controls.Disable();

        public void ResetInput()
        {
            // Reset one-frame inputs, preserve JumpHeld
            _input.Jump = false;
            _input.Crouch = false;
            _input.Fire = false;
            _input.Sprint = false;
            _input.ToggleCameraView = false;

        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            _input.MoveDirection = _controls.Gameplay.Move.ReadValue<Vector2>();

            Vector2 rawLook = _controls.Gameplay.Look.ReadValue<Vector2>();
            float lookSensitivity = 0.25f;
            _input.LookRotation += new Vector2(-rawLook.y, rawLook.x) * lookSensitivity;
            _input.LookRotation = ClampLookRotation(_input.LookRotation);

            _input.Jump |= _controls.Gameplay.Jump.WasPressedThisFrame();
            _input.JumpHeld = _controls.Gameplay.Jump.IsPressed();
            _input.Crouch |= _controls.Gameplay.Crouch.WasPressedThisFrame();
            _input.CrouchHeld = _controls.Gameplay.Crouch.IsPressed();
            _input.Fire |= _controls.Gameplay.Fire.WasPressedThisFrame();
            _input.Sprint = _controls.Gameplay.Sprint.IsPressed();
            _input.ToggleCameraView = _controls.Gameplay.CameraViewSwitch.WasPressedThisFrame();
        }

        public Vector2 ClampLookRotation(Vector2 lookRotation)
        {
            lookRotation.x = Mathf.Clamp(lookRotation.x, -30f, 70f);
            return lookRotation;
        }
    }
}
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
        public bool Fire;
        public bool Sprint;
        public bool ToggleCameraView;
    }

    public class PlayerCharacterInput : MonoBehaviour
    {
        public GameplayInput CurrentInput => _input;
        private GameplayInput _input;

        private PlayerControls _controls;

        private void Awake()
        {
            _controls = new PlayerControls();
        }

        private void OnEnable() => _controls.Enable();
        private void OnDisable() => _controls.Disable();

        public void ResetInput()
        {
            // Only reset one-frame inputs, preserve JumpHeld
            _input.Jump = false;
            _input.Crouch = false;
            _input.Fire = false;
            _input.Sprint = false;
            // Debug.Log($"[PlayerCharacterInput] ResetInput called, JumpHeld preserved: {_input.JumpHeld}");
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                // Debug.Log($"[PlayerCharacterInput] Cursor not locked, skipping input for {gameObject.name}");
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

            if (_input.JumpHeld)
            {
                Debug.Log($"[PlayerCharacterInput] Jump key held for {gameObject.name}, JumpHeld: {_input.JumpHeld}");
            }
        }

        public Vector2 ClampLookRotation(Vector2 lookRotation)
        {
            lookRotation.x = Mathf.Clamp(lookRotation.x, -30f, 70f);
            return lookRotation;
        }
    }
}
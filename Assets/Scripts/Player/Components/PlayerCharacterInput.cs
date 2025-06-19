using UnityEngine;
using UnityEngine.InputSystem;

namespace LichLord
{
    public class PlayerCharacterInput : MonoBehaviour
    {
        public FGameplayInput CurrentInput => _input;
        public FGameplayInput PreviousInput { get; private set; }

        private FGameplayInput _input;
        private PlayerControls _controls;

        private void Awake()
        {
            _controls = new PlayerControls();
            _input = new FGameplayInput { ActionSelection = 0 }; // Default to 0 per reset requirement
            Debug.Log($"[PlayerCharacterInput] Initialized ActionSelection={_input.ActionSelection}");

            // Bind Action1 to Action9 dynamically
            InputAction[] actions = new[]
            {
                _controls.Gameplay.Action1, _controls.Gameplay.Action2, _controls.Gameplay.Action3,
                _controls.Gameplay.Action4, _controls.Gameplay.Action5, _controls.Gameplay.Action6,
                _controls.Gameplay.Action7, _controls.Gameplay.Action8, _controls.Gameplay.Action9
            };

            for (int i = 0; i < actions.Length; i++)
            {
                int actionIndex = i + 1; // 1-based index for ActionSelection
                actions[i].performed += _ =>
                {
                    _input.ActionSelection = actionIndex;
                    Debug.Log($"[PlayerCharacterInput] Action{actionIndex} performed, ActionSelection={_input.ActionSelection}");
                };
            }
        }

        private void OnEnable() => _controls.Enable();
        private void OnDisable() => _controls.Disable();

        public void ResetInput()
        {
            // Reset one-frame inputs, preserve JumpHeld, ActionSelection, and ScrollDelta
            _input.Jump = false;
            _input.Crouch = false;
            _input.Fire = false;
            _input.FireHeld = false;
            _input.Sprint = false;
            _input.ToggleCameraView = false;
            _input.ScrollDelta = 0f;
            _input.ActionSelection = 0;
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            // Movement and lookZ
            _input.MoveDirection = _controls.Gameplay.Move.ReadValue<Vector2>();

            Vector2 rawLook = _controls.Gameplay.Look.ReadValue<Vector2>();
            const float lookSensitivity = 0.25f;
            _input.LookRotation += new Vector2(-rawLook.y, rawLook.x) * lookSensitivity;
            _input.LookRotation = ClampLookRotation(_input.LookRotation);

            // Button inputs
            _input.Jump |= _controls.Gameplay.Jump.WasPressedThisFrame();
            _input.JumpHeld = _controls.Gameplay.Jump.IsPressed();
            _input.Crouch |= _controls.Gameplay.Crouch.WasPressedThisFrame();
            _input.CrouchHeld = _controls.Gameplay.Crouch.IsPressed();
            _input.Fire |= _controls.Gameplay.Fire.WasPressedThisFrame();
            _input.FireHeld |= _controls.Gameplay.Fire.IsPressed();
            _input.Sprint = _controls.Gameplay.Sprint.IsPressed();
            _input.ToggleCameraView |= _controls.Gameplay.CameraViewSwitch.WasPressedThisFrame();

            // Scroll input
            if (_controls.Gameplay.Scroll.WasPerformedThisFrame())
            {
                float scrollY = _controls.Gameplay.Scroll.ReadValue<Vector2>().y;
                if (scrollY != 0)
                {
                    _input.ScrollDelta = scrollY;
                }
            }
        }

        private Vector2 ClampLookRotation(Vector2 lookRotation)
        {
            lookRotation.x = Mathf.Clamp(lookRotation.x, -30f, 70f);
            return lookRotation;
        }
    }
}
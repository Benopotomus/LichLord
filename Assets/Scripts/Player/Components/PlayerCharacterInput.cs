using LichLord.Buildables;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LichLord
{
    public class PlayerCharacterInput : ContextBehaviour
    {
        public FGameplayInput CurrentInput => _input;
        public FGameplayInput PreviousInput { get; private set; }

        private FGameplayInput _input;
        private PlayerControls _controls;

        public void OnSpawned()
        {
            _controls = new PlayerControls();
            _input = new FGameplayInput { ActionSelection = 0 };

            // Bind Action1 to Action9
            InputAction[] actions = new[]
            {
                _controls.Gameplay.Action1, _controls.Gameplay.Action2, _controls.Gameplay.Action3,
                _controls.Gameplay.Action4, _controls.Gameplay.Action5, _controls.Gameplay.Action6,
                _controls.Gameplay.Action7, _controls.Gameplay.Action8, _controls.Gameplay.Action9
            };

            for (int i = 0; i < actions.Length; i++)
            {
                int actionIndex = i + 1; // 1-based
                actions[i].performed += _ =>
                {
                    _input.ActionSelection = actionIndex;
                };
            }

            // Bind BuildCategory1 to BuildCategory4
            InputAction[] buildCategories = new[]
            {
                _controls.Gameplay.BuildCategory1,
                _controls.Gameplay.BuildCategory2,
                _controls.Gameplay.BuildCategory3,
                _controls.Gameplay.BuildCategory4
            };

            for (int i = 0; i < buildCategories.Length; i++)
            {
                int categoryIndex = i + 1; // matches EBuildableCategory
                buildCategories[i].performed += _ =>
                {
                    _input.BuildCategory = (EBuildableCategory)categoryIndex;
                };
            }

            _controls.Enable();
        }

        //private void OnEnable() => _controls.Enable();
        //private void OnDisable() => _controls.Disable();

        // Called from the save/load
        public void SetLookRotation(Quaternion rotation)
        {
            _input.LookRotation = new Vector2(0, rotation.eulerAngles.y);
        }

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
            _input.BuildMode = false;
            _input.DeleteMode = false;
            _input.Interact = false;
        }

        private void Update()
        {
            if (!HasStateAuthority)
                return;

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
            _input.BuildMode |= _controls.Gameplay.BuildMode.WasPressedThisFrame();
            _input.DeleteMode |= _controls.Gameplay.DeleteMode.WasPressedThisFrame();
            _input.Interact |= _controls.Gameplay.Interact.WasPressedThisFrame();

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
            lookRotation.x = Mathf.Clamp(lookRotation.x, -60f, 70f);
            return lookRotation;
        }
    }
}
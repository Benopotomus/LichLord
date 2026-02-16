using LichLord.Buildables;
using UnityEngine;

namespace LichLord
{
    using UnityEngine;
    using Fusion;

    public enum EGameplayInputAction
    {
        Fire = 0,
        AltFire = 1,
        MMB = 2,
        Jump = 3,
        Crouch = 4,
        Sprint = 5,
        Interact = 6,

    }

    public struct FGameplayInput : INetworkInput
    {
        public Vector2 LookDelta;
        public Vector2 MoveDirection;
        public bool Jump;
        public bool JumpHeld;
        public bool Crouch;
        public bool CrouchHeld;
        public bool Fire;
        public bool FireHeld;
        public bool AltFire;
        public bool AltFireHeld;

        public bool PlaceBuildable;
        public bool RotateBuildableYaw;
        public bool RotateBuildablePitch;

        public bool Sprint;
        public bool ToggleCameraView;
        public int ActionSelection; // Index of selected action (1-9 for keys, 0 for none)
        public float ScrollDelta; // Mouse wheel delta (positive: up, negative: down)
        public bool BuildMode; // Toggles build mode
        public bool SummonMode;
        public EBuildableCategory BuildCategory;
        public bool DeleteMode;
        public bool Interact;
        public bool SwapWeapon;
        public bool ShowTooltips;
        public bool InventoryToggle;
        public bool Cancel;
        public bool UI_Interact; // Interact while UI is open.
        public bool UI_DebugConsole;
    }
}
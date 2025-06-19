using UnityEngine;

namespace LichLord
{
    public struct FGameplayInput
    {
        public Vector2 LookRotation;
        public Vector2 MoveDirection;
        public bool Jump;
        public bool JumpHeld;
        public bool Crouch;
        public bool CrouchHeld;
        public bool Fire;
        public bool FireHeld;
        public bool Sprint;
        public bool ToggleCameraView;
        public int ActionSelection; // Index of selected action (1-9 for keys, 0 for none)
        public float ScrollDelta; // Mouse wheel delta (positive: up, negative: down)
    }
}
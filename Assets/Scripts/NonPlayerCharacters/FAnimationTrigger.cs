using System;

namespace LichLord
{
    [Serializable]
    public struct FAnimationTrigger
    {
        public int Action;
        public int Weapon;
        public int TriggerNumber;
        public bool IsMoving;
        public bool IsBlocking;
    }
}
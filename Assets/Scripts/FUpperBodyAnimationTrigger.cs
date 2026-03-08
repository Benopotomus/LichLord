using System;
using UnityEngine;

namespace LichLord
{
    [Serializable]
    public struct FUpperBodyAnimationTrigger
    {
        public int UpperbodyTriggerNumber;

        public int UpperbodyTriggerDuration;
        public float PitchOffset;
        public float YawOffset;
        public float RollOffset;

        [Header("Forearm Offset (Euler Degrees)")]
        public Vector3 UpperArmOffsetEuler;
        public Vector3 LowerArmOffsetEuler;

    }
}

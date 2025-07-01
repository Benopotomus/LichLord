using LichLord.Projectiles;
using System;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public struct FAnimationCallbackData
    {
        public string AnimationStateName;
        public int TotalFrames;
        public int Frame;
    }
}

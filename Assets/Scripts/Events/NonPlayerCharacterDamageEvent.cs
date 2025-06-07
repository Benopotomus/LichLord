using FusionHelpers;
using UnityEngine;

namespace LichLord
{
    public struct NonPlayerCharacterDamageEvent : INetworkEvent
    {
        public int guid;
        public Vector3 impulse;
        public int damage;
    }
}

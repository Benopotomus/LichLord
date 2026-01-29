using LichLord.Projectiles;
using System;
using UnityEngine;

namespace LichLord
{
    [Serializable]
    public struct FManeuverAction
    {
        public int SpawnTick;
        public ManeuverActionDefinition Definition;
    }
}

using LichLord.Projectiles;
using System;
using UnityEngine;

namespace LichLord
{
    [Serializable]
    public struct FManeuverProjectile
    {
        public int SpawnTick;
        public ProjectileDefinition Definition;
        public EMuzzle Muzzle;
        public FDamagePotential Damage;
        public Vector2 AimOffset;
    }
}

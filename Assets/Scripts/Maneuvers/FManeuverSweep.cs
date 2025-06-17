using LichLord.Projectiles;
using System;
using UnityEngine;

namespace LichLord
{
    [Serializable]
    public struct FManeuverSweep
    {
        public int SpawnTick;
        public EMuzzle Muzzle;
        public LayerMask HitMask; // Layers to hit for gun/spell raycasts
        public EShapeType Shape;
        public Vector3 Extents;
    }
}

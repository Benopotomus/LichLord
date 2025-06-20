using LichLord.Projectiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord
{
    static class MuzzleUtility
    {
        public static Vector3 GetMuzzlePosition(INetActor actor, EMuzzle muzzle)
        {
            if (actor is PlayerCharacter pc)
            {
                return pc.GetMuzzlePosition(muzzle);
            }

            return Vector3.zero;
        
        }
    }
}

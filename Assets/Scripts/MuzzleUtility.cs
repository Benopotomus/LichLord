using LichLord.NonPlayerCharacters;
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
                return pc.Muzzle.GetMuzzlePosition(muzzle);
            }

            if (actor is NonPlayerCharacter npc)
            {
                return npc.Muzzle.GetMuzzlePosition(muzzle);
            }

            return Vector3.zero;
        
        }
    }
}

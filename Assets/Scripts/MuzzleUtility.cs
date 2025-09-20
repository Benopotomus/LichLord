using LichLord.NonPlayerCharacters;
using LichLord.Projectiles;
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

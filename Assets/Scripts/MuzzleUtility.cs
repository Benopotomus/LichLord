using LichLord.NonPlayerCharacters;
using LichLord.Projectiles;
using UnityEngine;

namespace LichLord
{
    static class MuzzleUtility
    {
        public static Vector3 GetMuzzlePosition(PlayerCharacter pc, EMuzzle muzzle)
        {
            return pc.Weapons.GetMuzzlePosition(muzzle);
        }

        public static Transform GetMuzzleTransform(PlayerCharacter pc, EMuzzle muzzle)
        {
            return pc.Weapons.GetMuzzleTransform(muzzle);
        }

        public static Vector3 GetMuzzlePosition(NonPlayerCharacter npc, EMuzzle muzzle)
        {
            return npc.Weapons.GetMuzzlePosition(muzzle);
        }

        public static Vector3 GetMuzzlePosition(IHitInstigator instigator, EMuzzle muzzle)
        {
            if(instigator is NonPlayerCharacter npc)
                return npc.Weapons.GetMuzzlePosition(muzzle);

            if (instigator is PlayerCharacter pc)
                return pc.Weapons.GetMuzzlePosition(muzzle);

            return Vector3.zero;
        }

        public static Transform GetMuzzleTransform(IHitInstigator instigator, EMuzzle muzzle)
        {
            if (instigator is NonPlayerCharacter npc)
                return npc.transform;

            if (instigator is PlayerCharacter pc)
                return pc.Weapons.GetMuzzleTransform(muzzle);

            return null;
        }
    }
}

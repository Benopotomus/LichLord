
using DWD.Utility.Loading;
using LichLord.World;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public abstract class NonPlayerCharacterManeuverHitEvent : ScriptableObject
    {
        public virtual void Execute(NonPlayerCharacter npc, 
            NonPlayerCharacterManeuverDefinition definition, 
            IChunkTrackable target)
        {
        }
    }
}

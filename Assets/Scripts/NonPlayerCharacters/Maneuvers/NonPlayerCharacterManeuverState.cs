using System;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public class NonPlayerCharacterManeuverState
    {
        public NonPlayerCharacterManeuverDefinition Definition;
        public ENonPlayerState ActiveState = ENonPlayerState.Maneuver_1;
        public float CooldownExpirationTick;
        public float ActivationExpirationTick;

        public bool IsValid()
        {
            if (Definition == null)
                return false;

            return true;
        }

        public bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            if(Definition == null) 
                return false;
            
            if (IsOnCooldown(tick))
                return false;

            if(Definition.RequiresEnemyTarget && brainComponent.AttackTarget == null)
                return false;

            return true;
        }

        public bool IsOnCooldown(int tick)
        {
            return CooldownExpirationTick > tick;
        }

        public bool HasExpired(int tick)
        { 
            return ActivationExpirationTick < tick;
        }

        public bool ExecuteManeuver(NonPlayerCharacter npc, 
            ref FNonPlayerCharacterData data, 
            int tick)
        {
            if (data.State != ENonPlayerState.Idle)
                return false;

            if(IsOnCooldown(tick)) 
                return false;

            data.State = ActiveState;

            int currentAnimIndex = data.AnimationIndex;
            int newAnimIndex = UnityEngine.Random.Range(0, 4);

            // If the new index is the same as the current, increment and wrap around
            if (newAnimIndex == currentAnimIndex)
            {
                newAnimIndex = (currentAnimIndex + 1) % 4;
            }

            data.AnimationIndex = newAnimIndex;

            npc.Replicator.UpdateNPCData(data);
            CooldownExpirationTick = tick + Definition.CooldownTicks;
            ActivationExpirationTick = tick + Definition.StateTicks;
            return true;
        }
    }
}

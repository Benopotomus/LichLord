using System;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public class NonPlayerCharacterManeuverState
    {
        public NonPlayerCharacterManeuverDefinition Definition;
        public ENonPlayerState ActiveState = ENonPlayerState.Maneuver_1;
        public float CooldownTimer;
        public float ManeuverAnimationTimer;

        public bool IsValid()
        {
            if (Definition == null)
                return false;

            return true;
        }

        public bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent)
        {
            if(Definition == null) 
                return false;
            
            if (IsOnCooldown())
                return false;

            if(Definition.RequiresEnemyTarget && brainComponent.AttackTarget == null)
                return false;

            return true;
        }

        public bool IsOnCooldown()
        {
            return CooldownTimer > 0;
        }

        public void UpdateCooldownTimer(float renderDeltaTime)
        {
            if(!IsOnCooldown()) 
                return;

            CooldownTimer -= renderDeltaTime;
        }

        public bool HasExpired()
        { 
            return ManeuverAnimationTimer < 0;
        }

        // has expired
        public bool UpdateStateTimer(NonPlayerCharacter npc, ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            if (npc.State.CurrentState == ActiveState)
            {
                ManeuverAnimationTimer -= renderDeltaTime;
            }

            return false;
        }

        public bool ExecuteManeuver(NonPlayerCharacter npc, ref FNonPlayerCharacterData data)
        {
            if (data.State != ENonPlayerState.Idle)
                return false;

            if(IsOnCooldown()) 
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
            CooldownTimer = Definition.Cooldown;
            ManeuverAnimationTimer = Definition.StateTime;
            return true;
        }
    }
}

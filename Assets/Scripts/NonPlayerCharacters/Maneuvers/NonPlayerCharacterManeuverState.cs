using System;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [Serializable]
    public class NonPlayerCharacterManeuverState
    {
        public NonPlayerCharacterManeuverDefinition Definition;
        public ENonPlayerState ActiveState = ENonPlayerState.Maneuver_1;
        public float CooldownTimer;
        public float ManeuverAnimationTimer;

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

        public void UpdateStateTimer(NonPlayerCharacter npc, ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            if (npc.State.CurrentState == ActiveState)
            {
                ManeuverAnimationTimer -= renderDeltaTime;
                if (ManeuverAnimationTimer < 0)
                {
                    data.State = ENonPlayerState.Idle;
                    npc.Replicator.UpdateNPCData(data);
                }
            }
        }

        public bool ExecuteManeuver(NonPlayerCharacter npc, ref FNonPlayerCharacterData data)
        {
            if (data.State != ENonPlayerState.Idle)
                return false;

            if(IsOnCooldown()) 
                return false;

            data.State = ActiveState;
            data.AnimationIndex = UnityEngine.Random.Range(0, Definition.AnimationTriggers.Count);
            npc.Replicator.UpdateNPCData(data);
            CooldownTimer = Definition.Cooldown;
            ManeuverAnimationTimer = Definition.StateTime;
            return true;
        }
    }
}

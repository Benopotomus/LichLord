using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    [CreateAssetMenu(menuName = "LichLord/NonPlayerCharacters/InvaderDataDefinition")]
    public class InvaderDataDefinition : NonPlayerCharacterDataDefinition
    {
        // Config (7 from base)


        // Events
        private const int HEALTH_BITS = 12;             // 0–4095
        private const int HEALTH_SHIFT = 0;
        private const ushort HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        public override void InitializeData(ref FNonPlayerCharacterData npcData, 
            NonPlayerCharacterDefinition definition, 
            ENPCSpawnType spawnType,
            ETeamID teamID,
            EAttitude attitude)
        {
            base.InitializeData(ref npcData, definition, spawnType, teamID, attitude);


            // Initialize Events
            npcData.Events = 0;
            SetHealth(definition.MaxHealth, ref npcData);
        }


        // Invasion NPC
        public bool IsInvasionNPC(ref FNonPlayerCharacterData npcData)
        {
            return true;
        }

        // Health
        public override int GetHealth(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Events >> HEALTH_SHIFT) & HEALTH_MASK;
        }

        public override void SetHealth(int newHealth, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            newHealth = Mathf.Clamp(newHealth, 0, HEALTH_MASK);
            events = (ushort)((events & ~(HEALTH_MASK << HEALTH_SHIFT)) | (newHealth << HEALTH_SHIFT));
            npcData.Events = events;
        }

        // Handle damage application
        public override void ApplyDamage(
            ref FNonPlayerCharacterData npcData,
            int damage, 
            int hitReactIndex)
        {
            NonPlayerCharacterDefinition definition = npcData.Definition;

            int currentHealth = GetHealth(ref npcData);
            damage = Mathf.Max(damage - definition.DamageReduction, 0);
            damage = (int)((float)damage * (1.0f - definition.DamageResistance));

            SetHealth(currentHealth - damage, ref npcData);

            if (GetHealth(ref npcData) == 0)
            {
                SetState(TryAssignState(ref npcData, ENPCState.Dead), ref npcData);
            }
            else
            {
                SetState(TryAssignState(ref npcData, ENPCState.HitReact), ref npcData);
                SetAnimationIndex(hitReactIndex, ref npcData);
            }
        }
    }
}

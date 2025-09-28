using UnityEngine;

//14 bits

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "DestructibleBuildableDataDefinition", menuName = "LichLord/Buildables/DestructibleBuildableDataDefinition")]
    public class DestructibleBuildableDataDefinition : BuildableDataDefinition
    {
        protected const int HEALTH_BITS = 10;         // 0-1023

        protected const int HEALTH_SHIFT = STATE_SHIFT + STATE_BITS;

        protected const int HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        public override void InitializeData(ref FBuildableData buildableData, BuildableDefinition definition)
        {
            // Initialize fields
            buildableData.DefinitionID = (ushort)definition.TableID; // Assuming definition has an ID
            buildableData.StateData = 0;

            // Set initial values
            SetState(StartingState, ref buildableData);
            SetHealth(definition.MaxHealth, ref buildableData); // Default health, adjust as needed
        }

        // Health
        public int GetHealth(ref FBuildableData buildableData)
        {
            return (buildableData.StateData >> HEALTH_SHIFT) & HEALTH_MASK;
        }

        public void SetHealth(int health, ref FBuildableData buildableData)
        {
            int stateData = buildableData.StateData;
            health = Mathf.Clamp(health, 0, HEALTH_MASK);
            stateData = (stateData & ~(HEALTH_MASK << HEALTH_SHIFT)) | (health << HEALTH_SHIFT);
            buildableData.StateData = stateData;
        }

        // Handle damage application
        public void ApplyDamage(ref FBuildableData buildableData, int damage)
        {
            var definition = Global.Tables.BuildableTable.TryGetDefinition(buildableData.DefinitionID);

            int currentHealth = GetHealth(ref buildableData);
            damage = Mathf.Max(damage - definition.DamageReduction, 0);
            damage = (int)((float)damage * (1.0f - definition.DamageResistance));

            SetHealth(currentHealth - damage, ref buildableData);

            if (GetHealth(ref buildableData) <= 0)
            {
                SetState(TryAssignState(ref buildableData, EBuildableState.Destroyed), ref buildableData);
            }
            else
            {
                SetState(TryAssignState(ref buildableData, EBuildableState.HitReact), ref buildableData);
            }

            //Debug.Log($"Apply Damage " + GetState(ref buildableData) + ", Health: " + GetHealth(ref buildableData));
        }

        // Prioritize destroyed state
        public override EBuildableState TryAssignState(ref FBuildableData buildableData, EBuildableState newState)
        {
            EBuildableState currentState = GetState(ref buildableData);

            switch (newState)
            {
                case EBuildableState.Inactive:
                    SetState(newState, ref buildableData);
                    return newState;
                case EBuildableState.HitReact:
                    switch (currentState)
                    {
                        case EBuildableState.Destroyed:
                        case EBuildableState.Inactive:

                            SetState(currentState, ref buildableData);
                            return currentState;
                    }
                    break;
            }

            SetState(currentState, ref buildableData);
            return newState;
        }
    }


}
using UnityEngine;

namespace LichLord.Props
{
    [CreateAssetMenu(fileName = "HarvestNodeDataDefinition", menuName = "LichLord/Props/HarvestNodeDataDefinition")]
    public class HarvestNodeDataDefinition : PropDataDefinition
    {

        [SerializeField]
        protected int _maxHarvestPoints = 1000;
        public int MaxHarvestPoints => _maxHarvestPoints;

        // Bit size constants (matching PropDataDefinition)

        protected const int HARVEST_POINTS_BITS = 12;         // 0-4095
        protected const int IS_INTERACTING_BITS = 1;

        // Bit shifts and masks for StateData (int)

        protected const int HARVEST_POINTS_SHIFT = STATE_SHIFT + STATE_BITS;
        protected const int IS_INTERACTING_SHIFT = HARVEST_POINTS_SHIFT + HARVEST_POINTS_BITS;

        protected const int HARVEST_POINTS_MASK = (1 << HARVEST_POINTS_BITS) - 1;
        protected const int IS_INTERACTING_MASK = (1 << IS_INTERACTING_BITS) - 1;

        public override void InitializeData(ref FPropData propData, PropDefinition definition)
        {
            base.InitializeData(ref propData, definition);

            // Set initial values
            SetState(StartingState, ref propData);
            SetHarvestPoints(MaxHarvestPoints, ref propData); // Default health, adjust as needed
            SetIsInteracting(false, ref propData);
        }

        // Harvest Points
        public int GetHarvestPoints(ref FPropData propData)
        {
            return (propData.StateData >> HARVEST_POINTS_SHIFT) & HARVEST_POINTS_MASK;
        }

        public void SetHarvestPoints(int harvestPoints, ref FPropData propData)
        {
            int stateData = propData.StateData;
            harvestPoints = Mathf.Clamp(harvestPoints, 0, HARVEST_POINTS_MASK);
            stateData = (stateData & ~(HARVEST_POINTS_MASK << HARVEST_POINTS_SHIFT)) | (harvestPoints << HARVEST_POINTS_SHIFT);
            propData.StateData = stateData;
        }

        // Handle harvesting
        public void ApplyHarvest(ref FPropData propData, int damage)
        {
            int currentHealth = GetHarvestPoints(ref propData);

            SetHarvestPoints(currentHealth - damage, ref propData);

            Debug.Log($"Harvested " + propData.GUID + ", Harvest Points: " + GetHarvestPoints(ref propData));

            if (GetHarvestPoints(ref propData) <= 0)
            {
                SetState(TryAssignState(ref propData, EPropState.Destroyed), ref propData);
            }
            else
            {
                SetState(TryAssignState(ref propData, EPropState.HitReact), ref propData);
            }
        }

        public bool GetIsInteracting(ref FPropData propData)
        {
            return (propData.StateData & (IS_INTERACTING_MASK << IS_INTERACTING_SHIFT)) != 0;
        }

        public void SetIsInteracting(bool isInteracting, ref FPropData propData)
        {
            int stateData = propData.StateData;
            stateData &= ~(IS_INTERACTING_MASK << IS_INTERACTING_SHIFT); // clear bit
            if (isInteracting)
                stateData |= (1 << IS_INTERACTING_SHIFT);
            propData.StateData = stateData;
        }
    }
}

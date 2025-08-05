using UnityEngine;

namespace LichLord.Props
{
    [CreateAssetMenu(fileName = "NexusDataDefinition", menuName = "LichLord/Props/NexusDataDefinition")]
    public class NexusDataDefinition : PropDataDefinition
    {
        [SerializeField]
        protected bool _startingActivation = false;
        public bool StartingActivation => _startingActivation;

        private const int IS_ACTIVATED_BITS = 1;
        private const int IS_INTERACTING_BITS = 1;

        private const int IS_ACTIVATED_SHIFT = STATE_SHIFT + STATE_BITS; 
        private const int IS_INTERACTING_SHIFT = IS_ACTIVATED_SHIFT + IS_ACTIVATED_BITS; 

        private const int IS_ACTIVATED_MASK = (1 << IS_ACTIVATED_BITS) - 1;
        private const int IS_INTERACTING_MASK = (1 << IS_INTERACTING_BITS) - 1;

        public bool GetIsActivated(ref FPropData propData)
        {
            return (propData.StateData & (IS_ACTIVATED_MASK << IS_ACTIVATED_SHIFT)) != 0;
        }

        public void SetIsActivated(bool isActive, ref FPropData propData)
        {
            int stateData = propData.StateData;
            stateData &= ~(IS_ACTIVATED_MASK << IS_ACTIVATED_SHIFT); // clear bit
            if (isActive)
                stateData |= (1 << IS_ACTIVATED_SHIFT);
            propData.StateData = stateData;
        }

        public override void InitializeData(ref FPropData propData, PropDefinition definition)
        {
            base.InitializeData(ref propData, definition);
            SetIsActivated(StartingActivation, ref propData); // default inactive
            SetIsInteracting(false, ref propData);
        }

        public void ToggleActivation(ref FPropData propData)
        {
            bool current = GetIsActivated(ref propData);
            SetIsActivated(!current, ref propData);
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

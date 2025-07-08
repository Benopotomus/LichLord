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
        private const int IS_ACTIVATED_SHIFT = HEALTH_SHIFT + HEALTH_BITS; // 20
        private const int IS_ACTIVATED_MASK = (1 << IS_ACTIVATED_BITS);    // 0b1

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
        }

        public void ToggleActivation(ref FPropData propData)
        {
            bool current = GetIsActivated(ref propData);
            SetIsActivated(!current, ref propData);
        }
    }
}

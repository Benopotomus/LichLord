using LichLord.Items;
using System;

namespace LichLord
{
    [Serializable]
    public class TestSummonable : TestItem
    {
        // Optional: Runtime validation
        public void SetDefinition(ItemDefinition definition)
        {
            if (definition != null && !(definition is SummonableDefinition))
            {
                throw new ArgumentException("TestSummonable Definition must be a SummonableDefinition.");
            }
            Definition = definition;
        }
    }
}

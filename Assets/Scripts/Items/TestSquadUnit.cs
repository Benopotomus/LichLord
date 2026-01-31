using LichLord.Items;
using System;

namespace LichLord
{
    [Serializable]
    public class TestSquadUnit : TestItem
    {
        // Optional: Runtime validation
        public void SetDefinition(ItemDefinition definition)
        {
            if (definition != null && !(definition is CommandUnitItemDefinition))
            {
                throw new ArgumentException("TestSummonable Definition must be a Command Unit Item Definition.");
            }
            Definition = definition;
        }
    }
}

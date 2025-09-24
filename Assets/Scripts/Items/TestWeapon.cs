using LichLord.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LichLord
{
    [Serializable]
    public class TestWeapon : TestItem
    {
        // Optional: Runtime validation
        public void SetDefinition(ItemDefinition definition)
        {
            if (definition != null && !(definition is WeaponDefinition))
            {
                throw new ArgumentException("TestWeapon Definition must be a WeaponDefinition.");
            }
            Definition = definition;
        }
    }
}

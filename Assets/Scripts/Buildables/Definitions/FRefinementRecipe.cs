using LichLord.Items;
using System;
using System.Collections.Generic;

namespace LichLord
{
    [Serializable]
    public struct FRefinementRecipe
    {
        public List<FItemRecipeValue> InItems;

        public List<FItemRecipeValue> OutItems;
    }

    [Serializable]
    public struct FItemRecipeValue
    { 
        public ItemDefinition Item;
        public int Count;
    }
}

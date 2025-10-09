using LichLord.Items;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class RefineryDefinition : BuildableFeatureDefinition
    {
        // Refinement
        [Header("Refinement")]
        [SerializeField]
        protected int _ticksPerProgress = 32;
        public int TicksPerProgress => _ticksPerProgress;

        [SerializeField]
        protected int _maxProgress = 10;
        public int MaxProgress => _maxProgress;

        [SerializeField]
        protected RecipeListDataDefinition _recipeList;
        public RecipeListDataDefinition RecipeList => _recipeList;

        [SerializeField]
        protected int _inSlots;
        public int InSlots => _inSlots;

        [SerializeField]
        protected int _outSlots;
        public int OutSlots => _outSlots;

        /// Returns the first recipe that matches the provided input items, or null if none match.
        public RefinementRecipe GetValidRecipe(List<(int, FItemSlotData)> itemDatas)
        {
            return _recipeList.GetValidRecipe(itemDatas);
        }

    }
}

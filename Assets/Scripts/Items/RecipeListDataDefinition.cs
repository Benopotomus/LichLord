using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Items
{
    [CreateAssetMenu(fileName = "RecipeListDataDefinition", menuName = "LichLord/Items/RecipeListDataDefinition")]
    public class RecipeListDataDefinition : ScriptableObject
    {
        [SerializeField]
        protected RefinementRecipe[] _recipes;
        public RefinementRecipe[] Recipes => _recipes;

        /// Returns the first recipe that matches the provided input items, or null if none match.
        public RefinementRecipe GetValidRecipe(List<(int, FItemSlotData)> itemDatas)
        {
            if (_recipes == null || _recipes.Length == 0)
                return null;

            foreach (var recipe in _recipes)
            {
                if (recipe != null && recipe.IsRecipeValid(itemDatas))
                    return recipe; // First matching recipe
            }

            return null; // No valid recipe found
        }
    }
}

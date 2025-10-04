using DWD.Utility.Loading;
using UnityEngine;

namespace LichLord.Buildables
{
    [CreateAssetMenu(fileName = "IronRefineryDefinition", menuName = "LichLord/Buildables/IronRefineryDefinition")]
    public class IronRefineryDefinition : BuildableDefinition
    {
        // Refinement
        [Header("Refinement")]
        [SerializeField]
        protected int _ticksPerProgress = 32;
        public int TicksPerProgress => _ticksPerProgress;

        [SerializeField]
        protected FRefinementRecipe[] _recipes;
        public FRefinementRecipe[] Recipes => _recipes;

    }
}

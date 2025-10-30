using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterHealthComponent : MonoBehaviour
    {
        [SerializeField]
        private NonPlayerCharacter _npc;

        [SerializeField]
        private int _currentHealth;

        [SerializeField]
        private int _maxHealth;

        public void OnSpawned(NonPlayerCharacterRuntimeState state)
        {
            _currentHealth = state.GetHealth();
            _maxHealth = state.GetMaxHealth();
        }

        public void OnRender(NonPlayerCharacterRuntimeState state, int tick)
        {
            var newHealth = state.GetHealth();

            if (newHealth == _currentHealth)
                return;

            _currentHealth = newHealth;
        }
    }
}

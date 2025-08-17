using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableHealthComponent : MonoBehaviour
    {
        [SerializeField] private int _currentHealth;
        public int CurrentHealth => _currentHealth;

        public void UpdateHealth(int newHealth)
        {
            if (_currentHealth == newHealth)
                return;

            _currentHealth = newHealth;
        }
    }
}

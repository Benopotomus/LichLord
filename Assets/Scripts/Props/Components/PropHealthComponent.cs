using UnityEngine;

namespace LichLord.Props
{
    public class PropHealthComponent : MonoBehaviour
    {
        [SerializeField] private Prop _prop;
        public Prop Prop => _prop;

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

using Fusion;

using UnityEngine;

namespace LichLord
{
    public class PlayerHealthComponent : ContextBehaviour
    {
        [Networked]
        private int _currentHealth { get; set; } = 100;
        public int CurrentHealth => _currentHealth;

        [Networked]
        private int _maxHealth { get; set; } = 100;
        public int MaxHealth => _maxHealth;

        public float HealthPercent { get { return Mathf.Clamp01(CurrentHealth / MaxHealth); } }



    }
}

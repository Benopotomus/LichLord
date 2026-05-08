using System.Collections.Generic;
using UnityEngine;

namespace LichLord.CardGame
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "CardGame/Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string enemyName;

        [TextArea(2, 5)]
        public string description;

        public int maxHealth;

        [Range(2, 5)]
        [Tooltip("Number of d6 this enemy rolls each round. Per design rules, enemies use 2–5 dice.")]
        public int diceCount;

        [Tooltip("Passive effects evaluated each round against this enemy's dice roll.")]
        public List<EnemyPassiveEffectData> passiveEffects = new List<EnemyPassiveEffectData>();
    }
}

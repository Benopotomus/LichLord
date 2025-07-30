using LichLord.NonPlayerCharacters;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "InvasionSpawnWave", menuName = "LichLord/Invasions/InvasionSpawnWave")]
    public class InvasionSpawnWaveDefinition : ScriptableObject
    {
        
        [SerializeField]
        private List<NonPlayerCharacterDefinition> _invasionCharacters = new List<NonPlayerCharacterDefinition>();
        public List<NonPlayerCharacterDefinition> InvasionCharacters => _invasionCharacters;


    }
}

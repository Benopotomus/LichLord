using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "InvasionDefinition", menuName = "LichLord/Invasions/InvasionDefinition")]
    public class InvasionDefinition : TableObject
    {
        [SerializeField]
        private List<InvasionSpawnWaveDefinition> _spawnWaves = new List<InvasionSpawnWaveDefinition>();
        public List<InvasionSpawnWaveDefinition> SpawnWaves => _spawnWaves;

        [SerializeField]
        private int _ticksBetweenWaves = 160;
        public int TicksBetweenWaves => _ticksBetweenWaves;


        [SerializeField]
        private int _invasionTotalTicks = 640;
        public int InvasionTotalTicks => _invasionTotalTicks;
    }
}

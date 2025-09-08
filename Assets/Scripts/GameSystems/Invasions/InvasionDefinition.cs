using LichLord.Dialog;
using LichLord.NonPlayerCharacters;
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
        private EAttitude _startingAttitude;
        public EAttitude StartingAttitude => _startingAttitude;

        [SerializeField]
        private int _ticksBetweenWaves = 160;
        public int TicksBetweenWaves => _ticksBetweenWaves;

        [SerializeField]
        private int _invasionTotalTicks = 640;
        public int InvasionTotalTicks => _invasionTotalTicks;

        [Header("Dialog")]
        [SerializeField]
        private NonPlayerCharacterDefinition _dialogNPC;
        public NonPlayerCharacterDefinition DialogNPC => _dialogNPC;

        [SerializeField]
        private DialogDefinition _dialog;
        public DialogDefinition Dialog => _dialog;

        [SerializeField] // ticks until invaders will retreat after final wave spawns
        private int _ticksUntilRetreat = 1920;
        public int TicksUntilRetreat => _ticksUntilRetreat;

        [SerializeField] // ticks until invaders despawn after final wave spawns
        private int _ticksUntilDespawn = 2880;
        public int TicksUntilDespawn => _ticksUntilDespawn;
    }
}

using Fusion;
using LichLord.NonPlayerCharacters;
using LichLord.Props;
using LichLord.World;
using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

/// This class is used to spawn invasions that attack player defenses

namespace LichLord
{
    public class InvasionManager : ContextBehaviour
    {
        [Networked]
        public byte InvasionID { get; set; }
        private byte _localInvasionID;

        [Networked]
        public int InvasionStartTick { get; set; }

        [Networked]
        public byte InvasionSpawnWave { get; set; }

        [Networked]
        public FWorldPosition InvasionSpawnPosition { get; set; }

        [Networked]
        public ref FStrongholdData TargetStrongholdData => ref MakeRef<FStrongholdData>();
        private Stronghold _targetStronghold;
        public Stronghold TargetStronghold => _targetStronghold;

        public InvasionDefinition ActiveInvasion;

        public Action<Stronghold> onInvasionStarted;
        public Action onInvasionEnded;

        public void BeginInvasion(byte invasionID, FStrongholdData targetStrongholdData)
        {
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                return;

            InvasionID = invasionID;
            InvasionStartTick = Runner.Tick;
            InvasionSpawnWave = 0;

            TargetStrongholdData = targetStrongholdData;

            SpawnInvasionWave(InvasionSpawnWave);
        }

        public void RPC_BeginInvasion(byte invasionID, FStrongholdData targetStrongholdData)
        { 
            
        }

        public override void Render()
        {
            base.Render();

            if (InvasionID != _localInvasionID)
            {
                _localInvasionID = InvasionID;
                LocalInvasionChanged();
            }
        }

        private void LocalInvasionChanged()
        {
            if (InvasionID == 0)
            {
                ActiveInvasion = null;
                _targetStronghold = null;
                onInvasionEnded?.Invoke();
            }
            else
            {
                ActiveInvasion = Global.Tables.InvasionTable.TryGetDefinition(InvasionID);
                _targetStronghold = Context.StrongholdManager.GetStronghold(TargetStrongholdData);

                onInvasionStarted?.Invoke(_targetStronghold);
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (InvasionID == 0)
                return;

            ActiveInvasion = Global.Tables.InvasionTable.TryGetDefinition(InvasionID);

            int tick = Runner.Tick;

            int ticksSinceStart =  tick - InvasionStartTick;

            if (ticksSinceStart > ActiveInvasion.InvasionTotalTicks)
            {
                InvasionID = 0;
                return;
            }

            if (InvasionSpawnWave >= ActiveInvasion.SpawnWaves.Count)
                return;

            if (ticksSinceStart > (InvasionSpawnWave * ActiveInvasion.TicksBetweenWaves))
            {
                SpawnInvasionWave(InvasionSpawnWave);
            }
        }

        private Vector3 GetInvasionStagingPosition()
        {
            var players = Context.NetworkGame.ActivePlayers;
            var state = Context.StrongholdManager.GetNexusState(TargetStrongholdData);

            Chunk strongholdChunk = state.chunk;
            Vector3 strongholdPosition = state.position;

            var nearbyChunks = Context.ChunkManager.GetNearbyChunks(strongholdChunk.ChunkID,2);
            var nearbyInvasionSpawns = new List<InvasionSpawnPoint>();

            foreach (var chunk in nearbyChunks)
            {
                foreach (var spawnPoint in chunk.InvasionSpawnPoints)
                {
                    nearbyInvasionSpawns.Add(spawnPoint);
                }
            }

            Vector3[] playerPositions = new Vector3[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                playerPositions[i] = players[i].CachedTransform.position;
            }

            const float minNexusDistance = 100f;
            const float maxNexusDistance = 200f;
            const float minPlayerDistance = 150f;

            // 1. Filter spawn points by distance to the nexus position (min/max distance)
            var validSpawnPoints = new List<InvasionSpawnPoint>();
            for (int i = 0; i < nearbyInvasionSpawns.Count; i++)
            {
                float distToNexus = Vector3.Distance(nearbyInvasionSpawns[i].position, strongholdPosition);
                if (distToNexus >= minNexusDistance && distToNexus <= maxNexusDistance)
                {
                    validSpawnPoints.Add(nearbyInvasionSpawns[i]);
                }
            }

            // 2. Filter out spawn points that are too close to any player
            for (int i = validSpawnPoints.Count - 1; i >= 0; i--)
            {
                bool tooCloseToPlayer = false;
                for (int p = 0; p < playerPositions.Length; p++)
                {
                    float distToPlayer = Vector3.Distance(validSpawnPoints[i].position, playerPositions[p]);
                    if (distToPlayer < minPlayerDistance)
                    {
                        tooCloseToPlayer = true;
                        break;
                    }
                }

                if (tooCloseToPlayer)
                {
                    validSpawnPoints.RemoveAt(i);
                }
            }

            // 3. Choose random valid spawn point or fallback
            if (validSpawnPoints.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, validSpawnPoints.Count);
                return validSpawnPoints[randomIndex].position;
            }

            return strongholdPosition;
        }

        private void SpawnInvasionWave(int wave)
        {
            ActiveInvasion = Global.Tables.InvasionTable.TryGetDefinition(InvasionID);
            var spawnWaveDefinition = ActiveInvasion.SpawnWaves[wave];
            var waveCharacters = spawnWaveDefinition.InvasionCharacters;

            var stagingPosition = GetInvasionStagingPosition();

            for (int i = 0; i < waveCharacters.Count; i++)
            {
                // Generate random position above ground
                Vector3 randomPositionAbove = new Vector3(
                    UnityEngine.Random.Range(-5f, 5f),
                    100f, // Fixed height to raycast from
                    UnityEngine.Random.Range(-5f, 5f)
                );

                randomPositionAbove += stagingPosition;

                // Raycast down to find ground
                Vector3 spawnPosition = Vector3.zero;
                RaycastHit hit;
                if (Physics.Raycast(randomPositionAbove, Vector3.down, out hit, 200f))
                {
                    spawnPosition = hit.point;
                    
                }
                else
                {
                    // Fallback if raycast fails
                    Debug.LogWarning($"Raycast failed from {randomPositionAbove}, using default position");
                    continue; // Skip this spawn if no valid ground is found
                }

                // Spawn the NPC at the calculated position
                //Debug.Log("NPC spawned at spawn position: " + spawnPosition);
                Context.NonPlayerCharacterManager.SpawnNPC(spawnPosition, waveCharacters[i], ENPCSpawnType.Invasion, ETeamID.EnemiesTeamA, EAttitude.Hostile);
            }

            InvasionSpawnWave++;
        }

        public void LoadInvasionSpawnPointsForChunk(Chunk chunk)
        {
            ChunkMarkupData baseMarkupData = Context.WorldManager.WorldSettings.GetMarkupData(chunk.ChunkID);

            if (baseMarkupData == null)
                return;

            for (int i = 0; i < baseMarkupData.InvasionSpawnPointMarkupDatas.Length; i++)
            {
                var invasionSpawnMarkupData = baseMarkupData.InvasionSpawnPointMarkupDatas[i];
                if (invasionSpawnMarkupData == null)
                {
                    continue;
                }

                InvasionSpawnPoint invasionSpawnPoint = new InvasionSpawnPoint(
                    invasionSpawnMarkupData.position,
                    chunk);

                chunk.AddInvasionSpawnPoint(invasionSpawnPoint); // Add to chunk's invasionSpawns
            }
        }

    }
}

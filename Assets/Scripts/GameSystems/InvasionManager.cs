using Fusion;
using LichLord.NonPlayerCharacters;
using LichLord.Props;
using LichLord.World;
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
        public sbyte InvasionSpawnWave { get; set; }
        private int _localSpawnWave = -1;

        [Networked]
        public ref FWorldPosition InvasionStagingPosition => ref MakeRef<FWorldPosition>();

        [Networked]
        public sbyte TargetStrongholdID { get; set; }

        [SerializeField]
        private Stronghold _targetStronghold;
        public Stronghold TargetStronghold => _targetStronghold;

        [Networked]
        public EInvasionState InvasionState { get; set; }
        private EInvasionState _localInvasionState = EInvasionState.None;

        [SerializeField] private int _nextWaveTick = 0;
        [SerializeField] private int _retreatTick = 0;
        [SerializeField] private int _despawnTick = 0;

        public InvasionDefinition ActiveInvasion;

        public Action<Stronghold> onInvasionStarted;
        public Action onInvasionEnded;

        public void LoadInvasionData()
        {
            if (!HasStateAuthority)
                return;

            FInvasionSaveData saveData = Context.WorldSaveLoadManager.LoadedInvasion;

            if (saveData.invasionId == 0)
                return;

            int tick = Runner.Tick;

            InvasionID = (byte)saveData.invasionId;
            ActiveInvasion = Global.Tables.InvasionTable.TryGetDefinition(InvasionID);

            InvasionSpawnWave = (sbyte)saveData.invasionSpawnWave;
            _localSpawnWave = InvasionSpawnWave; // ensure we don't respawn the wave
            _nextWaveTick = tick + ActiveInvasion.TicksBetweenWaves;

            int maxInvasionIndex = ActiveInvasion.SpawnWaves.Count - 1;
            if (_localSpawnWave == maxInvasionIndex)
                _retreatTick = tick + ActiveInvasion.TicksUntilRetreat;

            InvasionStagingPosition.CopyPosition(saveData.invasionSpawnPosition);

            TargetStrongholdID =(sbyte)saveData.targetStrongholdId;

            _targetStronghold = Context.StrongholdManager.GetStronghold(TargetStrongholdID);

            InvasionState = saveData.invasionState;
            _localInvasionState = InvasionState;

            if (_localInvasionState == EInvasionState.Retreating)
                _despawnTick = tick + ActiveInvasion.TicksUntilDespawn;
        }

        public void BeginInvasion(byte invasionID, int targetStrongholdId)
        {
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                return;

            InvasionID = invasionID;
            InvasionSpawnWave = 0;
            InvasionState = EInvasionState.Approaching;
            TargetStrongholdID = (sbyte)targetStrongholdId;
            InvasionStagingPosition.CopyPosition(GetInvasionStagingPosition());
            //Debug.Log(InvasionStagingPosition.Position);
            _localSpawnWave = -1;
            //SpawnInvasionWave(InvasionSpawnWave);
        }

        public void RPC_BeginInvasion(byte invasionID, FStaticPropPosition targetPropPosition)
        { 
            
        }

        public void StopInvasion()
        {
            if (!HasStateAuthority)
                return;

            InvasionID = 0;
            InvasionSpawnWave = -1;
            InvasionState = EInvasionState.None;

            Context.NonPlayerCharacterManager.DespawnAllInvaders();
        }

        public override void Render()
        {
            base.Render();

            if (InvasionID != _localInvasionID)
            {
                _localInvasionID = InvasionID;
                LocalInvasionChanged();
            }

            if (_localInvasionID == 0)
            {
                _localSpawnWave = -1;
                return;
            }

            if(TargetStrongholdID > 0 && _targetStronghold == null)
                _targetStronghold = Context.StrongholdManager.GetStronghold(TargetStrongholdID);

            ActiveInvasion = Global.Tables.InvasionTable.TryGetDefinition(InvasionID);

            int tick = Runner.Tick;
            int maxInvasionIndex = ActiveInvasion.SpawnWaves.Count - 1;

            if (_localSpawnWave != InvasionSpawnWave)
            {
                _localSpawnWave = InvasionSpawnWave;
                _nextWaveTick = tick + ActiveInvasion.TicksBetweenWaves;

                if (_localSpawnWave == maxInvasionIndex)
                {
                    _retreatTick = tick + ActiveInvasion.TicksUntilRetreat;
                }

                if (HasStateAuthority)
                {
                    SpawnInvasionWave(_localSpawnWave);
                }
            }

            if (_localInvasionState != InvasionState)
            {
                _localInvasionState = InvasionState;

                if (_localInvasionState == EInvasionState.Retreating)
                {
                    _despawnTick = tick + ActiveInvasion.TicksUntilDespawn;
                    Context.NonPlayerCharacterManager.SetInvaderAttitude(EAttitude.Defensive);
                }
            }

            if (!HasStateAuthority)
                return;

            if (_localInvasionState == EInvasionState.Retreating)
            {
                if (tick >= _despawnTick)
                {
                    StopInvasion();
                    return;
                }
            }

            if (_localSpawnWave == maxInvasionIndex &&
                tick >= _retreatTick)
            {
                //Context.NonPlayerCharacterManager.SetInvaderAttitude(EAttitude.Hostile);
                InvasionState = EInvasionState.Retreating;
            }

            if (tick >= _nextWaveTick)
            {
                if (InvasionSpawnWave < maxInvasionIndex)
                {
                    InvasionSpawnWave = (sbyte)Mathf.Clamp(InvasionSpawnWave + 1, -1, maxInvasionIndex);
                }
            }
        }

        private void LocalInvasionChanged()
        {
            if (_localInvasionID == 0)
            {
                ActiveInvasion = null;
                _targetStronghold = null;
                onInvasionEnded?.Invoke();
            }
            else
            {
                ActiveInvasion = Global.Tables.InvasionTable.TryGetDefinition(InvasionID);
                _targetStronghold = Context.StrongholdManager.GetStronghold(TargetStrongholdID);

                onInvasionStarted?.Invoke(_targetStronghold);
            }
        }

        public override void FixedUpdateNetwork()
        {
        }

        private Vector3 GetInvasionStagingPosition()
        {
            var players = Context.NetworkGame.ActivePlayers;
            var stronghold = Context.StrongholdManager.GetStronghold(TargetStrongholdID);

            Chunk strongholdChunk = stronghold.CurrentChunk;
            Vector3 strongholdPosition = stronghold.Position;

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
            const float maxNexusDistance = 150f;
            const float minPlayerDistance = 100f;

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
            if (InvasionID == 0)
                return;

            if (!HasStateAuthority)
                return;
                
            ActiveInvasion = Global.Tables.InvasionTable.TryGetDefinition(InvasionID);
            var spawnWaveDefinition = ActiveInvasion.SpawnWaves[wave];
            var waveCharacters = spawnWaveDefinition.InvasionCharacters;

            Vector3 spawnPosition = Vector3.zero;

            for (int i = 0; i < waveCharacters.Count; i++)
            {
                // Generate random position above ground
                Vector3 randomPositionAbove = new Vector3(
                    UnityEngine.Random.Range(-5f, 5f),
                    100f, // Fixed height to raycast from
                    UnityEngine.Random.Range(-5f, 5f)
                );

                randomPositionAbove += InvasionStagingPosition.Position;

                // Raycast down to find ground
                spawnPosition = Vector3.zero;
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
                Context.NonPlayerCharacterManager.SpawnNPCInvader(spawnPosition,
                    waveCharacters[i], 
                    ETeamID.EnemiesTeamA, 
                    ActiveInvasion.StartingAttitude,
                    i);
            }

            if(wave == 0)
            {
                if (ActiveInvasion.Dialog != null)
                {
                    Context.NonPlayerCharacterManager.SpawnNPCInvader(spawnPosition,
                        ActiveInvasion.DialogNPC,
                        ETeamID.EnemiesTeamA,
                        ActiveInvasion.StartingAttitude,
                        4,
                        ActiveInvasion.Dialog);
                }  
            }
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

        public Vector3 GetInvasionTargetPosition(Vector3 formationOffset)
        {
            switch (InvasionState)
            {
                case EInvasionState.Approaching:

                    var targetPosition = TargetStronghold.CachedTransform.position;

                    Vector3 direction = (InvasionStagingPosition.Position - targetPosition).normalized;

                    Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                    Vector3 rotatedOffset = rotation * formationOffset;

                    Vector3 backedUpTarget = (targetPosition + direction * TargetStronghold.InfluenceDistance) + rotatedOffset;

                    return backedUpTarget;
                case EInvasionState.Retreating:
                    return InvasionStagingPosition.Position;
            }

            return Vector3.zero;
        }

        public void MSG_SetInvasionRetreating()
        {
            RPC_SetInvasionState(EInvasionState.Retreating);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SetInvasionState(EInvasionState newState)
        { 
            InvasionState = newState;
        }

        public void MSG_SetInvadersHostile()
        {
            RPC_SetInvadersHostile();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SetInvadersHostile()
        {
            Context.NonPlayerCharacterManager.SetInvaderAttitude(EAttitude.Hostile);
        }
    }

    public enum EInvasionState : byte
    { 
        None,
        Approaching,
        Retreating,
    }
}

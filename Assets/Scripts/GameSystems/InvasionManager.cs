using Fusion;
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
        public ref FStrongholdData TargetNexus => ref MakeRef<FStrongholdData>();

        public InvasionDefinition ActiveInvasion;

        public Action<FStrongholdData> onInvasionStarted;
        public Action onInvasionEnded;

        public PropRuntimeState GetTargetNexus()
        {
            if (!TargetNexus.IsValid())
                return null;

            Chunk chunk = Context.ChunkManager.GetChunk(TargetNexus.ChunkID);
            if (chunk != null && chunk.GetRenderState(HasStateAuthority, TargetNexus.GUID, out var state))
            {
                return state;
            }

            return null;
        }

        public void BeginInvasion(byte invasionID, FStrongholdData nexusTarget)
        {  
            InvasionID = invasionID;
            InvasionStartTick = Runner.Tick;
            InvasionSpawnWave = 0;

            TargetNexus = nexusTarget;

            SpawnInvasionWave(InvasionSpawnWave);
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
                onInvasionEnded?.Invoke();
            }
            else
            {
                ActiveInvasion = Global.Tables.InvasionTable.TryGetDefinition(InvasionID);
                onInvasionStarted?.Invoke(TargetNexus);
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
            var state = GetTargetNexus();
            Vector3 statePosition = state.position;

            // Pre-allocate array to avoid List resizing
            Vector3[] playerPositions = new Vector3[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                playerPositions[i] = players[i].CachedTransform.position;
            }

            const float minNexusDistance = 150f;
            const float maxNexusDistance = 200f;
            const float minPlayerDistance = 150f;
            const int maxAttempts = 50;
            const float raycastHeight = 500f;
            const float maxRaycastDistance = 1000f;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Generate random angle and distance
                float angle = UnityEngine.Random.value * Mathf.PI * 2f;
                float distance = UnityEngine.Random.Range(minNexusDistance, maxNexusDistance);

                // Calculate candidate position in XZ plane
                Vector3 direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                Vector3 candidatePosition = statePosition + direction * distance;

                // Verify nexus distance in XZ plane
                Vector2 stateXZ = new Vector2(statePosition.x, statePosition.z);
                Vector2 candidateXZ = new Vector2(candidatePosition.x, candidatePosition.z);
                float nexusDistanceSqr = (candidateXZ - stateXZ).sqrMagnitude;
                if (nexusDistanceSqr < minNexusDistance * minNexusDistance ||
                    nexusDistanceSqr > maxNexusDistance * maxNexusDistance)
                {
                    Debug.LogWarning($"Attempt {attempt}: Invalid nexus distance {Mathf.Sqrt(nexusDistanceSqr)}");
                    continue;
                }

                // Check XZ distance to players
                bool valid = true;
                float minPlayerDistanceSqr = minPlayerDistance * minPlayerDistance;
                for (int i = 0; i < playerPositions.Length; i++)
                {
                    Vector2 playerXZ = new Vector2(playerPositions[i].x, playerPositions[i].z);
                    float playerDistanceSqr = (candidateXZ - playerXZ).sqrMagnitude;
                    if (playerDistanceSqr < minPlayerDistanceSqr)
                    {
                        valid = false;
                        Debug.Log($"Attempt {attempt}: Too close to player {i}, distance {Mathf.Sqrt(playerDistanceSqr)}");
                        break;
                    }
                }

                if (valid)
                {
                    // Raycast down from 500 units above
                    Vector3 raycastOrigin = new Vector3(candidatePosition.x, raycastHeight, candidatePosition.z);
                    RaycastHit hit;
                    if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, maxRaycastDistance))
                    {
                        Vector3 finalPosition = hit.point;
                        // Double-check final position distances
                        float finalNexusDistance = (new Vector2(finalPosition.x, finalPosition.z) - stateXZ).magnitude;
                        bool playersValid = true;
                        for (int i = 0; i < playerPositions.Length; i++)
                        {
                            if ((new Vector2(finalPosition.x, finalPosition.z) - new Vector2(playerPositions[i].x, playerPositions[i].z)).sqrMagnitude < minPlayerDistanceSqr)
                            {
                                playersValid = false;
                                Debug.Log($"Raycast hit invalid: Too close to player {i}");
                                break;
                            }
                        }
                        if (finalNexusDistance >= minNexusDistance && finalNexusDistance <= maxNexusDistance && playersValid)
                        {
                            Debug.Log($"Valid position found at attempt {attempt}: {finalPosition}, nexus distance {finalNexusDistance}");
                            return finalPosition;
                        }
                        Debug.Log($"Raycast hit invalid: Nexus distance {finalNexusDistance}");
                    }
                    else
                    {
                        Debug.Log($"Attempt {attempt}: Raycast missed terrain");
                    }
                }
            }

            // Fallback: Try raycast from 500 units above a point 250 units from nexus
            float fallbackAngle = UnityEngine.Random.value * Mathf.PI * 2f;
            Vector3 fallbackDirection = new Vector3(Mathf.Sin(fallbackAngle), 0, Mathf.Cos(fallbackAngle));
            Vector3 fallbackPosition = statePosition + fallbackDirection * 250f;
            Vector3 fallbackRaycastOrigin = new Vector3(fallbackPosition.x, statePosition.y + raycastHeight, fallbackPosition.z);
            RaycastHit fallbackHit;
            if (Physics.Raycast(fallbackRaycastOrigin, Vector3.down, out fallbackHit, maxRaycastDistance))
            {
                Vector3 finalPosition = fallbackHit.point;
                Debug.Log($"Fallback used: Position {finalPosition}, nexus distance {(new Vector2(finalPosition.x, finalPosition.z) - new Vector2(statePosition.x, statePosition.z)).magnitude}");
                return finalPosition;
            }

            // Ultimate fallback: Return position at nexus height
            Vector3 ultimateFallback = new Vector3(fallbackPosition.x, statePosition.y, fallbackPosition.z);
            Debug.LogWarning($"Ultimate fallback used: {ultimateFallback}");
            return ultimateFallback;
        }

        private void SpawnInvasionWave(int wave)
        {
            ActiveInvasion = Global.Tables.InvasionTable.TryGetDefinition(InvasionID);
            var spawnWaveDefinition = ActiveInvasion.SpawnWaves[wave];
            var waveCharacters = spawnWaveDefinition.InvasionCharacters;

            var state = Context.StrongholdManager.GetNexusState(TargetNexus);
            var stagingPosition = GetInvasionStagingPosition();

            for (int i = 0; i < waveCharacters.Count; i++)
            {
                // Generate random position above ground
                Vector3 randomPositionAbove = new Vector3(
                    UnityEngine.Random.Range(-10f, 10f),
                    100f, // Fixed height to raycast from
                    UnityEngine.Random.Range(-10f, 10f)
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
                Debug.Log("NPC spawned at spawn position: " + spawnPosition);
                Context.NonPlayerCharacterManager.SpawnNPC(spawnPosition, waveCharacters[i], ETeamID.EnemiesTeamA, true);
            }

            InvasionSpawnWave++;
        }
    }
}

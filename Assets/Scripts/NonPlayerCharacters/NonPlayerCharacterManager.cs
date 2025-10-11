using Fusion;
using LichLord.Dialog;
using LichLord.World;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterManager : ContextBehaviour
    {
        [SerializeField] private NonPlayerCharacterSpawner _spawner;
        [SerializeField] private NonPlayerCharacterReplicator _replicatorPrefab;
        [SerializeField] private NonPlayerCharacterManagerDebug _debug;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float raycastLength = 6f; 

        private Dictionary<int, FNonPlayerCharacterData> _deltaStates = new Dictionary<int, FNonPlayerCharacterData>();

        private List<NonPlayerCharacterReplicator> _replicators = new List<NonPlayerCharacterReplicator>();

        public void AddReplicator(NonPlayerCharacterReplicator replicator)
        {
            if (!_replicators.Contains(replicator))
            {
                _replicators.Add(replicator);
            }
        }

        public override void Spawned()
        {
           _debug.OnSpawned();
        }

        private FNonPlayerCharacterData CreateNPCData(Vector3 spawnPos,
              NonPlayerCharacterDefinition definition,
              ENPCSpawnType spawnType,
              ETeamID teamID,
              EAttitude attitude)
        {
            FNonPlayerCharacterData data = new FNonPlayerCharacterData
            {
                DefinitionID = definition.TableID,
                SpawnType = spawnType,
                Position = spawnPos,
                Rotation = Quaternion.identity
            };

            var dataDefinition = definition.GetDataDefinition(spawnType);
            if (dataDefinition != null)
                dataDefinition.InitializeData(ref data, definition, spawnType, teamID, attitude);

            return data;
        }

        private void SpawnNPC(ref FNonPlayerCharacterData data)
        {
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                return;

            NonPlayerCharacterReplicator replicator = GetReplicatorWithFreeSlots();
            if (replicator == null) return;

            int freeIndex = replicator.GetFreeIndex();
            if (freeIndex == -1) return;

            _deltaStates[freeIndex] = data;
            replicator.SpawnNPC(ref data, freeIndex);
        }

        public void SpawnNPCInvader(Vector3 spawnPos,
            NonPlayerCharacterDefinition definition,
            ETeamID teamID,
            EAttitude attitude,
            int formationIndex,
            DialogDefinition dialog = null)
        {
            FNonPlayerCharacterData data = CreateNPCData(spawnPos, definition, ENPCSpawnType.Invader, teamID, attitude);

            var invaderData = definition.GetDataDefinition(ENPCSpawnType.Invader) as InvaderDataDefinition;
            if (invaderData == null)
            {
                Debug.Log("Trying to spawn a non-invader as an invader");
                return;
            }

            invaderData.SetFormationIndex(formationIndex, ref data);

            if (dialog != null)
            {
                int freeIndex = Context.DialogManager.AddActiveDialog(dialog);
                invaderData.SetDialogIndex(freeIndex, ref data);
            }
            SpawnNPC(ref data);
        }

        public void SpawnNPCWorker(Vector3 spawnPos, NonPlayerCharacterDefinition definition, ETeamID teamID, int strongholdId, int workerIndex)
        {
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                return;

            FNonPlayerCharacterData data = CreateNPCData(spawnPos, definition, ENPCSpawnType.Worker, teamID, EAttitude.Passive);

            // Type-specific adjustment: worker index
            var workerData = definition.GetDataDefinition(ENPCSpawnType.Worker) as WorkerDataDefinition;
            if (workerData == null)
            {
                Debug.Log("Trying to spawn a non-worker as a worker");
                return;
            }

            workerData.SetState(ENPCState.Spawning, ref data);
            workerData.SetStrongholdId(strongholdId, ref data);
            workerData.SetWorkerIndex(workerIndex, ref data);
            SpawnNPC(ref data);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_SpawnNPCWarriorGroup(FWorldPosition centerPosition,
            byte[] npcDefinitionIds,
            ETeamID teamId,
            byte playerFollowIndex
        )
        {
            var pc = Context.NetworkGame.GetPlayerByIndex(playerFollowIndex);
            if (pc == null)
                return;

            var formationComponent = pc.Formation;
            if (formationComponent == null)
                return;

            for (int i = 0; i < npcDefinitionIds.Length; i++)
            {
                NonPlayerCharacterDefinition definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(npcDefinitionIds[i]);
                if (definition == null)
                    continue;

                Vector3 randomPosition = new Vector3(
                Random.Range(-2f, 2f),
                0, // Keep Y fixed
                Random.Range(-2f, 2f)
                );

                // Combine offset into raycast origin
                if (Physics.Raycast((randomPosition + centerPosition.Position) +
                    (Vector3.up * (raycastLength * 0.5f)),
                    Vector3.down,
                    out RaycastHit hit,
                    raycastLength,
                    hitMask))
                {
                    var result = definition.IsFrontlineCombatant ?
                        formationComponent.GetFreeFrontlineIndex() :
                        formationComponent.GetFreeBacklineIndex();

                    int freeFormationId = result.formationId;
                    int freeFormationIndex = result.formationIndex;

                    // If no slot found, try the opposite
                    if (freeFormationId == -1 || freeFormationIndex == -1)
                    {
                        result = definition.IsFrontlineCombatant
                            ? formationComponent.GetFreeBacklineIndex()
                            : formationComponent.GetFreeFrontlineIndex();

                        freeFormationId = result.formationId;
                        freeFormationIndex = result.formationIndex;
                    }

                    // If still no slot found, bail
                    if (freeFormationId == -1 || freeFormationIndex == -1)
                        continue;

                    pc.Formation.SetFormationIndexFilled(freeFormationId, freeFormationIndex);
                    SpawnNPCWarrior(hit.point, definition, teamId, playerFollowIndex, freeFormationId, freeFormationIndex);
                }
            }
        }

        public void SpawnNPCWarrior(Vector3 spawnPos, NonPlayerCharacterDefinition definition, ETeamID teamId, int playerFollowIndex, int formationID, int formationIndex)
        {
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                return;

            FNonPlayerCharacterData data = CreateNPCData(spawnPos, definition, ENPCSpawnType.Warrior, teamId, EAttitude.Hostile);

            // Type-specific adjustment: worker index
            var warriorData = definition.GetDataDefinition(ENPCSpawnType.Warrior) as WarriorDataDefinition;
            if (warriorData == null)
            {
                Debug.Log("Trying to spawn a non-warrior as a warrior");
                return;
            }

            var pc = Context.NetworkGame.GetPlayerByIndex(playerFollowIndex);

            warriorData.SetPlayerFollowIndex(playerFollowIndex, ref data);
            warriorData.SetFormationID(formationID, ref data);
            warriorData.SetFormationIndex(formationIndex, ref data);
            warriorData.SetState(ENPCState.Spawning, ref data);
            SpawnNPC(ref data);
        }

        public void SpawnNPCFromSave(FNonPlayerCharacterSaveState saveState)
        {
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
            {
                Debug.Log("Cannot spawn, I'm not the master client");
                return;
            }

            NonPlayerCharacterReplicator replicator = GetReplicatorWithFreeSlots();
            if (replicator == null)
                return;

            int freeIndex = replicator.GetFreeIndex();
            if (freeIndex == -1)
            {
                Debug.Log("Can't Spawn NPC No Free Index");
                return;
            }

            FNonPlayerCharacterData data = new FNonPlayerCharacterData();
            data.Configuration = saveState.configuration;
            data.Position = saveState.position;
            data.Rotation = saveState.rotation;
            data.Condition = (byte)saveState.condition;
            data.Events = (ushort)saveState.events;
            data.CarriedItem = saveState.carriedItem.ToNetworkItem();

            _deltaStates[freeIndex] = data; // Store full state for persistence
            replicator.SpawnNPC(ref data, freeIndex);
        }

        public NonPlayerCharacterReplicator GetReplicatorWithFreeSlots()
        {
            foreach (var replicator in _replicators)
            {
                if (replicator.HasFreeIndex())
                    return replicator;
            }

            if (_replicators.Count < NonPlayerCharacterConstants.MAX_REPLICATORS)
            {
                var newReplicator = Runner.Spawn(_replicatorPrefab, Vector3.zero, Quaternion.identity);
                if (newReplicator != null)
                {
                    AddReplicator(newReplicator);
                    return newReplicator;
                }
            }

            Debug.Log("No replicator with free slots found");
            return null;
        }

        public void LoadNPCsFromSaves()
        {
            List<FNonPlayerCharacterSaveState> loadedNPCs =
               Context.WorldSaveLoadManager.LoadedNPCs;

            foreach (var npc in loadedNPCs)
            {
                SpawnNPCFromSave(npc);
            }
        }

        // Called on match disconnect on the host to save npcs
        public List<FNonPlayerCharacterSaveState> GetAllSaveStates()
        {
            var allSaves = new List<FNonPlayerCharacterSaveState>((NonPlayerCharacterConstants.MAX_NPC_REPS *
                NonPlayerCharacterConstants.MAX_REPLICATORS));

            foreach (var replicator in _replicators)
            {
                allSaves.AddRange(replicator.GetSaveStates());
            }

            return allSaves;
        }

        public void DespawnAllInvaders()
        {
            if (!HasStateAuthority)
                return;

            foreach (var replicator in _replicators)
            {
                replicator.DespawnInvaders();
            }
        }

        public void SetInvaderAttitude(EAttitude newAttitude)
        {
            if (!HasStateAuthority)
                return;

            foreach (var replicator in _replicators)
            {
                replicator.SetInvaderAttitude(newAttitude);
            }
        }
    }
}
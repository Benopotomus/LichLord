using Fusion;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{

    public class NonPlayerCharacterReplicator : ContextBehaviour, IStateAuthorityChanged
    {
        public class NPCLoadState
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
        }

        public enum ELoadState
        {
            None,
            Loading,
            Loaded,
            Unloading,
        }

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        // Local data for clients to predict changes
        private List<FNonPlayerCharacterData> _localNpcDatas = new List<FNonPlayerCharacterData>();

        [SerializeField] private NonPlayerCharacterSpawner _spawner;

        private List<NPCLoadState> _loadStates = new List<NPCLoadState>();
        private HashSet<int> _freeIndices = new HashSet<int>();

        private Dictionary<int, FNonPlayerCharacterData> _predictedDatas = new Dictionary<int, FNonPlayerCharacterData>();

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamageToNPC(int guid, int damage)
        {
            // Data is updated on authority and here
            if (HasStateAuthority)
                ApplyDamage(guid, Vector3.zero, damage);

            if (HasStateAuthority)
                return;
                
            var targetData = _npcDatas.Get(guid);

            if (_predictedDatas.TryGetValue(guid, out FNonPlayerCharacterData predictedData))
            {
                predictedData.Health = Mathf.Clamp(predictedData.Health - damage, 0, 1000);

                if (predictedData.Health <= 0)
                {
                    predictedData.State = ENonPlayerState.Inactive;
                }
            }
            else
            {
                FNonPlayerCharacterData newData = new FNonPlayerCharacterData();
                newData.Copy(ref targetData);
                newData.Health = Mathf.Clamp(newData.Health - damage, 0, 1000);

                //if (newData.Health <= 0)
                {
                    newData.State = ENonPlayerState.Inactive;
                }

                _predictedDatas.Add(guid, newData);
            }
        }

        public override void Spawned()
        {
            base.Spawned();

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                _localNpcDatas.Add(new FNonPlayerCharacterData());
                _loadStates.Add(new NPCLoadState());
                _freeIndices.Add(i); // Initially, all indices are free
            }

            Context.NonPlayerCharacterManager.AddReplicator(this);
            _spawner.OnSpawned += OnNonPlayerCharacterSpawned;

            if (HasStateAuthority)
            {
                RebuildFreeIndices();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _spawner.OnSpawned -= OnNonPlayerCharacterSpawned;
        }

        public void StateAuthorityChanged()
        {
            Debug.Log($"StateAuthority Changed, HasStateAuthority: {HasStateAuthority}");
            if (HasStateAuthority)
            {
                RebuildFreeIndices();
            }
        }

        private void RebuildFreeIndices()
        {
            _freeIndices.Clear();
            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                var data = _npcDatas.GetRef(i);
                if (!NonPlayerCharacterDataUtility.IsActive(data))
                {
                    _freeIndices.Add(i);
                }
            }
        }

        public bool TryGetNPCData(int index, out FNonPlayerCharacterData data)
        {
            data = _npcDatas.GetRef(index);
            return true;
        }

        public void UpdateNPCData(FNonPlayerCharacterData updatedData)
        {
            int index = NonPlayerCharacterDataUtility.GetGUID(ref updatedData);
            var currentData = _npcDatas.GetRef(index);
            bool wasActive = NonPlayerCharacterDataUtility.IsActive(currentData);
            bool willBeActive = NonPlayerCharacterDataUtility.IsActive(updatedData);

            _npcDatas.Set(index, updatedData);

            if (!willBeActive && wasActive)
            {
                _freeIndices.Add(index); // NPC became inactive
            }
            else if (willBeActive && !wasActive)
            {
                _freeIndices.Remove(index); // NPC became active
            }
        }

        public void UpdateNPCData(FNonPlayerCharacterSpawnParams spawnParams)
        {
            FNonPlayerCharacterData data = new FNonPlayerCharacterData();
            NonPlayerCharacterDefinition definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(spawnParams.definitionId);
            if (definition != null)
            {
                NonPlayerCharacterDataUtility.InitializeData(ref data, definition, spawnParams.index, spawnParams.teamID);
                data.Position = spawnParams.position;
                data.Rotation = spawnParams.rotation;
                _npcDatas.Set(spawnParams.index, data);
            }
        }

        public bool HasFreeIndex()
        {
            return GetFreeIndex() >= 0;
        }

        public int GetFreeIndex()
        {
            if (_freeIndices.Count > 0)
            {
                return _freeIndices.First();
            }
            return -1;
        }

        public override void Render()
        {
            base .Render();

            if (!Context.IsGameplayActive())
                return;

            if (!PlayerCreature.TryGetLocalPlayer(Runner, out PlayerCreature playerCreature))
                return;

            Vector3 viewPosition = playerCreature.transform.position;
            float renderDeltaTime = Time.deltaTime;
            float ping = (float)Runner.GetPlayerRtt(playerCreature.Object.StateAuthority);
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                FNonPlayerCharacterData usedData = _npcDatas.Get(i);

                if (!hasAuthority)
                {
                    bool hasPredictedData = _predictedDatas.TryGetValue(i, out var predictedData);
                    var localData = _localNpcDatas[i];

                    // Check for if local data has changed
                    if (localData.Health != usedData.Health ||
                        localData.State != usedData.State)
                    {
                        Debug.Log("Local Health: " + _localNpcDatas[i].Health + " Master Health: " + usedData.Health);

                        // local data changed
                        if (hasPredictedData)
                        {
                            hasPredictedData = false;
                            _predictedDatas.Remove(i);
                        }

                        localData.Health = usedData.Health;
                        localData.State = usedData.State;

                        _localNpcDatas[i] = localData;
                    }
                    else

                    if (hasPredictedData)
                    {
                        Debug.Log("Using Predicted Data " + predictedData.State);
                        usedData = predictedData;
                    }
                }


                bool shouldBeActive = usedData.IsActive();

                NPCLoadState loadState = _loadStates[i];

                if (shouldBeActive && loadState.LoadState == ELoadState.None)
                {
                    loadState.LoadState = ELoadState.Loading;
                    _spawner.SpawnNPC(ref usedData);
                }
                else if (shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    if (hasAuthority)
                        loadState.NPC.AuthorityUpdate(ref usedData, renderDeltaTime);
                    else
                        loadState.NPC.RemoteUpdate(ref usedData, renderDeltaTime, ping);
                }
                else if (!shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    DespawnNPC(i);
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Context.IsGameplayActive())
                return;

            if (!PlayerCreature.TryGetLocalPlayer(Runner, out PlayerCreature playerCreature))
                return;

            int tick = Runner.Tick;

            float ping = (float)Runner.GetPlayerRtt(playerCreature.Object.StateAuthority);
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                 FNonPlayerCharacterData data = _npcDatas.Get(i);
                NPCLoadState loadState = _loadStates[i];

                if (loadState.LoadState == ELoadState.Loaded)
                {
                    loadState.NPC.OnFixedUpdate(ref data, tick);
                }
            }
        }

        private void DespawnNPC(int index)
        {
            NPCLoadState loadState = _loadStates[index];
            if (loadState.LoadState == ELoadState.Loaded)
            {
                loadState.NPC.StartRecycle();
                loadState.LoadState = ELoadState.None;
            }
        }

        private void OnNonPlayerCharacterSpawned(FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacter character)
        {
            NPCLoadState loadState = _loadStates[spawnParams.index];
            loadState.NPC = character;
            loadState.LoadState = ELoadState.Loaded;
            character.OnSpawned(ref spawnParams, Context.NonPlayerCharacterManager, this);
        }

        public void ApplyDamage(int index, Vector3 impulse, int damage)
        {
            NPCLoadState loadState = _loadStates[index];
            if (loadState.LoadState == ELoadState.Loaded)
            {
                loadState.NPC.ApplyDamage(impulse, damage);
            }
        }
    }
}
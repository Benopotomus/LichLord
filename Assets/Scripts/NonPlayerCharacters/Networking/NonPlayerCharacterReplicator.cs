using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterReplicator : ContextBehaviour, IStateAuthorityChanged
    {
        public struct NPCLoadState
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
        }

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        [SerializeField] private NonPlayerCharacterSpawner _spawner;

        private NPCLoadState[] _loadStates = new NPCLoadState[NonPlayerCharacterConstants.MAX_NPC_REPS];
        public NPCLoadState[] LoadStates => _loadStates;

        private HashSet<int> _freeIndices = new HashSet<int>();

        // Prediction
        private Dictionary<int, NonPlayerCharacterState> _predictedStates = new Dictionary<int, NonPlayerCharacterState>();
        private List<FNonPlayerCharacterData> _localNpcDatas = new List<FNonPlayerCharacterData>();

        public void Predict_DealDamageToNPC(int guid, int damage, int hitReactIndex)
        {
            var targetData = _npcDatas.Get(guid);

            if (_predictedStates.TryGetValue(guid, out var predictedData))
            {
                predictedData.ApplyDamage(damage, hitReactIndex);
                Debug.Log("Guid: " + guid + ", Applying Predicted State " + 
                    predictedData.Data.State +
                    " Anim " + predictedData.Data.AnimationIndex +
                    ", tick: " + Runner.Tick);
            }
            else
            {
                var newPredictedData = new NonPlayerCharacterState(ref targetData);
                newPredictedData.ApplyDamage(damage, hitReactIndex);
                Debug.Log("Guid: " + guid + ", Applying Predicted State " + 
                    newPredictedData.Data.State +
                    " Anim " + newPredictedData.Data.AnimationIndex +
                    ", tick: " + Runner.Tick);

                _predictedStates.Add(guid, newPredictedData);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamageToNPC(int guid, int damage, int hitReactIndex)
        {
            ApplyDamage(guid, damage, hitReactIndex);
        }

        public override void Spawned()
        {
            base.Spawned();

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                _localNpcDatas.Add(new FNonPlayerCharacterData());
                _loadStates[i] = new NPCLoadState();
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
                if (!NonPlayerCharacterDataUtility.IsActive(ref data))
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

        public void UpdateNPCData(ref FNonPlayerCharacterData updatedData)
        {
            int index = NonPlayerCharacterDataUtility.GetGUID(ref updatedData);
            ref FNonPlayerCharacterData currentData = ref _npcDatas.GetRef(index);
            bool wasActive = NonPlayerCharacterDataUtility.IsActive(ref currentData);
            bool willBeActive = NonPlayerCharacterDataUtility.IsActive(ref updatedData);

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
                NonPlayerCharacterDataUtility.InitializeData(ref data, definition, spawnParams.index, spawnParams.teamId);
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
            base.Render();

            if (!Context.IsGameplayActive())
                return;

            if (!PlayerCharacter.TryGetLocalPlayer(Runner, out PlayerCharacter playerCreature))
                return;

            Vector3 viewPosition = playerCreature.transform.position;
            float renderDeltaTime = Time.deltaTime;
            int tick = Runner.Tick;
            float ping = (float)Runner.GetPlayerRtt(playerCreature.Object.StateAuthority);
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                // write to the data "_renderWriteData"
                GetRenderData(hasAuthority, i);

                bool shouldBeActive = NonPlayerCharacterDataUtility.GetNPCState(ref _renderWriteData) != ENonPlayerState.Inactive;

                ref NPCLoadState loadState = ref _loadStates[i];

                if (shouldBeActive && loadState.LoadState == ELoadState.None)
                {
                    loadState.LoadState = ELoadState.Loading;
                    _spawner.SpawnNPC(ref _renderWriteData);
                }
                else if (shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    if (hasAuthority)
                        loadState.NPC.AuthorityUpdate(ref _renderWriteData, renderDeltaTime, tick);
                    else
                        loadState.NPC.RemoteUpdate(ref _renderWriteData, renderDeltaTime, ping);
                }
                else if (!shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    DespawnNPC(i);
                }
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Runner.IsForward || 
                !Runner.IsFirstTick ||
                !Context.IsGameplayActive())
                return;

            int tick = Runner.Tick;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                ref FNonPlayerCharacterData data = ref _npcDatas.GetRef(i);
                ref NPCLoadState loadState = ref _loadStates[i];

                if (loadState.LoadState == ELoadState.Loaded)
                {
                    loadState.NPC.OnFixedUpdate(ref data, tick);
                }
            }
        }

        private void DespawnNPC(int index)
        {
            ref NPCLoadState loadState = ref _loadStates[index];
            if (loadState.LoadState == ELoadState.Loaded)
            {
                loadState.NPC.StartRecycle();
                loadState.LoadState = ELoadState.None;
            }
        }

        private void OnNonPlayerCharacterSpawned(FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacter character)
        {
            ref NPCLoadState loadState = ref _loadStates[spawnParams.index];
            loadState.NPC = character;
            loadState.LoadState = ELoadState.Loaded;
            character.OnSpawned(ref spawnParams, this);
        }

        public void ApplyDamage(int index, int damage, int hitReactIndex)
        {
            if (TryGetNPCData(index, out FNonPlayerCharacterData data))
            {
                //Debug.Log("Apply Damage " + damage);
                NonPlayerCharacterDataUtility.ApplyDamage(ref data, data.Definition, damage, hitReactIndex);
                UpdateNPCData(ref data);
            }
        }

        private FNonPlayerCharacterData _renderWriteData = new FNonPlayerCharacterData();

        public void GetRenderData(bool hasAuthority, int index)
        {
            ref FNonPlayerCharacterData authorityData = ref _npcDatas.GetRef(index);
            _renderWriteData.Copy(ref authorityData);

            // If we are the authority, we dont need to handle prediction
            if (hasAuthority)
                return;

            // Check for predicted data
            bool hasPredictedData = _predictedStates.TryGetValue(index, out var predictedState);
            FNonPlayerCharacterData localData = _localNpcDatas[index];

            // Check for if local data has changed
            if (localData.State != authorityData.State ||
                localData.Health != authorityData.Health ||
                localData.AnimationIndex != authorityData.AnimationIndex)
            {
                Debug.Log("Local Health: " + localData.Health + 
                    ", Master Health: " + authorityData.Health +
                    ", Local State: " + localData.State + 
                    ", Master State: " + authorityData.State);
                
                // local data changed
                if (hasPredictedData)
                {
                    hasPredictedData = false;
                    _predictedStates.Remove(index);
                    Debug.Log("Guid: " + predictedState.Data.GUID + ", Clearing Predicted State " + predictedState.Data.State + 
                        " Anim " + predictedState.Data.AnimationIndex +
                        ", tick: " + Runner.Tick);
                }

                localData.Health = _renderWriteData.Health;
                localData.State = _renderWriteData.State;
                localData.AnimationIndex = _renderWriteData.AnimationIndex;

                _localNpcDatas[index] = localData;
            }

            if (hasPredictedData)
            {
                FNonPlayerCharacterData predictedData = predictedState.Data;

                // Keep the position updates consistent with the authority
                predictedData.PositionX = authorityData.PositionX;
                predictedData.PositionY = authorityData.PositionY;
                predictedData.PositionZ =  authorityData.PositionZ;
                predictedState.CopyData(ref predictedData);

                _renderWriteData.Copy(ref predictedData);
            }
        }
    }
}
using Fusion;
using LichLord.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public partial class NonPlayerCharacterReplicator : ContextBehaviour, IStateAuthorityChanged
    {
        [Serializable]
        public struct FNPCLoadState
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
        }

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        [OnChangedRender(nameof(OnRep_NPCDatas))]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        [SerializeField] private NonPlayerCharacterSpawner _spawner;

        [SerializeField]
        private FNPCLoadState[] _loadStates = new FNPCLoadState[NonPlayerCharacterConstants.MAX_NPC_REPS];
        public FNPCLoadState[] LoadStates => _loadStates;

        // Prediction
        private Dictionary<int, NonPlayerCharacterRuntimeState> _predictedStates = new Dictionary<int, NonPlayerCharacterRuntimeState>();
        private NonPlayerCharacterRuntimeState[] _localRuntimeStates = 
            new NonPlayerCharacterRuntimeState[NonPlayerCharacterConstants.MAX_NPC_REPS];
        
        [SerializeField] private int _activeNPCs;

        [SerializeField] private LayerMask hitMask = ~0; // used to ground npcs on replication

        public override void Spawned()
        {
            base.Spawned();

            Context.NonPlayerCharacterManager.AddReplicator(this);
            _spawner.OnSpawned += OnNonPlayerCharacterSpawned;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                _loadStates[i] = new FNPCLoadState();
                _localRuntimeStates[i] = new NonPlayerCharacterRuntimeState(this, i);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _spawner.OnSpawned -= OnNonPlayerCharacterSpawned;
        }

        // The save data needs the network data and the NPC for specific authority information
        public List<FNonPlayerCharacterSaveState> GetSaveStates()
        {
            List<FNonPlayerCharacterSaveState> saves = new List<FNonPlayerCharacterSaveState>();

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                if (_loadStates[i].LoadState == ELoadState.Loaded)
                {
                    if (_loadStates[i].NPC == null)
                        continue;

                    FNonPlayerCharacterSaveState saveState = new FNonPlayerCharacterSaveState(_loadStates[i].NPC, _npcDatas.Get(i));

                    saves.Add(saveState);
                }
            }

            return saves;
        }

        public void StateAuthorityChanged()
        {
            Debug.Log($"StateAuthority Changed, HasStateAuthority: {HasStateAuthority}");
            if (!HasStateAuthority)
                return;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                if (_loadStates[i].LoadState == ELoadState.Loaded)
                {
                    _loadStates[i].NPC.Movement.OnStateAuthorityChanged(true);
                }
            }
        }

        public void SpawnNPC(ref FNonPlayerCharacterData data, int index)
        { 
            _npcDatas.Set(index, data);
            _localRuntimeStates[index].CopyData(ref data);
        }

        public void ReplicateRuntimeState(NonPlayerCharacterRuntimeState runtimeState)
        {
            if (!HasStateAuthority)
                return;

            _npcDatas.Set(runtimeState.Index, runtimeState.Data);
        }

        public bool HasFreeIndex()
        {
            return GetFreeIndex() >= 0;
        }

        public int GetFreeIndex()
        {
            for(int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                if (_localRuntimeStates[i].GetStateFromData(ref _npcDatas.GetRef(i)) == ENPCState.Inactive)
                    return i;
            }

            return -1;
        }

        public override void Render()
        {
            base.Render();

            if (!Context.IsGameplayActive())
                return;

            var playerCreature = Context.LocalPlayerCharacter;
            if (playerCreature == null)
                return;

            Vector3 viewPosition = playerCreature.transform.position;
            float renderDeltaTime = Time.deltaTime;
            int tick = Runner.Tick;
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            if (!hasAuthority)
                TimeoutPredictedStates(tick);

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                var renderState = GetRenderState(hasAuthority, i);
                var renderStateData = renderState.Data;

                bool shouldBeActive = renderState.IsActive();

                ref FNPCLoadState loadState = ref _loadStates[i];

                if (shouldBeActive)
                {
                    //Debug.Log("Active Index " + i);
                }

                if (shouldBeActive && loadState.LoadState == ELoadState.None)
                {
                    loadState.LoadState = ELoadState.Loading;
                    _spawner.SpawnNPC(ref renderStateData, i);
                }
                else if (shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    loadState.NPC.OnRender(renderState, 
                        hasAuthority, 
                        renderDeltaTime, 
                        tick);
                }
                else if (!shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    DespawnNPCGameObject(i);
                }
            }
        }

        int _timeoutPredictionTick = -1;
        private void TimeoutPredictedStates(int tick)
        {
            if (_timeoutPredictionTick == tick)
                return;

            _timeoutPredictionTick = tick;

            // Remove expired states in one go
            var keysToRemove = new List<int>(_predictedStates.Count);

            foreach (var (key, state) in _predictedStates)
            {
                if (tick > state.PredictionTimeoutTick)
                    keysToRemove.Add(key);
            }

            for (int i = 0; i < keysToRemove.Count; i++)
                _predictedStates.Remove(keysToRemove[i]);
        }

        private void DespawnNPCGameObject(int index)
        {
            ref FNPCLoadState loadState = ref _loadStates[index];
            if (loadState.LoadState == ELoadState.Loaded)
            {
                loadState.NPC.StartRecycle();
                loadState.LoadState = ELoadState.None;
            }
        }

        private void OnNonPlayerCharacterSpawned(FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacter character)
        {
            ref FNPCLoadState loadState = ref _loadStates[spawnParams.Index];
            loadState.NPC = character;
            loadState.LoadState = ELoadState.Loaded;

            _localRuntimeStates[spawnParams.Index].SetPosition(spawnParams.Position);

            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;
            int tick = Runner.Tick;

            character.OnSpawned(_localRuntimeStates[spawnParams.Index], this, hasAuthority, tick);
        }

        private void OnRep_NPCDatas()
        {
            _activeNPCs = 0;
            int tick = Runner.Tick;
            const int raycastInterval = 1; // Extracted magic number for clarity

            // Cache frequently accessed values
            bool hasStateAuthority = HasStateAuthority;
            
            if (hasStateAuthority)
                return;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                ref FNonPlayerCharacterData networkedData = ref _npcDatas.GetRef(i); // Use ref readonly for performance
                ref var localState = ref _localRuntimeStates[i]; // Use ref to avoid struct copying

                var newNetworkedState = localState.GetStateFromData(ref networkedData);

                // Early state check
                if (newNetworkedState != ENPCState.Inactive)
                {
                    _activeNPCs++;
                }

                ENPCState oldState = localState.GetState();
                
                bool needsRaycast = !hasStateAuthority &&
                                   (tick + i) % raycastInterval == 0 &&
                                   localState.GetPosition() != networkedData.Position &&
                                   newNetworkedState != ENPCState.Inactive;
                
                Vector3 hitPosition = Vector3.zero;
                bool raycastHit = false;

                if (needsRaycast)
                {
                    // Combine offset into raycast origin
                    if (Physics.Raycast(networkedData.Position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f, hitMask))
                    {
                        raycastHit = true;
                        hitPosition = hit.point;
                    }
                }

                bool hasStateChanged = oldState != newNetworkedState;
                localState.CopyData(ref networkedData); // Ensure this method accepts ref parameter

                if (raycastHit)
                {
                    localState.SetPosition(hitPosition);
                }

                if (hasStateChanged &&
                    _predictedStates.TryGetValue(i, out NonPlayerCharacterRuntimeState predictedState))
                {
                    if ((localState.GetState() == predictedState.GetState() &&
                         localState.GetAnimationIndex() == predictedState.GetAnimationIndex()) ||
                        localState.GetState() == ENPCState.Dead)
                    {
                        _predictedStates.Remove(i);
                    }
                }
            }
        }

        public NonPlayerCharacterRuntimeState GetRenderState(bool hasAuthority, int index)
        {
            var localState = _localRuntimeStates[index];

            // If we are the authority, we dont need to handle prediction
            if (!hasAuthority)
            {
                // Check for predicted data
                if (_predictedStates.TryGetValue(index, out var predictedState))
                {
                    //Debug.Log("Using predicted state " + predictedState.GetState() + " Anim: " + predictedState.GetAnimationIndex() + " index: " + index);
                    var predictedStateData = predictedState.Data;
                    predictedStateData.Position = localState.GetPosition();
                    predictedStateData.RawCompressedYaw = localState.GetRawCompressedYaw();

                    predictedState.CopyData(ref predictedStateData);
                    return predictedState;
                }
            }

            return localState;
        }

        public void DespawnInvaders()
        {
            if (!HasStateAuthority)
                return;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                NonPlayerCharacterRuntimeState curState = _localRuntimeStates[i];

                if (curState.IsInvader())
                    curState.SetState(ENPCState.Inactive);
            }
        }

        public void SetInvaderAttitude(EAttitude newAttitude)
        {
            if (!HasStateAuthority)
                return;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                NonPlayerCharacterRuntimeState curState = _localRuntimeStates[i];

                if (curState.IsInvader())
                    curState.SetAttitude(newAttitude);
            }
        }
    }
}
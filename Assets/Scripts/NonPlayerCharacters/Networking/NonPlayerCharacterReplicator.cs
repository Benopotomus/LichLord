using Fusion;
using LichLord.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterReplicator : ContextBehaviour, IStateAuthorityChanged
    {
        public struct FNPCLoadState
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
        }

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        [OnChangedRender(nameof(OnRep_NPCDatas))]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        [SerializeField] private NonPlayerCharacterSpawner _spawner;

        private FNPCLoadState[] _loadStates = new FNPCLoadState[NonPlayerCharacterConstants.MAX_NPC_REPS];
        public FNPCLoadState[] LoadStates => _loadStates;

        // Prediction
        private Dictionary<int, NonPlayerCharacterRuntimeState> _predictedStates = new Dictionary<int, NonPlayerCharacterRuntimeState>();
        private NonPlayerCharacterRuntimeState[] _localRuntimeStates = 
            new NonPlayerCharacterRuntimeState[NonPlayerCharacterConstants.MAX_NPC_REPS];

        public void Predict_DealDamageToNPC(int index, int damage, int hitReactIndex)
        {
            var targetData = _npcDatas.Get(index);

            int predictionTicks = (int)(32.0f * (Runner.GetPlayerRtt(Context.LocalPlayerRef) * 4f));

            if (_predictedStates.TryGetValue(index, out NonPlayerCharacterRuntimeState predictedState))
            {
                predictedState.ApplyDamage(damage, hitReactIndex);
                predictedState.PredictionTimeoutTick = Runner.Tick + predictionTicks;
            }
            else
            {
                NonPlayerCharacterRuntimeState newPredictedState = new NonPlayerCharacterRuntimeState(this,index, ref targetData);
                newPredictedState.ApplyDamage(damage, hitReactIndex);
                newPredictedState.PredictionTimeoutTick = Runner.Tick + predictionTicks;
                _predictedStates[index] = newPredictedState;

                //Debug.Log("Predicted State " + newPredictedState.GetState() + "Anim: " + newPredictedState.GetAnimationIndex());
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamageToNPC(int index, int damage, int hitReactIndex)
        {
            _localRuntimeStates[index].ApplyDamage(damage, hitReactIndex);
        }

        public override void Spawned()
        {
            base.Spawned();

            Context.NonPlayerCharacterManager.AddReplicator(this);
            _spawner.OnSpawned += OnNonPlayerCharacterSpawned;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                _loadStates[i] = new FNPCLoadState();
                _localRuntimeStates[i] = new NonPlayerCharacterRuntimeState(this, i, ref _npcDatas.GetRef(i));
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
        }

        public bool GetRenderState(int index, out FNonPlayerCharacterData data)
        {
            data = _npcDatas.GetRef(index);
            return true;
        }

        public void ReplicateRuntimeState(NonPlayerCharacterRuntimeState runtimeState)
        {
            if (!HasStateAuthority)
                return;

            ref FNonPlayerCharacterData oldData = ref _npcDatas.GetRef(runtimeState.Index);
            FNonPlayerCharacterData newData = runtimeState.Data;
            oldData.Copy(runtimeState.Data);
        }

        public bool HasFreeIndex()
        {
            return GetFreeIndex() >= 0;
        }

        public int GetFreeIndex()
        {
            for(int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                if (_npcDatas.GetRef(i).State == ENonPlayerState.Inactive)
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
            float ping = (float)Runner.GetPlayerRtt(playerCreature.Object.StateAuthority);
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            if (!hasAuthority)
                TimeoutPredictedStates(tick);

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                var renderState = GetRenderState(hasAuthority, i);
                var renderStateData = renderState.Data;

                bool shouldBeActive = renderState.IsActive();

                ref FNPCLoadState loadState = ref _loadStates[i];

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
                        ping, 
                        tick);
                }
                else if (!shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    DespawnNPC(i);
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

        private void DespawnNPC(int index)
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
            character.OnSpawned(ref spawnParams, this);
        }


        private void OnRep_NPCDatas()
        {
            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++) 
            {
                // get the data
                ref FNonPlayerCharacterData authorityData = ref _npcDatas.GetRef(i);

                // check against the local runtime state for that index
                var localState = _localRuntimeStates[i];
                var oldState = localState.GetState();

                bool hasChanged = oldState != authorityData.State;
                localState.CopyData(ref authorityData);

                if (hasChanged)
                {
                    if (_predictedStates.TryGetValue(i, out NonPlayerCharacterRuntimeState predictedState))
                    {
                        if ((localState.GetState() == predictedState.GetState() && 
                            localState.GetAnimationIndex() == predictedState.GetAnimationIndex()) ||
                            localState.GetState() == ENonPlayerState.Dead)
                        {
                            _predictedStates.Remove(i);
                        }
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
    }
}
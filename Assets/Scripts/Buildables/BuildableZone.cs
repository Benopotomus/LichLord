using Fusion;
using LichLord.Props;
using LichLord.World;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    [RequireComponent(typeof(CapsuleCollider))]
    public partial class BuildableZone : ContextBehaviour
    {
        [SerializeField] private CapsuleCollider _trigger;

        private BuildableSpawner _spawner = new BuildableSpawner();

        [Networked, Capacity(BuildableConstants.MAX_BUILDABLE_REPS)]
        protected virtual NetworkArray<FBuildableData> _buildableDatas { get; }
        public NetworkArray<FBuildableData> Data => _buildableDatas;

        private FBuildableLoadState[] _buildableLoadStates;

        private Dictionary<int, BuildableRuntimeState> _buildableRuntimeStates = new Dictionary<int, BuildableRuntimeState>();

        public override void Spawned()
        {
            base.Spawned();
            _spawner.OnBuildableSpawned += OnBuildableSpawned;
            _buildableLoadStates = new FBuildableLoadState[BuildableConstants.MAX_BUILDABLE_REPS];
        }

        public void SetTriggerSize(float size)
        {
            _trigger.radius = size;
        }

        int _lastAuthorityTick = -1;

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            base.Render();

            bool hasAuthority = HasStateAuthority;
            float renderDeltaTime = Time.deltaTime;
            int tick = Runner.Tick;

            for (int i = 0; i < _buildableDatas.Length; i++)
            {
                var loadstate = _buildableLoadStates[i];
                ref FBuildableData data = ref _buildableDatas.GetRef(i);
                int definitionID = data.DefinitionID;
                BuildableRuntimeState runtimeState = GetRenderState(i, ref data);
                BuildableDefinition definition = Global.Tables.BuildableTable.TryGetDefinition(definitionID);
                bool shouldBeLoaded = definitionID > 0;

                if (hasAuthority &&
                    _lastAuthorityTick != tick)
                {

                    if(runtimeState.AuthorityUpdate(tick))
                        ReplicateRuntimeState(runtimeState);
                }

                if (shouldBeLoaded)
                {
                    switch (loadstate.LoadState)
                    {
                        case ELoadState.None:

                            _buildableLoadStates[i].LoadState = ELoadState.Loading;
                            
                            _spawner.SpawnBuildable(this,
                                i,
                                definition,
                                data.Transform.Position,
                                data.Transform.Rotation,
                                data.StateData
                                );

                            break;

                        case ELoadState.Loaded:

                            loadstate.Buildable.OnRender(runtimeState, renderDeltaTime, hasAuthority);

                            break;
                    }
                }
                else
                {
                    if (loadstate.LoadState == ELoadState.Loaded)
                    {
                        loadstate.LoadState = ELoadState.None;
                        loadstate.Buildable.StartRecycle();
                        loadstate.Buildable = null;
                        _buildableLoadStates[i] = loadstate;
                    }
                }
            }

            _lastAuthorityTick = tick;
        }

        private void OnBuildableSpawned(int index, Buildable buildable)
        {
            _buildableLoadStates[index].Buildable = buildable;
            _buildableLoadStates[index].LoadState = ELoadState.Loaded;
 
            ref FBuildableData data = ref _buildableDatas.GetRef(index);
            _buildableRuntimeStates[index] = new BuildableRuntimeState(index, ref data);

            buildable.OnSpawned(this, _buildableRuntimeStates[index]);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_PlaceBuildable(ushort definitionID, FWorldTransform worldTransform)
        {
            int freeIndex = GetFreeIndex();

            if (freeIndex < 0)
            {
                Debug.Log("No free index at buildable");
                return;
            }

            // Sweep at location to make sure it can be placed
            
            // If i hit something, we have to make sure its not a valid snapping position

            // Determine if theres any connectors near my connectors

            ref FBuildableData data = ref _buildableDatas.GetRef(freeIndex);
             
            data.DefinitionID = definitionID;
            data.Transform = worldTransform;

            BuildableDefinition definition = Global.Tables.BuildableTable.TryGetDefinition(data.DefinitionID);

            if (definition == null)
            {
                Debug.LogWarning("No valid definition " + definitionID);
                return;
            }

            definition.BuildableDataDefinition.InitializeData(ref data, definition);

            if (definition.BuildableDataDefinition is StockpileDataDefinition stockpileDataDefinition)
            {
                int freeStockpileIndex = Context.ContainerManager.FindFreeStockpileIndex();
                stockpileDataDefinition.SetStockpileIndex(freeStockpileIndex, ref data);
                Context.ContainerManager.AssignStockpileIndex(freeStockpileIndex);
            }
        }

        public int GetFreeIndex()
        {
            for (int i = 0; i < _buildableDatas.Length; i++)
            {
                if (_buildableDatas.GetRef(i).DefinitionID == 0)
                    return i;

            }
            return -1;
        }

        public void LoadBuildables(FBuildableSaveState[] buildableSaveStates)
        {
            for (int i = 0; i < buildableSaveStates.Length; i++)
            {
                FBuildableSaveState saveState = buildableSaveStates[i];
                ref FBuildableData data = ref _buildableDatas.GetRef(i);

                data.LoadFromSave(saveState);
            }
        }

        private BuildableRuntimeState GetRenderState(int index, ref FBuildableData data)
        {
            if (_buildableRuntimeStates.TryGetValue(index, out BuildableRuntimeState state)) 
            {
                state.CopyData(ref data);
                return state;
            }

            _buildableRuntimeStates[index] = new BuildableRuntimeState(index, ref data);
            return _buildableRuntimeStates[index];
        }
    }
}

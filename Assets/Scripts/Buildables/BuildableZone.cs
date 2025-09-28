using DWD.Pooling;
using Fusion;
using LichLord.Items;
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
        private VisualEffectSpawner _effectSpawner = new VisualEffectSpawner();

        [Networked, Capacity(BuildableConstants.MAX_BUILDABLE_REPS)]
        protected virtual NetworkArray<FBuildableData> _buildableDatas { get; }
        public NetworkArray<FBuildableData> Data => _buildableDatas;

        private FBuildableLoadState[] _buildableLoadStates;
        public FBuildableLoadState[] LoadStates => _buildableLoadStates;

        private Dictionary<int, BuildableRuntimeState> _buildableRuntimeStates = new Dictionary<int, BuildableRuntimeState>();

        public override void Spawned()
        {
            base.Spawned();
            _spawner.OnBuildableSpawned += OnBuildableSpawned;
            _buildableLoadStates = new FBuildableLoadState[BuildableConstants.MAX_BUILDABLE_REPS];

            _effectSpawner.OnLoaded += OnBuildingVisualEffectLoaded;
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

                BuildableRuntimeState runtimeState = GetRenderState(i, ref data);

                if (hasAuthority &&
                    _lastAuthorityTick != tick)
                {
                    runtimeState.AuthorityUpdateTick(tick);
                }

                int definitionID = data.DefinitionID;
                bool shouldBeLoaded = definitionID > 0;

                if (shouldBeLoaded)
                {
                    switch (loadstate.LoadState)
                    {
                        case ELoadState.None:

                            _buildableLoadStates[i].LoadState = ELoadState.Loading;
                            BuildableDefinition definition = Global.Tables.BuildableTable.TryGetDefinition(definitionID);

                            _spawner.SpawnBuildable(this,
                                i,
                                definition,
                                data.Transform.Position,
                                data.Transform.Rotation,
                                data.StateData
                                );

                            break;

                        case ELoadState.Loaded:

                            loadstate.Buildable.OnRender(runtimeState, renderDeltaTime, tick, hasAuthority);

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
            _buildableRuntimeStates[index] = new BuildableRuntimeState(this, index, ref data);

            buildable.OnSpawned(this, _buildableRuntimeStates[index]);
        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_PlaceBuildable(ushort definitionID, FWorldTransform buildableTransform)
        {
            int freeIndex = GetFreeIndex();

            if (freeIndex < 0)
            {
                Debug.Log("No free index at buildable");
                return;
            }

            BuildableDefinition definition = Global.Tables.BuildableTable.TryGetDefinition(definitionID);

            if (definition == null)
            {
                Debug.LogWarning("No valid definition " + definitionID);
                return;
            }

            if (!IsInsideTrigger(buildableTransform.Position))
                return;

            // Sweep at location to make sure it can be placed

            // If i hit something, we have to make sure its not a valid snapping position

            // Determine if theres any connectors near my connectors



            // Spawn VFX for defintion
            _effectSpawner.SpawnVisualEffect(buildableTransform.Position, buildableTransform.Rotation, definition.PlacementVFX);

            ref FBuildableData data = ref _buildableDatas.GetRef(freeIndex);

            if (definition.BuildableDataDefinition is StockpileDataDefinition stockpileDataDefinition)
            {
                int freeStockpileIndex = Context.ContainerManager.GetFreeStockpileIndex();
                if (freeStockpileIndex < 0)
                {
                    Debug.Log("No Free Stockpile Index");
                    return;
                }

                stockpileDataDefinition.InitializeData(ref data, definition);
                stockpileDataDefinition.SetStockpileIndex(freeStockpileIndex, ref data);
                Context.ContainerManager.AssignStockpileIndex(freeStockpileIndex);

            }
            else if (definition.BuildableDataDefinition is CryptDataDefinition cryptDataDefinition)
            {
                int freeWorkerIndex = Context.WorkerManager.GetFreeIndex();
                if (freeWorkerIndex < 0)
                {
                    Debug.Log("No Free Worker Index");
                    return;
                }
 
                cryptDataDefinition.InitializeData(ref data, definition);
                cryptDataDefinition.SetWorkerIndex(freeWorkerIndex, ref data);

                Context.WorkerManager.AssignWorkerIndexToBuildable(freeWorkerIndex, this, freeIndex);
            }
            else if (definition.BuildableDataDefinition is ContainerDataDefinition containerDataDefinition)
            { 
                var containerData = Context.ContainerManager.GetContainerFreeReplicatorAndIndex(definition.ContainerSlots);

                if (containerData.freeIndex < 0)
                {
                    Debug.Log("No Free Container Index");
                    return;
                }

                int fullContainerIndex = containerData.freeIndex + (containerData.replicator.Index * ItemConstants.CONTAINERS_PER_REPLICATOR);
                containerDataDefinition.InitializeData(ref data, definition);
                containerDataDefinition.SetContainerIndex(fullContainerIndex, ref data);

                Context.ContainerManager.SetupContainer(definition.ContainerSlots);
            }
            else
            {
                definition.BuildableDataDefinition.InitializeData(ref data, definition);
            }

            data.DefinitionID = definitionID;
            data.Transform = buildableTransform;
        }

        private void OnBuildingVisualEffectLoaded(GameObject loadedGameObject, Vector3 position, Quaternion rotation)
        {
            var poolObject = loadedGameObject.GetComponent<DWDObjectPoolObject>();

            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn Visuals Prefab for Impact");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAt(poolObject, position, rotation);
            if (instance is StandaloneVisualEffect standaloneEffect)
                standaloneEffect.Initialize();
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

            _buildableRuntimeStates[index] = new BuildableRuntimeState(this, index, ref data);
            return _buildableRuntimeStates[index];
        }

        public bool IsInsideTrigger(Vector3 worldPosition)
        {
            // Convert to local position relative to the capsule
            Vector3 localPoint = transform.InverseTransformPoint(worldPosition);

            // Capsule extends from -height/2 to +height/2 along Y
            float halfHeight = Mathf.Max(0, _trigger.height * 0.5f - _trigger.radius);

            // Clamp the point to capsule’s central line
            float y = Mathf.Clamp(localPoint.y, -halfHeight, halfHeight);
            Vector3 capsuleLinePoint = new Vector3(0, y, 0);

            // Distance from point to line segment, compare with radius
            float distance = Vector3.Distance(localPoint, capsuleLinePoint);
            return distance <= _trigger.radius;
        }
    }
}

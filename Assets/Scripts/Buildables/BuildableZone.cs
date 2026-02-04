using Fusion;
using LichLord.Items;
using LichLord.World;
using UnityEngine;

namespace LichLord.Buildables
{
    [RequireComponent(typeof(CapsuleCollider))]
    public partial class BuildableZone : ContextBehaviour
    {
        [SerializeField] 
        private Lair _lair;
        public Lair Lair => _lair;

        [SerializeField] 
        private CapsuleCollider _trigger;

        private BuildableSpawner _spawner = new BuildableSpawner();

        [Networked, Capacity(BuildableConstants.MAX_BUILDABLE_REPS)]
        protected virtual NetworkArray<FBuildableData> _buildableDatas { get; }
        public NetworkArray<FBuildableData> Data => _buildableDatas;
        private FBuildableData[] _localBuildableDatas;

        private FBuildableLoadState[] _loadStates;
        public FBuildableLoadState[] LoadStates => _loadStates;

        private BuildableRuntimeState[] _runtimeStates;
        public BuildableRuntimeState[] RuntimeStates => _runtimeStates;

        public override void Spawned()
        {
            base.Spawned();
            _spawner.OnBuildableSpawned += OnBuildableSpawned;
            _localBuildableDatas = new FBuildableData[BuildableConstants.MAX_BUILDABLE_REPS];

            _runtimeStates = new BuildableRuntimeState[BuildableConstants.MAX_BUILDABLE_REPS];

            for (int i = 0; i < _runtimeStates.Length; i++)
                _runtimeStates[i] = new BuildableRuntimeState(this, i, ref _localBuildableDatas[i]);

            _loadStates = new FBuildableLoadState[BuildableConstants.MAX_BUILDABLE_REPS];
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
                FBuildableLoadState loadstate = _loadStates[i];
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

                            _loadStates[i].LoadState = ELoadState.Loading;
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
                        _loadStates[i] = loadstate;
                    }
                }
            }

            _lastAuthorityTick = tick;
        }

        private void OnBuildableSpawned(int index, Buildable buildable)
        {
            _loadStates[index].Buildable = buildable;
            _loadStates[index].LoadState = ELoadState.Loaded;
 
            ref FBuildableData data = ref _buildableDatas.GetRef(index);
            _runtimeStates[index] = new BuildableRuntimeState(this, index, ref data);

            buildable.OnSpawned(this, _runtimeStates[index]);
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

            // Spawn VFX for definition
            Context.VFXManager.SpawnVisualEffect(buildableTransform.Position, buildableTransform.Rotation, definition.PlacementVFX);

            ref FBuildableData data = ref _buildableDatas.GetRef(freeIndex);

            if (definition.BuildableDataDefinition is ContainerDataDefinition containerDataDefinition)
            {
                var containerData = Context.ContainerManager.GetContainerFreeReplicatorAndIndex(definition.ContainerSlots);

                if (containerData.freeIndex < 0)
                {
                    Debug.Log("No Free Container Index");
                    return;
                }

                int fullContainerIndex = containerData.freeIndex + (containerData.replicator.Index * ContainerConstants.CONTAINERS_PER_REPLICATOR);
                containerDataDefinition.InitializeData(ref data, definition);
                containerDataDefinition.SetContainerIndex(fullContainerIndex, ref data);

                Context.ContainerManager.SetupContainer(definition.ContainerSlots, definition.IsStockpile);
            }
            else
            {
                definition.BuildableDataDefinition.InitializeData(ref data, definition);
            }

            data.DefinitionID = definitionID;
            data.Transform = buildableTransform;
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
            BuildableRuntimeState state = _runtimeStates[index];
            state.CopyData(ref data);
            return state;
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

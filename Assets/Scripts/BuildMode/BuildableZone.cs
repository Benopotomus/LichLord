using Fusion;
using LichLord.World;
using UnityEngine;

namespace LichLord.Buildables
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class BuildableZone : ContextBehaviour
    {
        [SerializeField] private CapsuleCollider _trigger;

        private BuildableSpawner _spawner = new BuildableSpawner();

        [Networked, Capacity(1024)]
        protected virtual NetworkArray<FBuildableData> _buildableDatas { get; }
        public NetworkArray<FBuildableData> Data => _buildableDatas;

        private FBuildableLoadState[] _buildableLoadStates;

        public override void Spawned()
        {
            base.Spawned();
            _spawner.OnBuildableSpawned += OnBuildableSpawned;
            _buildableLoadStates = new FBuildableLoadState[1024];
        }

        public void SetTriggerSize(float size)
        {
            _trigger.radius = size;
        }

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            base.Render();

            float renderDeltaTime = Time.deltaTime;

            for (int i = 0; i < _buildableDatas.Length; i++)
            {
                var loadstate = _buildableLoadStates[i];
                ref FBuildableData data = ref _buildableDatas.GetRef(i);
                int definitionID = data.DefinitionID;

                bool shouldBeLoaded = definitionID > 0;

                if (shouldBeLoaded)
                {
                    switch (loadstate.LoadState)
                    {
                        case ELoadState.None:

                            loadstate.LoadState = ELoadState.Loading;

                            _spawner.SpawnBuildable(this,
                                i,
                                Global.Tables.BuildableTable.TryGetDefinition(definitionID),
                                data.Transform.Position,
                                data.Transform.Rotation,
                                data.StateData
                                );

                            break;
                        case ELoadState.Loaded:
                            loadstate.Buildable.OnRender(renderDeltaTime);
                            break;
                    }
                }
                else
                {
                    if (loadstate.LoadState == ELoadState.Loaded)
                    {
                        loadstate.LoadState = ELoadState.None;
                        loadstate.Buildable.StartRecycle();
                    }
                }
            }
        }

        private void OnBuildableSpawned(int index, Buildable buildable)
        {
            _buildableLoadStates[index].Buildable = buildable;
            _buildableLoadStates[index].LoadState = ELoadState.Loaded;
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

            ref FBuildableData data = ref _buildableDatas.GetRef(freeIndex);

            data.DefinitionID = definitionID;
            data.Transform = worldTransform;
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

                data.DefinitionID = (ushort)saveState.definitionId;
                data.Position = saveState.position;
                data.Rotation = Quaternion.Euler(saveState.eulerAngles);
                data.StateData = (ushort)saveState.stateData;
            }
        }
    }
}

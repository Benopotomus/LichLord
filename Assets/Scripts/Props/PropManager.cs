using UnityEngine;
using LichLord.World;

namespace LichLord.Props
{
    public partial class PropManager : ContextBehaviour
    {
        [SerializeField] private PropSpawner _propSpawner;

        public override void Spawned()
        {
            _propSpawner.OnPropSpawned += OnPropSpawned;
        }

        public void SpawnProp(PropRuntimeState runtimeState)
        { 
            _propSpawner.SpawnProp(runtimeState);
        }

        public void LoadPropsForChunk(Chunk chunk)
        {
            ChunkMarkupData baseMarkupData = Context.WorldManager.WorldSettings.GetMarkupData(chunk.ChunkID);

            if (baseMarkupData == null)
                return;

            chunk.InitializeRuntimeStates(baseMarkupData.PropMarkupDatas);
            
            if (HasStateAuthority)
            {
                var loadedChunks = Context.WorldSaveLoadManager.LoadedChunks;

                if (loadedChunks.TryGetValue(chunk.ChunkID, out FChunkSaveData chunkSaveData))
                {
                    foreach (var savedProp in chunkSaveData.props)
                    {
                        PropRuntimeState state = chunk.PropStates[savedProp.guid];
                        FPropData savedData = new FPropData { StateData = (ushort)savedProp.stateData };
                        state.CopyData(ref savedData);
                        chunk.UpdatePropRuntimeState(state);
                        chunk.DeltaPropStates[savedProp.guid] = state;
                    }
                }
            }
            
        }

        private void OnPropSpawned(PropRuntimeState propRuntimeState, Prop prop)
        {
            Chunk chunk = propRuntimeState.chunk;

            if (propRuntimeState == null)
            {
                Debug.LogWarning("Null propRuntimeState in OnPropSpawned.", this);
                return;
            }

            int index = propRuntimeState.index;

            chunk.PropLoadStates[index].Prop = prop;
            chunk.PropLoadStates[index].LoadState = ELoadState.Loaded; 

            prop.OnSpawned(propRuntimeState, this);
        }

        public void DespawnProp(Chunk chunk, int index)
        {
            FPropLoadState propLoadState = chunk.PropLoadStates[index];

            if (propLoadState.LoadState == ELoadState.Loaded && propLoadState.Prop != null)
            {
                propLoadState.Prop.StartRecycle();
                propLoadState.LoadState = ELoadState.None;
                chunk.PropLoadStates[index] = propLoadState;
            }
        }
    }

    public struct FPropLoadState
    {
        public Prop Prop;
        public ELoadState LoadState;
    }
}
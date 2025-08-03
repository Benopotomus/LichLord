using Fusion;
using UnityEngine;
using System.Collections.Generic;
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

            Debug.Log(baseMarkupData.ChunkCoord.X + ", " + baseMarkupData.ChunkCoord.Y);
            for (int i = 0; i < baseMarkupData.PropMarkupDatas.Length; i++)
            {
                PropMarkupData propMarkupData = baseMarkupData.PropMarkupDatas[i];
                if (propMarkupData == null)
                {
                    continue;
                }

                PropRuntimeState propRuntimeState = new PropRuntimeState(
                    propMarkupData.guid,
                    chunk,
                    propMarkupData.position,
                    propMarkupData.rotation,
                    propMarkupData.propDefinitionId);

                chunk.AddPropRuntimeState(propRuntimeState); // Add to chunk's PropStates
            }

            return;
            if (HasStateAuthority)
            {
                var loadedChunks = Context.WorldSaveLoadManager.LoadedChunks;

                if (loadedChunks.TryGetValue(chunk.ChunkID, out FChunkSaveData chunkSaveData))
                {
                    foreach (var savedProp in chunkSaveData.props)
                    {
                        FPropData savedData = new FPropData { StateData = savedProp.stateData };
                        if (chunk.PropStates.TryGetValue(savedProp.guid, out PropRuntimeState state))
                        { 
                            state.CopyData(ref savedData);
                            chunk.UpdatePropRuntimeState(state);
                            chunk.DeltaPropStates[savedProp.guid] = state;
                        }
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

            int guid = propRuntimeState.guid;
            if (!chunk.PropLoadStates.TryGetValue(guid, out FPropLoadState propLoadState))
            {
                Debug.LogWarning($"Missing or null PropLoadState for GUID {guid} in OnPropSpawned.", this);
                return;
            }

            propLoadState.Prop = prop;
            propLoadState.LoadState = ELoadState.Loaded;
            chunk.PropLoadStates[guid] = propLoadState; 

            prop.OnSpawned(propRuntimeState, this);
        }

        public void DespawnProp(Chunk chunk, int guid)
        {
            if (!chunk.PropLoadStates.TryGetValue(guid, out FPropLoadState propLoadState))
            {
                Debug.LogWarning($"Missing or null PropLoadState for GUID {guid} in DespawnProp.", this);
                return;
            }

            if (propLoadState.LoadState == ELoadState.Loaded && propLoadState.Prop != null)
            {
                propLoadState.Prop.StartRecycle();
                propLoadState.LoadState = ELoadState.None;
            }
        }
    }

    public struct FPropLoadState
    {
        public Prop Prop;
        public ELoadState LoadState;
    }
}
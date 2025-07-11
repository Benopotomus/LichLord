using Fusion;
using UnityEngine;
using System.Collections.Generic;
using LichLord.World;

namespace LichLord.Props
{
    public partial class PropManager : ContextBehaviour
    {
        [SerializeField] private PropSpawner _propSpawner;

        [SerializeField] private NetworkProp _propPrefab;

        private Stack<NetworkProp> _propPool = new Stack<NetworkProp>();

        private List<NetworkProp> _activeProps = new List<NetworkProp>();

        public override void Spawned()
        {
            _propSpawner.OnPropSpawned += OnPropSpawned;
        }

        public void LoadPropsForChunk(Chunk chunk)
        {
            ChunkPropsMarkupData baseMarkupData = Context.WorldManager.WorldSettings.GetMarkupData(chunk.ChunkID);

            if (baseMarkupData == null)
                return;

            for (int i = 0; i < baseMarkupData.propMarkupDatas.Length; i++)
            {
                PropMarkupData propMarkupData = baseMarkupData.propMarkupDatas[i];
                if (propMarkupData == null || propMarkupData.propDefinition == null)
                {
                    continue;
                }

                PropRuntimeState propRuntimeState = new PropRuntimeState(
                    propMarkupData.guid,
                    chunk,
                    propMarkupData.position,
                    propMarkupData.rotation,
                    propMarkupData.propDefinition.TableID,
                    propMarkupData.terrainId);

                chunk.AddPropRuntimeState(propRuntimeState); // Add to chunk's PropStates
            }

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

        int _lastTick = -1;
        public override void Render()
        {
           //Debug.Log(_activeProps.Count);
            return;
            if (_lastTick == Runner.Tick)
                return;

            _lastTick = Runner.Tick;

            if (!Context.IsGameplayActive())
                return;

            if (!PlayerCharacter.TryGetLocalPlayer(Runner, out PlayerCharacter pc))
                return;

            float renderDeltaTime = Runner.LocalAlpha;
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            // Update states from player's cached list
            foreach (Chunk chunk in pc.CachedChunks)
            {
                foreach (var kvp in chunk.PropStates)
                {
                    int guid = kvp.Key;

                    if (!chunk.GetRenderState(hasAuthority, chunk, guid, out var usedState))
                    {
                        Debug.LogWarning($"Null PropRuntimeState for GUID {guid} in Render.", this);
                        continue;
                    }

                    // if we have no loader for guid, create one
                    if (!chunk.PropLoadStates.TryGetValue(guid, out FPropLoadState propLoadState))
                    {
                        propLoadState = new FPropLoadState();
                        chunk.PropLoadStates[guid] = propLoadState;
                    }

                    if (propLoadState.LoadState == ELoadState.None)
                    {
                        // Set the state to loading
                        propLoadState.LoadState = ELoadState.Loading;
                        chunk.PropLoadStates[guid] = propLoadState;

                        _propSpawner.SpawnProp(usedState);
                    }
                    else if (propLoadState.LoadState == ELoadState.Loaded && propLoadState.Prop != null)
                    {
                        propLoadState.Prop.OnRender(usedState, renderDeltaTime);
                    }
                    else if (propLoadState.LoadState == ELoadState.Loaded && propLoadState.Prop != null)
                    {
                        DespawnProp(chunk, guid);
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

        public void SpawnNetworkPropsForChunk(Chunk chunk)
        {
            //return;
            foreach (var prop in chunk.PropStates.Values)
            {
                NetworkProp networkProp;

                if (_propPool.Count > 0)
                {
                    networkProp = _propPool.Pop();
                    networkProp.transform.position = prop.position;
                    networkProp.transform.rotation = prop.rotation;

                }
                else
                { 
                    networkProp = Runner.Spawn(_propPrefab, prop.position, prop.rotation, null);
                }

                networkProp.OnSpawned(prop, this);
                chunk.NetworkProps.Add(networkProp);
                _activeProps.Add(networkProp);
            }
        }

        public void DespawnNetworkPropsForChunk(Chunk chunk)
        {
            //return;
            foreach (var networkProp in chunk.NetworkProps)
            {
                networkProp.Deactivate();
                _propPool.Push(networkProp);
                _activeProps.Remove(networkProp);
            }
        }
    }

    public struct FPropLoadState
    {
        public Prop Prop;
        public ELoadState LoadState;
    }
}
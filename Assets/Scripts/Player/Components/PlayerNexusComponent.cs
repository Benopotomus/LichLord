using LichLord.Props;
using LichLord.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{
    [Serializable]
    public struct FNexusData
    {
        public FChunkPosition ChunkID;
        public int GUID;
    }

    public class PlayerNexusComponent : ContextBehaviour
    {
        // saved data for activated nexuses. lets us look it up
        public List<FNexusData> _activeNexus = new List<FNexusData>();

        [SerializeField] private PlayerCharacter _pc;

        public PropRuntimeState NearestNexus;

        public override void Render()
        {
            base.Render();

            foreach (var nexusData in _activeNexus)
            {
                Chunk chunk = Context.ChunkManager.GetChunk(nexusData.ChunkID);
                if (chunk.GetRenderState(HasStateAuthority, nexusData.GUID, out var state))
                {
                    Vector3 sqrDist = (state.position - _pc.CachedTransform.position);
                    Debug.Log(state.GetHealth());

                    NearestNexus = state;
                }

            }

            // Get nearest nexus

        }

        public void AddNexus(Nexus nexus)
        {
            foreach (var data in _activeNexus)
            {
                if (data.ChunkID.Equals(nexus.ChunkID) && data.GUID == nexus.GUID)
                    return; // Already added
            }

            FNexusData nexusData = new FNexusData
            {
                ChunkID = nexus.ChunkID,
                GUID = nexus.GUID
            };

            _activeNexus.Add(nexusData);
        }
    }
}

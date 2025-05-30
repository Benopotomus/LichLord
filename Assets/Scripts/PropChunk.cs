using Fusion;
using UnityEngine;
using System.Collections.Generic;

// Creates and manages a list of chunk actor array.
// This will apply the damage and connect the chunk actor to this for networking and updates.

namespace LichLord.Props
{
    public class PropChunk : ContextBehaviour, INetActor
    {
        //[SerializeField] private PropPrefabLoader _prefabLoader;

        [Networked]
        [Capacity(128)]
        private NetworkArray<FPropData> _propDatas { get; }

        public FNetObjectID NetObjectID
        {
            get => Object != null ? new FNetObjectID { guid = Object.Id } : default;
        }

        // the indexes that cause their props to launch on spawn.
        public Dictionary<int, Vector2> _launchOnSpawnIndices = new Dictionary<int, Vector2>();

        public override void Spawned()
        {

        }

        public override void Render()
        {

        }
    }
}

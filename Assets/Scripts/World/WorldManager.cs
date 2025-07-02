using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.World
{
    public class WorldManager : ContextBehaviour
    {
        [SerializeField]
        private WorldSettings _worldSettings;
        public WorldSettings WorldSettings => _worldSettings;

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority)
            {
                Context.WorldSaveLoadManager.LoadChunks();
            }

            Context.ChunkManager.InitializeWorldChunks();

            if (HasStateAuthority)
            {
                Context.ChunkManager.LoadChunksFromSaves();
            }
            
            if (HasStateAuthority)
            {
                Context.WorldSaveLoadManager.LoadNPCs();
                Context.NonPlayerCharacterManager.LoadNPCsFromSaves();
            }

            Context.SpawnManager.SpawnLocalPlayer(Runner.LocalPlayer);
        }
    }
}

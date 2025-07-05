

namespace LichLord.Buildables
{
    using Fusion;
    using UnityEngine;

    public partial class BuildableManager : ContextBehaviour
    {
        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_PlaceBuildableFloor(BuildableZone zone, 
            byte posX, 
            byte posY,
            byte posZ, 
            byte definitionID)
        {
            Debug.Log(zone);

            zone.PlaceBuildableFloor(posX,
            posY,
            posZ,
            definitionID);
          
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_PlaceBuildableWall(BuildableZone zone,
            EWallOrientation wallOrientation,
            byte posX,
            byte posY,
            byte posZ,
            byte definitionID)
        {
            Debug.Log(zone);

            zone.PlaceBuildableWall(definitionID,
                wallOrientation,
                posX,
                posY,
                posZ);


        }
    }
}

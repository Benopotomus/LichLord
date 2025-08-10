

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
            if (zone == null)
                return;

            //Debug.Log("Place Floor Position: " + new Vector3Int(posX, posY, posZ) + ", Def ID: " + definitionID);
            /*
            zone.PlaceBuildableFloor(definitionID,
            posX,
            posY,
            posZ);
            */
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_PlaceBuildableWall(BuildableZone zone,
            EWallOrientation wallOrientation,
            byte posX,
            byte posY,
            byte posZ,
            byte definitionID)
        {
            if (zone == null)
                return;

            //Debug.Log("Place Wall Position: " + new Vector3Int(posX, posY, posZ) + " Orientation: " + wallOrientation);
/*
zone.PlaceBuildableWall(definitionID,
    wallOrientation,
    posX,
    posY,
    posZ);
*/
}

[Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
public void RPC_PlaceBuildableFeature(BuildableZone zone,
EWallOrientation wallOrientation,
byte posX,
byte posY,
byte posZ,
byte definitionID)
{
if (zone == null)
    return;

//Debug.Log("Place Wall Position: " + new Vector3Int(posX, posY, posZ) + " Orientation: " + wallOrientation);
/*
zone.PlaceBuildableFeature(definitionID,
    wallOrientation,
    posX,
    posY,
    posZ);
            */
}
}
}

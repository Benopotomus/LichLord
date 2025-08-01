using UnityEngine;
using LichLord.World;

namespace LichLord.Props
{
    public class ChunkMarkupData : ScriptableObject
    {
        public FChunkPosition ChunkCoord;
        public PropMarkupData[] PropMarkupDatas = new PropMarkupData[0];
        public InvasionSpawnPointMarkupData[] InvasionSpawnPointMarkupDatas = new InvasionSpawnPointMarkupData[0];
        public StrongholdMarkupData[] StrongholdMarkupDatas = new StrongholdMarkupData[0];
    }
}
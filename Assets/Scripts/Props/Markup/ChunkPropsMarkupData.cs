using UnityEngine;
using LichLord.World;

namespace LichLord.Props
{
    public class ChunkPropsMarkupData : ScriptableObject
    {
        public FChunkPosition ChunkCoord;
        public PropMarkupData[] propMarkupDatas = new PropMarkupData[0];
    }
}
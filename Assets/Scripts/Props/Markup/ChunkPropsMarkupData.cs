using UnityEngine;
using System;

namespace LichLord.Props
{
    public class ChunkPropsMarkupData : ScriptableObject
    {
        public Vector2Int ChunkCoord;
        public PropMarkupData[] propMarkupDatas = new PropMarkupData[0];
    }
}
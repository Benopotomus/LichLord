using UnityEngine;
using Fusion;
using System.Collections.Generic;
using LichLord.Props;

[CreateAssetMenu(fileName = "WorldSettings", menuName = "LichLord/World/WorldSettings", order = 1)]
public class WorldSettings : ScriptableObject
{
    [SerializeField]
    private Vector2 _worldOrigin = new Vector2(-500f, -500f);
    public Vector2 WorldOrigin => _worldOrigin;
    
    [SerializeField]
    private Vector2 _worldSize = new Vector2(1000f, 1000f);
    public Vector2 WorldSize => _worldSize;

    [SerializeField]
    private List<ChunkPropsMarkupData> _propMarkupDatas = new List<ChunkPropsMarkupData>();
    public List<ChunkPropsMarkupData> PropMarkupDatas => _propMarkupDatas;

    // Get or create a LevelPropsMarkupData for a specific chunk coordinate
    public ChunkPropsMarkupData GetOrCreateMarkupData(Vector2Int chunkCoord)
    {
        ChunkPropsMarkupData markupData = _propMarkupDatas.Find(data => data.ChunkCoord == chunkCoord);
        if (markupData == null)
        {
            markupData = ScriptableObject.CreateInstance<ChunkPropsMarkupData>();
            markupData.name = $"MarkupData_{chunkCoord.x}_{chunkCoord.y}";
            markupData.ChunkCoord = chunkCoord;
            _propMarkupDatas.Add(markupData);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(markupData, this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
        return markupData;
    }

    // Calculate chunk coordinate from world position
    public Vector2Int GetChunkCoordFromPosition(Vector3 worldPosition)
    {
        Vector2 relativePos = new Vector2(worldPosition.x, worldPosition.z) - _worldOrigin;
        return new Vector2Int(
            Mathf.FloorToInt(relativePos.x / LichLord.World.WorldConstants.CHUNK_SIZE),
            Mathf.FloorToInt(relativePos.y / LichLord.World.WorldConstants.CHUNK_SIZE)
        );
    }
}

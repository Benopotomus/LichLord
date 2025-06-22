using UnityEngine;
using Fusion;
using System.Collections.Generic;
using LichLord.Props;
using LichLord.World;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

    public ChunkPropsMarkupData GetMarkupData(FChunkPosition chunkCoord)
    {
        // First, check the PropMarkupDatas list
        ChunkPropsMarkupData markupData = _propMarkupDatas.Find(data => data != null && data.ChunkCoord == chunkCoord);
        if (markupData != null)
        {
            return markupData;
        }

        return null;
    }

    // Get or create a LevelPropsMarkupData for a specific chunk coordinate
    public ChunkPropsMarkupData GetOrCreateMarkupData(FChunkPosition chunkCoord)
    {
        // First, check the PropMarkupDatas list
        ChunkPropsMarkupData markupData = _propMarkupDatas.Find(data => data != null && data.ChunkCoord == chunkCoord);
        if (markupData != null)
        {
            return markupData;
        }

        // Check for existing sub-assets not in PropMarkupDatas
#if UNITY_EDITOR
        string assetPath = AssetDatabase.GetAssetPath(this);
        if (!string.IsNullOrEmpty(assetPath))
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object subAsset in subAssets)
            {
                if (subAsset is ChunkPropsMarkupData existingMarkupData && existingMarkupData.ChunkCoord == chunkCoord)
                {
                    if (!_propMarkupDatas.Contains(existingMarkupData))
                    {
                        _propMarkupDatas.Add(existingMarkupData);
                        EditorUtility.SetDirty(this);
                        AssetDatabase.SaveAssets();
                        Debug.Log($"Re-added existing sub-asset to PropMarkupDatas: {existingMarkupData.name}");
                    }
                    return existingMarkupData;
                }
            }
        }
#endif

        // Create new markup data if none found
        markupData = ScriptableObject.CreateInstance<ChunkPropsMarkupData>();
        markupData.name = $"MarkupData_{chunkCoord.X}_{chunkCoord.Y}";
        markupData.ChunkCoord = chunkCoord;
        _propMarkupDatas.Add(markupData);
#if UNITY_EDITOR
        AssetDatabase.AddObjectToAsset(markupData, this);
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        Debug.Log($"Created new sub-asset: {markupData.name} for chunk {chunkCoord.X},{chunkCoord.Y}");
#endif
        return markupData;
    }

    // Calculate chunk coordinate from world position
    public FChunkPosition GetChunkCoordFromPosition(Vector3 worldPosition)
    {
        Vector2 relativePos = new Vector2(worldPosition.x, worldPosition.z) - _worldOrigin;
        return new FChunkPosition
        {
            X = (sbyte)(Mathf.FloorToInt(relativePos.x / LichLord.World.WorldConstants.CHUNK_SIZE)),
            Y = (sbyte)(Mathf.FloorToInt(relativePos.y / LichLord.World.WorldConstants.CHUNK_SIZE))
        };
    }

#if UNITY_EDITOR
    // Remove all ChunkPropsMarkupData sub-assets and clear the PropMarkupDatas list
    public void RemoveAllMarkupData()
    {
        if (_propMarkupDatas == null)
        {
            Debug.LogWarning("PropMarkupDatas is null, initializing and clearing.");
            _propMarkupDatas = new List<ChunkPropsMarkupData>();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return;
        }

        Undo.RecordObject(this, "Remove All Markup Data");

        // Get all sub-assets of this WorldSettings asset
        string assetPath = AssetDatabase.GetAssetPath(this);
        int totalSubAssets = 0;
        int deletedSubAssets = 0;
        int nullCount = 0;

        if (!string.IsNullOrEmpty(assetPath))
        {
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (Object subAsset in subAssets)
            {
                if (subAsset is ChunkPropsMarkupData markupData && subAsset != this)
                {
                    totalSubAssets++;
                    Debug.Log($"Removing sub-asset: {markupData.name} (ChunkCoord: {markupData.ChunkCoord.X},{markupData.ChunkCoord.Y})");
                    Undo.DestroyObjectImmediate(markupData);
                    deletedSubAssets++;
                }
            }
        }
        else
        {
            Debug.LogWarning("No asset path for WorldSettings, checking PropMarkupDatas only.");
        }

        // Clear PropMarkupDatas and handle null entries
        for (int i = _propMarkupDatas.Count - 1; i >= 0; i--)
        {
            if (_propMarkupDatas[i] == null)
            {
                nullCount++;
                _propMarkupDatas.RemoveAt(i);
            }
        }

        // Clear remaining entries in PropMarkupDatas
        int listCount = _propMarkupDatas.Count;
        _propMarkupDatas.Clear();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Remove All Markup Data: Found {totalSubAssets} ChunkPropsMarkupData sub-assets, removed {deletedSubAssets}. Cleared {listCount} items from PropMarkupDatas, {nullCount} null entries. PropMarkupDatas list is now empty.");
    }
#endif
}
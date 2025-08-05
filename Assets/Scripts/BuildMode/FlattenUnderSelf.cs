using UnityEngine;
using Pathfinding; // Required for AstarPath

[RequireComponent(typeof(Collider))]
public class FlattenUnderSelf : MonoBehaviour
{
    [Header("Terrain Flattening")]
    [SerializeField] private float flattenRadius = 5f;
    [SerializeField] private float flattenHeightOffset = 0f;
    [SerializeField, Range(0f, 1f)] private float feather = 0.5f;

    [Header("Terrain Texturing")]
    [SerializeField] private int textureLayerIndex = 0; // Grass, for example

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask terrainRaycastMask = ~0; // Everything by default

    private void Start()
    {
        TryFlattenUnderSelf();
    }

    private void TryFlattenUnderSelf()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 10f;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 100f, terrainRaycastMask))
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain)
            {
                FlattenTerrain(terrain, hit.point);
                PaintTextureOnFlattenedArea(terrain, hit.point, flattenRadius, textureLayerIndex, feather);

                // Update A* Recast Graph
                Bounds updateBounds = new Bounds(hit.point, new Vector3(flattenRadius * 2f, 20f, flattenRadius * 2f));
                AstarPath.active?.UpdateGraphs(updateBounds);
            }
        }
    }

    private void FlattenTerrain(Terrain terrain, Vector3 worldPosition)
    {
        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        int res = data.heightmapResolution;
        float normX = (worldPosition.x - terrainPos.x) / data.size.x;
        float normZ = (worldPosition.z - terrainPos.z) / data.size.z;

        int centerX = Mathf.RoundToInt(normX * res);
        int centerZ = Mathf.RoundToInt(normZ * res);
        int radiusSamples = Mathf.RoundToInt((flattenRadius / data.size.x) * res);

        int startX = Mathf.Clamp(centerX - radiusSamples, 0, res - 1);
        int startZ = Mathf.Clamp(centerZ - radiusSamples, 0, res - 1);
        int width = Mathf.Clamp(centerX + radiusSamples, 0, res) - startX;
        int height = Mathf.Clamp(centerZ + radiusSamples, 0, res) - startZ;

        float[,] heights = data.GetHeights(startX, startZ, width, height);
        float targetHeight = (worldPosition.y + flattenHeightOffset - terrainPos.y) / data.size.y;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x + startX - centerX) / (float)radiusSamples;
                float dz = (z + startZ - centerZ) / (float)radiusSamples;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist <= 1f)
                {
                    float blend = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(1f - feather, 1f, dist));
                    heights[z, x] = Mathf.Lerp(heights[z, x], targetHeight, blend);
                }
            }
        }

        data.SetHeights(startX, startZ, heights);
    }

    private void PaintTextureOnFlattenedArea(Terrain terrain, Vector3 worldPosition, float radius, int textureLayer, float feather)
    {
        TerrainData data = terrain.terrainData;
        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;
        Vector3 terrainPos = terrain.transform.position;

        float normX = (worldPosition.x - terrainPos.x) / data.size.x;
        float normZ = (worldPosition.z - terrainPos.z) / data.size.z;

        int centerX = Mathf.RoundToInt(normX * alphaWidth);
        int centerZ = Mathf.RoundToInt(normZ * alphaHeight);
        int radiusSamples = Mathf.RoundToInt((radius / data.size.x) * alphaWidth);

        int startX = Mathf.Clamp(centerX - radiusSamples, 0, alphaWidth - 1);
        int startZ = Mathf.Clamp(centerZ - radiusSamples, 0, alphaHeight - 1);
        int width = Mathf.Clamp(centerX + radiusSamples, 0, alphaWidth) - startX;
        int height = Mathf.Clamp(centerZ + radiusSamples, 0, alphaHeight) - startZ;

        float[,,] alphas = data.GetAlphamaps(startX, startZ, width, height);

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x + startX - centerX) / (float)radiusSamples;
                float dz = (z + startZ - centerZ) / (float)radiusSamples;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist <= 1f)
                {
                    float blend = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(1f - feather, 1f, dist));

                    float total = 0f;
                    for (int i = 0; i < data.alphamapLayers; i++)
                    {
                        alphas[z, x, i] = Mathf.Lerp(alphas[z, x, i], i == textureLayer ? 1f : 0f, blend);
                        total += alphas[z, x, i];
                    }

                    for (int i = 0; i < data.alphamapLayers; i++)
                    {
                        alphas[z, x, i] /= total;
                    }
                }
            }
        }

        data.SetAlphamaps(startX, startZ, alphas);
    }
}

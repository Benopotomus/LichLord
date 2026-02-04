using UnityEngine;
using Pathfinding; // For AstarPath graph updates

public class TerrainFlattener : MonoBehaviour
{
    [Header("Terrain Texturing")]
    [SerializeField] private int textureLayerIndex = 0; // Example: Grass layer

    [Header("Raycast Settings")]
    [SerializeField] private LayerMask terrainRaycastMask = ~0; // Default: everything

    /// <summary>
    /// Flattens and paints the terrain under this object with given flat and feather radii.
    /// </summary>
    public void TryFlatten(float flatRadius, float featherRadius)
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 10f;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 50f, terrainRaycastMask);

        foreach (RaycastHit hit in hits)
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                FlattenTerrain(terrain, hit.point, flatRadius, featherRadius);
                PaintTextureOnFlattenedArea(terrain, hit.point, flatRadius, featherRadius);

                // Update A* Recast Graph
                float totalRadius = flatRadius + featherRadius;
                Bounds updateBounds = new Bounds(hit.point, new Vector3(totalRadius * 2f, 50f, totalRadius * 2f));
                AstarPath.active?.UpdateGraphs(updateBounds);
                return; // Stop after first terrain hit
            }
        }
    }

    private void FlattenTerrain(Terrain terrain, Vector3 worldPosition, float flatRadius, float featherRadius)
    {
        TerrainData data = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;

        if (data == null)
            return;

        int res = data.heightmapResolution;
        float normX = (worldPosition.x - terrainPos.x) / data.size.x;
        float normZ = (worldPosition.z - terrainPos.z) / data.size.z;

        int centerX = Mathf.RoundToInt(normX * res);
        int centerZ = Mathf.RoundToInt(normZ * res);

        float totalRadius = flatRadius + featherRadius;
        int radiusSamples = Mathf.RoundToInt((totalRadius / data.size.x) * res);

        int startX = Mathf.Clamp(centerX - radiusSamples, 0, res - 1);
        int startZ = Mathf.Clamp(centerZ - radiusSamples, 0, res - 1);
        int width = Mathf.Clamp(centerX + radiusSamples, 0, res) - startX;
        int height = Mathf.Clamp(centerZ + radiusSamples, 0, res) - startZ;

        float[,] heights = data.GetHeights(startX, startZ, width, height);
        float targetHeight = (worldPosition.y - terrainPos.y) / data.size.y;

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float sampleX = terrainPos.x + ((startX + x) / (float)res) * data.size.x;
                float sampleZ = terrainPos.z + ((startZ + z) / (float)res) * data.size.z;

                float dist = Vector2.Distance(new Vector2(sampleX, sampleZ), new Vector2(worldPosition.x, worldPosition.z));
                float blend = CalculatePlateauBlend(dist, flatRadius, featherRadius);
                if (blend > 0f)
                {
                    if (blend >= 1f)
                    {
                        heights[z, x] = targetHeight;
                    }
                    else
                    {
                        heights[z, x] = Mathf.Lerp(heights[z, x], targetHeight, blend);
                    }
                }
            }
        }

        data.SetHeights(startX, startZ, heights);
    }

    private void PaintTextureOnFlattenedArea(Terrain terrain, Vector3 worldPosition, float flatRadius, float featherRadius)
    {
        return;
        /*
        flatRadius *= 0.5f;

        TerrainData data = terrain.terrainData;
        int alphaWidth = data.alphamapWidth;
        int alphaHeight = data.alphamapHeight;
        Vector3 terrainPos = terrain.transform.position;

        int centerX = Mathf.RoundToInt(((worldPosition.x - terrainPos.x) / data.size.x) * alphaWidth);
        int centerZ = Mathf.RoundToInt(((worldPosition.z - terrainPos.z) / data.size.z) * alphaHeight);

        int radiusSamples = Mathf.RoundToInt((flatRadius / data.size.x) * alphaWidth);

        int startX = Mathf.Clamp(centerX - radiusSamples, 0, alphaWidth - 1);
        int startZ = Mathf.Clamp(centerZ - radiusSamples, 0, alphaHeight - 1);
        int width = Mathf.Clamp(centerX + radiusSamples, 0, alphaWidth) - startX;
        int height = Mathf.Clamp(centerZ + radiusSamples, 0, alphaHeight) - startZ;

        float[,,] alphas = data.GetAlphamaps(startX, startZ, width, height);

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                float sampleX = terrainPos.x + ((startX + x) / (float)alphaWidth) * data.size.x;
                float sampleZ = terrainPos.z + ((startZ + z) / (float)alphaHeight) * data.size.z;

                float dist = Vector2.Distance(new Vector2(sampleX, sampleZ), new Vector2(worldPosition.x, worldPosition.z));

                if (dist <= flatRadius)
                {
                    // Paint fully inside flat radius (blend = 1)
                    alphas[z, x, textureLayerIndex] = 1f;

                    // Zero out all other layers
                    for (int i = 0; i < data.alphamapLayers; i++)
                    {
                        if (i != textureLayerIndex)
                            alphas[z, x, i] = 0f;
                    }
                }
            }
        }

        data.SetAlphamaps(startX, startZ, alphas);
        */
    }

    private float CalculatePlateauBlend(float dist, float flatRadius, float featherRadius)
    {
        if (dist > flatRadius + featherRadius)
            return 0f;

        if (dist <= flatRadius)
            return 1f;

        float t = (dist - flatRadius) / featherRadius;
        return Mathf.SmoothStep(1f, 0f, t);
    }
}

using UnityEngine;
using LichLord.Props;
using AYellowpaper.SerializedCollections;

#if UNITY_EDITOR
namespace LichLord.World
{
    public class LevelEditor : MonoBehaviour
    {
        [SerializeField] private WorldSettings worldSettings;
        public WorldSettings WorldSettings => worldSettings;

        [SerializeField] private GlobalTables globalTables;
        public GlobalTables GlobalTables => globalTables;

        [SerializedDictionary("Prop Definition", "Marker")]
        public SerializedDictionary<PropDefinition, PropMarker> PropMarkerPrefabs;

        public InvasionSpawnPointMarker InvasionSpawnMarkerPrefab;

        [SerializeField]
        public LevelEditorMarker MarkerPrefab;

        [SerializeField]
        private bool randomizeYaw = false; // Persist randomize yaw setting
        [SerializeField]
        private Vector2 randomScaleRange = Vector2.one; // Persist random scale range (min, max)

        // Properties to access serialized fields
        public bool RandomizeYaw
        {
            get => randomizeYaw;
            set => randomizeYaw = value;
        }

        public Vector2 RandomScaleRange
        {
            get => randomScaleRange;
            set => randomScaleRange = value;
        }

        public void OnDrawGizmos()
        {
            // Get world size (you may need to adjust this based on how WorldSettings is accessed)
            Vector2 worldSize = new Vector2(70500f, 70500f); // Default assumption: 235 * 300 (CHUNK_SIZE)
                                                             // If WorldSettings is accessible in editor, uncomment and adjust the following:
                                                             // WorldSettings worldSettings = chunkManager.Context?.WorldManager?.WorldSettings;
                                                             // if (worldSettings != null)
                                                             //     worldSize = worldSettings.WorldSize;

            // Calculate chunk grid size
            int chunkGridSizeX = Mathf.CeilToInt(worldSize.x / WorldConstants.CHUNK_SIZE);
            int chunkGridSizeY = Mathf.CeilToInt(worldSize.y / WorldConstants.CHUNK_SIZE);

            // Set Gizmos color (using green as in original OnDrawGizmos)
            Gizmos.color = Color.green;

            // Draw wireframe cubes for each chunk
            for (int x = 0; x < chunkGridSizeX; x++)
            {
                for (int y = 0; y < chunkGridSizeY; y++)
                {
                    // Calculate chunk bounds
                    Vector3 chunkCenter = new Vector3(
                        (x * WorldConstants.CHUNK_SIZE) + (WorldConstants.CHUNK_SIZE / 2f),
                        0f,
                        (y * WorldConstants.CHUNK_SIZE) + (WorldConstants.CHUNK_SIZE / 2f)
                    );

                    Vector3 chunkSize = new Vector3(
                        WorldConstants.CHUNK_SIZE - 0.1f,
                        0.1f,
                        WorldConstants.CHUNK_SIZE - 0.1f
                    );

                    // Draw wire cube
                    Gizmos.DrawWireCube(chunkCenter, chunkSize);
                }
            }

        }
    }
}
#endif

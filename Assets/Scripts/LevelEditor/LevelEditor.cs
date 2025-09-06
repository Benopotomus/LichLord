using UnityEngine;
using LichLord.Props;
using AYellowpaper.SerializedCollections;

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

    }
}

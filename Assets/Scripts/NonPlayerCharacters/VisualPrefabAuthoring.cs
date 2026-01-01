using Unity.Entities;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using LichLord.NonPlayerCharacters; // Assuming your NPCDefinition lives here

namespace LichLord
{
    public class VisualPrefabsAuthoring : MonoBehaviour
    {
        [Header("Map NPC Definitions to their Visual Prefabs")]
        [SerializedDictionary("NPC Definition", "Visual Prefab")]
        public SerializedDictionary<NonPlayerCharacterDefinition, GameObject> visualPrefabs;
    }

    // Buffer element now includes the key for lookup
    public struct VisualEntityPrefabElement : IBufferElementData
    {
        public int DefinitionTableId; // The key
        public Entity Prefab;                           // The baked visual entity
    }

    public class VisualPrefabsBaker : Baker<VisualPrefabsAuthoring>
    {
        public override void Bake(VisualPrefabsAuthoring authoring)
        {
            if (authoring.visualPrefabs == null || authoring.visualPrefabs.Count == 0)
                return;

            var entity = GetEntity(TransformUsageFlags.None);

            var buffer = AddBuffer<VisualEntityPrefabElement>(entity);

            foreach (var kvp in authoring.visualPrefabs)
            {
                var definition = kvp.Key;
                var prefabGO = kvp.Value;

                if (definition == null || prefabGO == null)
                    continue;

                var prefabEntity = GetEntity(prefabGO, TransformUsageFlags.Dynamic);

                buffer.Add(new VisualEntityPrefabElement
                {
                    DefinitionTableId = definition.TableID,
                    Prefab = prefabEntity
                });

                DependsOn(prefabGO);
            }
        }
    }
}
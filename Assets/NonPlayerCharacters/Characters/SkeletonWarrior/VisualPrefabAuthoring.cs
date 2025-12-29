using Unity.Entities;
using UnityEngine;

public class VisualPrefabAuthoring : MonoBehaviour
{
    public GameObject visualPrefab; // Drag your visual GameObject prefab here (the one with SkinnedMeshRenderer, etc.)
}

public class VisualPrefabBaker : Baker<VisualPrefabAuthoring>
{
    public override void Bake(VisualPrefabAuthoring authoring)
    {
        if (authoring.visualPrefab == null) return;

        var entity = GetEntity(TransformUsageFlags.None);

        var prefabEntity = GetEntity(authoring.visualPrefab, TransformUsageFlags.Dynamic);

        AddComponent(entity, new VisualEntityPrefab
        {
            Prefab = prefabEntity
        });

        DependsOn(authoring.visualPrefab);  // This fixes unreliable serialization/duplication issues
    }
}
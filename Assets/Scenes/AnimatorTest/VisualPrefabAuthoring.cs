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
        AddComponent(entity, new VisualEntityPrefab
        {
            Prefab = GetEntity(authoring.visualPrefab, TransformUsageFlags.Dynamic) // Bakes the reference
        });
    }
}
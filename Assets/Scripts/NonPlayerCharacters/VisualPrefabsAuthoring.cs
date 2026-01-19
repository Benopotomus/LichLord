using Unity.Entities;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using LichLord.NonPlayerCharacters;
using Rukhanka.Hybrid;

namespace LichLord
{
    public struct VisualEntityPrefabElement : IBufferElementData
    {
        public int DefinitionTableId;
        public Entity Prefab;
    }

    public struct GpuAttachmentElement : IBufferElementData
    {
        public int DefinitionTableId;
        public Entity AttachmentEntity;
        public int Side;
        public int BoneIndex;
    }

    public class VisualPrefabsAuthoring : MonoBehaviour
    {
        [Header("Map NPC Definitions to their Visual Prefabs")]
        [SerializedDictionary("NPC Definition", "Visual Prefab")]
        public SerializedDictionary<NonPlayerCharacterDefinition, GameObject> visualPrefabs;
    }

    public class VisualPrefabsBaker : Baker<VisualPrefabsAuthoring>
    {
        public override void Bake(VisualPrefabsAuthoring authoring)
        {
            if (authoring.visualPrefabs == null || authoring.visualPrefabs.Count == 0)
            {
                Debug.LogWarning("[VisualPrefabsBaker] No visual prefabs defined in authoring component!");
                return;
            }

            Debug.Log($"[VisualPrefabsBaker] Starting bake for {authoring.visualPrefabs.Count} visual prefab entries...");

            var singletonEntity = GetEntity(TransformUsageFlags.None);
            Debug.Log($"[VisualPrefabsBaker] Created singleton entity: {singletonEntity}");

            var prefabBuffer = AddBuffer<VisualEntityPrefabElement>(singletonEntity);
            var attachmentBuffer = AddBuffer<GpuAttachmentElement>(singletonEntity);

            int totalPrefabsBaked = 0;
            int totalAttachmentsBaked = 0;

            foreach (var kvp in authoring.visualPrefabs)
            {
                var definition = kvp.Key;
                var prefabGO = kvp.Value;

                if (definition == null)
                {
                    Debug.LogWarning($"[VisualPrefabsBaker] Skipping entry: Definition is null");
                    continue;
                }

                if (prefabGO == null)
                {
                    Debug.LogWarning($"[VisualPrefabsBaker] Skipping definition {definition.name} (TableID: {definition.TableID}): Prefab GameObject is null");
                    continue;
                }

                Debug.Log($"[VisualPrefabsBaker] Baking prefab for {definition.name} (TableID: {definition.TableID}) → GO: {prefabGO.name}");

                var prefabEntity = GetEntity(prefabGO, TransformUsageFlags.Dynamic);
                prefabBuffer.Add(new VisualEntityPrefabElement
                {
                    DefinitionTableId = definition.TableID,
                    Prefab = prefabEntity
                });

                DependsOn(prefabGO);

                totalPrefabsBaked++;

                // Find GPU attachments
                var gpuAttachments = prefabGO.GetComponentsInChildren<GPUAttachmentAuthoring>(includeInactive: true);
                int attachmentsThisPrefab = gpuAttachments.Length;

                Debug.Log($"[VisualPrefabsBaker] Found {attachmentsThisPrefab} GPU attachments under {prefabGO.name}");

                foreach (var gpuAuth in gpuAttachments)
                {
                    if (gpuAuth == null)
                    {
                        Debug.LogWarning($"[VisualPrefabsBaker] Found null GPUAttachmentAuthoring component under {prefabGO.name}");
                        continue;
                    }

                    var weaponIndex = gpuAuth.GetComponent<WeaponAuthoringIndex>();

                    var attachmentEntity = GetEntity(gpuAuth.gameObject, TransformUsageFlags.Dynamic);
                    int boneIndex = gpuAuth.attachedBoneIndex;

                    attachmentBuffer.Add(new GpuAttachmentElement
                    {
                        DefinitionTableId = definition.TableID,
                        AttachmentEntity = attachmentEntity,
                        Side = weaponIndex.Side,
                        BoneIndex = weaponIndex.WeaponIndex
                    });

                    DependsOn(gpuAuth.gameObject);

                    Debug.Log($"[VisualPrefabsBaker]   Added attachment: Entity={attachmentEntity}, BoneIndex={boneIndex}, ParentGO={gpuAuth.gameObject.name}");
                    totalAttachmentsBaked++;
                }
            }

            Debug.Log($"[VisualPrefabsBaker] Bake completed!");
            Debug.Log($"   → Total visual prefabs baked: {totalPrefabsBaked}");
            Debug.Log($"   → Total GPU attachments collected: {totalAttachmentsBaked}");
            Debug.Log($"   → Prefab buffer size: {prefabBuffer.Length}");
            Debug.Log($"   → Attachment buffer size: {attachmentBuffer.Length}");
        }
    }
}
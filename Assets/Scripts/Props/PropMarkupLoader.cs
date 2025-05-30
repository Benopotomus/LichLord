using UnityEngine;

namespace LichLord.Props
{
    public class PropMarkupLoader : MonoBehaviour
    {
        [SerializeField] private PropPointMarkupData propMarkupData;
        [SerializeField] private GameObject[] propPrefabs;

        private void Start()
        {
            LoadAndSpawnProps();
        }

        private void LoadAndSpawnProps()
        {
            // Check if propData is assigned
            if (propMarkupData == null)
            {
                Debug.LogError("PropPointDataAsset is not assigned in PropPointLoader.", this);
                return;
            }

            // Check if propPoints array is valid
            if (propMarkupData.propPoints == null || propMarkupData.propPoints.Length == 0)
            {
                Debug.LogWarning("PropPointDataAsset has no prop points to spawn.", this);
                return;
            }

            // Check if propPrefabs array is valid
            if (propPrefabs == null || propPrefabs.Length == 0)
            {
                Debug.LogWarning("No prefabs assigned to propPrefabs array in PropPointLoader.", this);
                return;
            }

            foreach (PropPointData propPoint in propMarkupData.propPoints)
            {
                if (propPoint == null || propPoint.propDefinition == null)
                {
                    Debug.LogWarning("Skipping invalid prop point with null PropDefinition.", this);
                    continue;
                }

                GameObject prefab = GetPrefabByDefinition(propPoint.propDefinition);
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, propPoint.position, Quaternion.identity);
                    Debug.Log($"Spawned {prefab.name} at {propPoint.position} for PropDefinition: {propPoint.propDefinition.propName}", instance);
                }
                else
                {
                    Debug.LogWarning($"No prefab found for PropDefinition: {propPoint.propDefinition.propName}", this);
                }
            }
        }

        private GameObject GetPrefabByDefinition(PropDefinition definition)
        {
            // Try matching by propName
            foreach (GameObject prefab in propPrefabs)
            {
                if (prefab != null && prefab.name == definition.propName)
                {
                    return prefab;
                }
            }

            // Fallback to PropDefinition's prefab field
            if (definition.prefab != null)
            {
                return definition.prefab;
            }

            return null;
        }
    }
}
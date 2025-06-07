using DWD.Pooling;
using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterSpawner : MonoBehaviour
    {
        public Action<FNonPlayerCharacterSpawnParams, NonPlayerCharacter> OnSpawned;

        public void SpawnNPC(ref FNonPlayerCharacterData data)
        {
            var spawnParams = new FNonPlayerCharacterSpawnParams
            {
                index = NonPlayerCharacterDataUtility.GetIndex(ref data),
                definitionId = NonPlayerCharacterDataUtility.GetDefinitionID(ref data),
                position = data.Position,
                rotation = data.Rotation,
                teamID = NonPlayerCharacterDataUtility.GetTeamID(ref data)
            };

            NonPlayerCharacterDefinition definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(spawnParams.definitionId);

            if (definition == null)
            {
                Debug.LogWarning("Trying to spawn NPC with invalid definition, id: " + spawnParams.definitionId);
                return;
            }

            BundleObject prefabBundle = definition.PrefabBundle;

            if (!prefabBundle.Ready)
            {
                Debug.LogWarning("Cannot load null Bundle Object! ");
                return;
            }

            List<ILoader> LoadedBundles = AssetBundleManager.Instance.CompleteLoaders;

            for (int i = 0; i < LoadedBundles.Count; i++)
            {
                AssetBundleLoader loadedBundle = LoadedBundles[i] as AssetBundleLoader;

                if (loadedBundle.BundleName == prefabBundle.Bundle)
                {
                    OnPrefabLoaded(spawnParams, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            NonPlayerCharacterLoader propLoader = new NonPlayerCharacterLoader(spawnParams, prefabLoader);

            if (propLoader.Loader != null)
            {
                if (propLoader.Loader.IsLoaded)
                    OnPrefabLoaded(propLoader);
                else
                    propLoader.OnLoadComplete += OnPrefabLoaded;
            }
        }

        private void OnPrefabLoaded(NonPlayerCharacterLoader loader)
        {
            loader.OnLoadComplete -= OnPrefabLoaded;
            OnPrefabLoaded(loader.SpawnParams, loader.Loader);
        }

        private void OnPrefabLoaded(FNonPlayerCharacterSpawnParams spawnParams, AssetBundleLoader loadedBundle)
        {
            GameObject prefab = loadedBundle.GetAssetWithin<GameObject>();

            if (prefab == null)
                return;

            var poolObject = prefab.GetComponent<DWDObjectPoolObject>();
            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn NPC " + spawnParams.definitionId + ". Could not find DWDObjectPoolObject Component!");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAt(poolObject, spawnParams.position, spawnParams.rotation);

            NonPlayerCharacter spawnedProp = instance.GetComponent<NonPlayerCharacter>();

            if (spawnedProp == null)
            {
                Debug.LogWarning("NPC is Invalid, Check Bundles! (" + loadedBundle.BundleName + ")");
                return;
            }

            OnSpawned?.Invoke(spawnParams, spawnedProp);
        }
    }
}
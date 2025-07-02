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

        public void SpawnNPC(ref FNonPlayerCharacterData data, int index)
        {
            var spawnParams = new FNonPlayerCharacterSpawnParams
            {
                Index = index,
                DefinitionId = NonPlayerCharacterDataUtility.GetDefinitionID(ref data),
                Position = data.Position,
                Rotation = data.Rotation,
                TeamId = NonPlayerCharacterDataUtility.GetTeamID(ref data)
            };

            NonPlayerCharacterDefinition definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(spawnParams.DefinitionId);

            if (definition == null)
            {
                Debug.LogWarning("Trying to spawn NPC with invalid definition, id: " + spawnParams.DefinitionId);
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
                Debug.LogWarning("Could not spawn NPC " + spawnParams.DefinitionId + ". Could not find DWDObjectPoolObject Component!");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAt(poolObject, spawnParams.Position, spawnParams.Rotation);

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
using DWD.Pooling;
using DWD.Utility.Loading;
using LichLord.Projectiles;
using UnityEngine;

namespace LichLord
{
    public class MuzzleComponent : MonoBehaviour
    {
        [SerializeField]
        private PlayerCharacter _pc;

        [SerializeField] 
        private Transform _handBoneLeft;
        
        [SerializeField] 
        private Transform _handBoneRight;

        // VISUALS

        private AssetBundleLoader PrefabLoader;
        public DWDObjectPoolObject VisualsPrefab { get; private set; }
        public VisualEffectBase VisualsInstance { get; private set; }

        public void OnRender()
        {
            
        }

        public Vector3 GetMuzzlePosition(EMuzzle muzzle)
        {
            return _pc.Weapons.GetMuzzlePosition(muzzle);
        }

        private void LoadMuzzleEffectVisualsPrefab(BundleObject prefabBundle)
        {
            ClearVisuals();

            if (prefabBundle.Ready == false)
            {
                Debug.LogWarning("Cannot spawn Muzzle Visuals for " + prefabBundle.Name + ".  Visual Bundle is not ready.");
                return;
            }

            PrefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            if (PrefabLoader != null)
            {
                if (PrefabLoader.IsLoaded)
                    SpawnLoadedVisuals(PrefabLoader);
                else
                    PrefabLoader.OnLoadComplete += OnVisualsPrefabLoaded;
            }
        }

        private void ClearVisuals()
        {
            if (VisualsInstance != null)
                VisualsInstance.StartRecycle();
        }

        private void OnVisualsPrefabLoaded(ILoader clipLoader)
        {
            if (PrefabLoader != null)
                PrefabLoader.OnLoadComplete -= OnVisualsPrefabLoaded;

            SpawnLoadedVisuals(clipLoader);
        }

        private void SpawnLoadedVisuals(ILoader clipLoader)
        {
            /*
            AssetBundleLoader loader = clipLoader as AssetBundleLoader;
            GameObject go = loader.GetAssetWithin<GameObject>();

            if (Definition == null)
            {
                // Projectile is deactivated before the visual is loaded. Dipose here.
                return;
            }

            VisualsPrefab = go.GetComponent<DWDObjectPoolObject>();

            if (VisualsPrefab == null)
            {
                Debug.LogWarning("Could not spawn Visuals Prefab for Projectile " + Definition.name + ".  Could not find DWDObjectPoolObject Component!");
                return;
            }

            VisualsInstance = DWDObjectPool.Instance.SpawnAt(VisualsPrefab, Position, Quaternion.identity) as VisualEffectBase;

            if (VisualsInstance != null)
                VisualsInstance.InitializeVisuals(this);
            else
                Debug.LogWarning("Visual Instance for Projectile is null!  Object Pool failed to spawn for " + Definition.name);
            */
        }

    }
}

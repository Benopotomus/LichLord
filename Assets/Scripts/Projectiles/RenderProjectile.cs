
namespace LichLord.Projectiles
{
    using UnityEngine;
    using DWD.Utility.Loading;
    using DWD.Pooling;
    using Fusion;
    using DG.Tweening;

    //using DG.Tweening;

    public class RenderProjectile : Projectile
    {
        public bool IsFinished { get; private set; }

        // Visuals
        private AssetBundleLoader PrefabLoader;
        public DWDObjectPoolObject VisualsPrefab { get; private set; }
        public ProjectileVisualEffect VisualsInstance { get; private set; }
        private const bool _FORCE_WARMUP_VISUALS = false;
        private bool _hasWarmedUp = false;

        // Interpolation
        public Vector3 StartOffset;
        public float InterpolationDuration = 0.1f;
        public Ease InterpolationEase = Ease.OutSine;
        public float InterpolationTime;
        public float RenderTimeSinceFired = 0;

        public void ActivateRender(ref FProjectileData data)
        {
            InterpolationTime = 0f;

            IsFinished = false;
            Instigator = data.InstigatorID.GetHitInstigator(Runner);
            Definition = Global.Tables.ProjectileTable.TryGetDefinition(data.DefinitionID);
            Timestamp = data.FireTick * Runner.DeltaTime;
            FireTick = data.FireTick;
            RenderTimeSinceFired = 0;

            if (Definition != null)
            {
                Definition.ProjectileMovement.ActivateRender(this, ref data);
                LoadVisualsPrefab(Definition.VisualsPrefab);
            }

            AffectedActors.Clear();
        }

        public void DeactivateRenderProjectile()
        {
            Instigator = null;
            Definition = null;
            Timestamp = 0f;
            FireTick = 0;
            Rotation = Quaternion.identity;

            ClearVisuals();
        }

        private void ClearVisuals()
        {
            if (VisualsInstance != null)
                VisualsInstance.StartRecycle();
        }

        public void OnRender(ref FProjectileData toData, ref FProjectileData fromData, float bufferAlpha, float renderTime, float networkDelta, float localDelta, int tick)
        {
            if (toData.IsFinished == true)
            {
                IsFinished = true;
                return;
            }

            if (Definition == null || VisualsInstance == null)
                return;

            if (Definition.TableID != toData.DefinitionID)
            { 
                ActivateRender(ref toData);
            }

            Timestamp = toData.FireTick * networkDelta;
            float renderTimeSinceFired = renderTime - Timestamp;

            Definition.ProjectileMovement.OnRender(this, ref toData, ref fromData, bufferAlpha, localDelta, renderTimeSinceFired, tick);

            VisualsInstance.UpdateVisuals(this);
        }

        // VISUALS

        private void LoadVisualsPrefab(BundleObject prefabBundle)
        {
            ClearVisuals();

            if (prefabBundle.Ready == false)
            {
                Debug.LogWarning("Cannot spawn Projectile Visuals for " + Definition.name + ".  Visual Bundle is not ready.");
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

        private void OnVisualsPrefabLoaded(ILoader clipLoader)
        {
            if (PrefabLoader != null)
                PrefabLoader.OnLoadComplete -= OnVisualsPrefabLoaded;

            SpawnLoadedVisuals(clipLoader);
        }

        private void SpawnLoadedVisuals(ILoader clipLoader)
        {
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
            if(_FORCE_WARMUP_VISUALS && _hasWarmedUp == false)
            {
                _hasWarmedUp = true;
                Shader.WarmupAllShaders();
            }
            VisualsInstance = DWDObjectPool.Instance.SpawnAt(VisualsPrefab, Position, Quaternion.identity) as ProjectileVisualEffect;

            if (VisualsInstance != null)
                VisualsInstance.InitializeVisuals(this);
            else
                Debug.LogWarning("Visual Instance for Projectile is null!  Object Pool failed to spawn for " + Definition.name);
        }

        
    }
}

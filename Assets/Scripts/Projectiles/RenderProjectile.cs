
namespace LichLord.Projectiles
{
    using UnityEngine;
    using DWD.Utility.Loading;
    using DWD.Pooling;
    using Fusion;
    using DG.Tweening;

    //using DG.Tweening;

    public class RenderProjectile
    {
        public ProjectilePool OwningPool { get; set; }
        public NetworkRunner Runner => OwningPool.Runner;
        public bool IsFinished { get; private set; }

        public ProjectileDefinition Definition { get; private set; }
        public INetActor Instigator { get; set; }
        public INetActor Target { get; private set; }
        public Vector2 SpawnPosition { get; private set; }
        public Vector2 TargetPosition { get; private set; }
        public float Timestamp { get; set; }
        public int FireTick { get; set; }

        public INetActor EncircleAttachedActor { get; set; }
        public Vector2 EncircleDirection { get; set; }

        public Vector3 RenderPosition { get; set; }
        public Quaternion RenderRotation { get; set; }
        public Vector3 RenderVelocity { get; set; }

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

        public void ActivateRender(ref FProjectileData data)
        {
            InterpolationTime = 0f;

            IsFinished = false;
            Definition = Global.Tables.ProjectileTable.TryGetDefinition(data.DefinitionID);
            Timestamp = data.FireTick * Runner.DeltaTime;
            FireTick = data.FireTick;
            
            if (Definition != null)
            {
                Definition.ProjectileMovement.ActivateRender(this, ref data);
                LoadVisualsPrefab(Definition.VisualsPrefab);
            }
        }

        public void DeactivateRenderProjectile()
        {
            Definition = null;
            Timestamp = 0f;
            FireTick = 0;
            SpawnPosition = Vector3.zero;
            TargetPosition = Vector3.zero;
            RenderRotation = Quaternion.identity;

            ClearVisuals();
        }

        private void ClearVisuals()
        {
            if (VisualsInstance != null)
                VisualsInstance.StartRecycle();
        }

        public void OnRender(ref FProjectileData toData, ref FProjectileData fromData, float bufferAlpha, float renderTime, float networkDelta, float localDelta)
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
            Definition.ProjectileMovement.OnRender(this, ref toData, ref fromData, bufferAlpha, localDelta, renderTimeSinceFired);

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
            VisualsInstance = DWDObjectPool.Instance.SpawnAt(VisualsPrefab, RenderPosition, Quaternion.identity) as ProjectileVisualEffect;

            if (VisualsInstance != null)
                VisualsInstance.InitializeVisuals(this);
            else
                Debug.LogWarning("Visual Instance for Projectile is null!  Object Pool failed to spawn for " + Definition.name);
        }

        
    }
}

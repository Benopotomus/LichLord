
namespace LichLord.Projectiles
{
    using UnityEngine;
    using DWD.Utility.Loading;
    using DWD.Pooling;
    using DG.Tweening;
    using LichLord.Props;

    //using DG.Tweening;

    public class RenderProjectile : Projectile
    {
        public bool IsFinished { get; private set; }

        // Visuals
        private ProjectileVisualSpawner VisualSpawner = new ProjectileVisualSpawner();
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

        private int _lastTick;

        public void ActivateRender(ref FProjectileData data)
        {
            InterpolationTime = 0f;

            IsFinished = false;
            Instigator = data.InstigatorID.GetHitInstigator(Runner);
            Definition = Global.Tables.ProjectileTable.TryGetDefinition(data.DefinitionID);
            Timestamp = data.FireTick * Runner.DeltaTime;
            FireTick = data.FireTick;
            RenderTimeSinceFired = 0;
            _lastTick = 0;

            if (Definition != null)
            {
                Definition.ProjectileMovement.ActivateRender(this, ref data);
                LoadVisualsPrefab(Definition.VisualsPrefab, ref data);
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

            UpdateNPCProjectileCasts(ref toData, tick, renderTime, networkDelta, localDelta);

            VisualsInstance.UpdateVisuals(this, ref toData);
        }

        // VISUALS

        private void LoadVisualsPrefab(BundleObject prefabBundle, ref FProjectileData data)
        {
            ClearVisuals();

            VisualSpawner.OnProjectileVisualSpawned += OnVisualsPrefabLoaded;
            VisualSpawner.SpawnProjectileVisual(Definition, ref data);
        }

        private void OnVisualsPrefabLoaded(GameObject go, FProjectileData data)
        {
            VisualSpawner.OnProjectileVisualSpawned -= OnVisualsPrefabLoaded;

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
                VisualsInstance.InitializeVisuals(this, ref data);
            else
                Debug.LogWarning("Visual Instance for Projectile is null!  Object Pool failed to spawn for " + Definition.name);
        }

        // CLIENT CHECKS

        private void UpdateNPCProjectileCasts(ref FProjectileData data, int tick, float simulationTime, float networkDeltaTime, float localDeltaTime)
        {
            if (!IsNPCProjectile)
                return;

            if (tick == _lastTick)
                return;

            _lastTick = tick;

            Vector3 newPosition = Position;
            Quaternion newRotation = Rotation;

            ProjectilePhysicsUtility.CheckAndHandleCollision(this,
                ref data,
                tick,
                simulationTime,
                networkDeltaTime,
                newPosition,
                newPosition,
                newRotation,
                newRotation);
        }
        
    }
}

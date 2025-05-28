
// the pool of monster projectiles

namespace LichLord.Projectiles
{
    using Fusion;
    using UnityEngine;

    public class ServerProjectilePool : ProjectilePool
    {
        /*
        protected new const int MAX_PROJECTILE_COUNT = 128;

        [Networked, Capacity(MAX_PROJECTILE_COUNT)]
        protected override NetworkArray<FProjectileData> _projectileDatas { get; }

        public override void Spawned()
        {
            _views = new(MAX_PROJECTILE_COUNT);
            _finishedViews = new(MAX_PROJECTILE_COUNT);
            _viewCount = _dataCount;

            _dataBufferReader = GetArrayReader<FProjectileData>(nameof(_projectileDatas));
            _dataCountReader = GetPropertyReader<int>(nameof(_dataCount));

            SetupFixedUpdateProjectiles(MAX_PROJECTILE_COUNT);
        }

        public override FixedUpdateProjectile SpawnProjectile(FProjectileFireEvent fireEvent)
        {
            if (fireEvent.projectileDefinition == null)
            {
                Debug.LogWarning("Spawing projectile with no definition");
                return null;
            }

            int dataIndex = _dataCount % MAX_PROJECTILE_COUNT;

            FProjectileData spawnData = GetProjectileData(fireEvent);
            _projectileDatas.Set(dataIndex, spawnData);

            FixedUpdateProjectile projectile = _fixedUpdateProjectiles[dataIndex];
            projectile.ActivateFixedUpdate(ref _projectileDatas.GetRef(dataIndex),
                ref fireEvent.payload,
                ref fireEvent.payload_spawnedProjectile);

            _dataCount++;

            return _fixedUpdateProjectiles[dataIndex];
        }

        public override void FixedUpdateNetwork()
        {
            if (IsProxy)
                return;

            int tick = Runner.Tick;
            float simulationTime = Runner.SimulationTime;
            float deltaTime = Runner.DeltaTime;

            for (int i = 0; i < MAX_PROJECTILE_COUNT; i++)
            {
                //ResetStaleProjectileData(ref _projectileDatas.GetRef(i), tick);
                UpdateData(i, ref _projectileDatas.GetRef(i), tick, simulationTime, deltaTime);
            }
        }
        */
    }
}
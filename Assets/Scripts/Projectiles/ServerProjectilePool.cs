
// the pool of monster projectiles

namespace LichLord.Projectiles
{
    using Fusion;
    using UnityEngine;

    public class ServerProjectilePool : ProjectilePool
    {
        protected new const int MAX_PROJECTILE_COUNT = 256;

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

            // For late-joining clients, spawn RenderProjectile instances for existing active projectiles
            if (!HasStateAuthority) // Only clients need to sync visuals, as server manages data
            {
                int bufferLength = _projectileDatas.Length;
                for (int i = 0; i < _dataCount; i++)
                {
                    int bufferIndex = i % bufferLength;
                    FProjectileData data = _projectileDatas[bufferIndex];

                    // Skip finished or invalid projectiles
                    if (data.IsFinished || data.DefinitionID == 0)
                        continue;

                    // Skip if a view already exists (shouldn't happen, but for safety)
                    if (_views.ContainsKey(i))
                        continue;

                    // Spawn a RenderProjectile for this projectile
                    RenderProjectile projectile = ProjectileViewPool.Get<RenderProjectile>();
                    if (projectile == null)
                    {
                        Debug.LogWarning($"[ProjectilePool] Failed to get RenderProjectile for index {i}");
                        continue;
                    }

                    projectile.OwningPool = this;
                    projectile.ActivateRender(ref data);

                    // Create and store ViewEntry
                    ViewEntry newEntry = ProjectileViewPool.Get<ViewEntry>();
                    newEntry.Projectile = projectile;
                    newEntry.LastData = data;
                    _views.Add(i, newEntry);
                }

                _viewCount = _dataCount; // Ensure view count matches data count
            }
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
            if (!Runner.IsForward
                || !Runner.IsFirstTick)
                return;

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

        protected override void SetupFixedUpdateProjectiles(int count)
        {
            _fixedUpdateProjectiles = new FixedUpdateProjectile[count];
            for (int i = 0; i < count; i++)
            {
                FixedUpdateProjectile projectile = new FixedUpdateProjectile();
                projectile.Index = i;
                projectile.OwningPool = this;
                projectile.IsNPCProjectile = true;
                projectile.Context = Context;
                projectile.StateAuthority = Object.StateAuthority;
                _fixedUpdateProjectiles[i] = projectile;
            }
        }

        protected override void SetupRenderProjectile(ref FProjectileData data, RenderProjectile projectile, int index)
        {
            projectile.OwningPool = this;
            projectile.Index = index;
            projectile.IsNPCProjectile = true;
            projectile.ActivateRender(ref data);
        }

    }
}
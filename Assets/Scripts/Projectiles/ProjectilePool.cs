
namespace LichLord.Projectiles
{
    using Fusion;
    using UnityEngine;
    using System.Collections.Generic;

    public partial class ProjectilePool : ContextBehaviour
    {
        protected const int MAX_PROJECTILE_COUNT = 64;

        [Networked, Capacity(MAX_PROJECTILE_COUNT)]
        protected virtual NetworkArray<FProjectileData> _projectileDatas { get; }

        [Networked]
        protected int _dataCount { get; set; }

        protected Dictionary<int, ViewEntry> _views;
        protected List<int> _finishedViews;
        protected int _viewCount;

        protected ArrayReader<FProjectileData> _dataBufferReader;
        protected PropertyReader<int> _dataCountReader;

        protected FixedUpdateProjectile[] _fixedUpdateProjectiles;

        protected readonly List<FixedUpdateProjectile> _activeProjectiles = new List<FixedUpdateProjectile>();
        protected readonly HashSet<int> _activeProjectileIndices = new HashSet<int>();

        public List<FixedUpdateProjectile> ActiveFixedUpdateProjectiles => _activeProjectiles;

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

        protected void SetupFixedUpdateProjectiles(int count)
        {
            _fixedUpdateProjectiles = new FixedUpdateProjectile[count];
            for (int i = 0; i < count; i++)
            {
                FixedUpdateProjectile projectile = new FixedUpdateProjectile();
                projectile.Index = i;
                projectile.OwningPool = this;
                projectile.Context = Context;
                projectile.StateAuthority = Object.InputAuthority;
                _fixedUpdateProjectiles[i] = projectile;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            foreach (var pair in _views)
                ReturnEntry(pair.Value, false);

            _views.Clear();
        }

        public virtual FixedUpdateProjectile SpawnProjectile(FProjectileFireEvent fireEvent)
        {
            if (fireEvent.projectileDefinition == null)
            {
                Debug.LogWarning("Spawing projectile with no definition");
                return null;
            }

            int dataIndex = _dataCount % _projectileDatas.Length;

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
            if (!Context.IsGameplayActive())
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

        protected virtual void UpdateData(int index, ref FProjectileData data, int tick, float simulationTime, float deltaTime)
        {
            FixedUpdateProjectile projectile = _fixedUpdateProjectiles[index];
            projectile.OnFixedUpdate(ref data, tick, simulationTime, deltaTime);
        }

        public override void Render()
        {
            if (!Context.IsGameplayActive())
                return;

            float renderTime = HasStateAuthority ? Runner.LocalRenderTime : Runner.RemoteRenderTime;
            int tick = Runner.Tick;
            float localDeltaTime = Runner.LocalAlpha;
            float networkDeltaTime = Runner.DeltaTime;

            if (TryGetSnapshotsBuffers(out var fromNetworkBuffer, out var toNetworkBuffer, out float bufferAlpha) == false)
                return;

            NetworkArrayReadOnly<FProjectileData> fromDataBuffer = _dataBufferReader.Read(fromNetworkBuffer);
            NetworkArrayReadOnly<FProjectileData> toDataBuffer = _dataBufferReader.Read(toNetworkBuffer);
            int fromDataCount = _dataCountReader.Read(fromNetworkBuffer);
            int toDataCount = _dataCountReader.Read(toNetworkBuffer);

            int bufferLength = _projectileDatas.Length;

            float ping = (float)Runner.GetPlayerRtt(Object.StateAuthority);

            // If our predicted views were not confirmed by the server, discard them
            for (int i = fromDataCount; i < _viewCount; i++)
            {
                if (_views.TryGetValue(i, out ViewEntry viewEntry) == false)
                    continue;

                ReturnEntry(viewEntry, true);
                _views.Remove(i);
            }

            // Spawn missing views
            for (int i = _viewCount; i < fromDataCount; i++)
            {
                int bufferIndex = i % bufferLength;
                var data = fromDataBuffer[bufferIndex];

                if (_views.TryGetValue(i, out ViewEntry oldEntry) == true)
                    continue;

                RenderProjectile projectile = ProjectileViewPool.Get<RenderProjectile>();
                if (projectile == null)
                    continue;

                projectile.OwningPool = this;
                projectile.ActivateRender(ref data);

                ViewEntry newEntry = ProjectileViewPool.Get<ViewEntry>();
                newEntry.Projectile = projectile;
                _views.Add(i, newEntry);
            }

            // At some point the buffer will be overriden
            // by new data (new buffer cycle) so we need to calculate
            // last valid data key in the buffer.
            int minDataKey = toDataCount - bufferLength;

            // Update all visible views
            foreach (var pair in _views)
            {
                RenderProjectile projectile = pair.Value.Projectile;

                if (pair.Key >= minDataKey)
                {
                    int bufferIndex = pair.Key % bufferLength;

                    var toData = toDataBuffer[bufferIndex];
                    var fromData = fromDataBuffer[bufferIndex];

                    //if (HasInputAuthority)
                    //   Debug.Log("Key: " + pair.Key + " toData Finished: " + toData.IsFinished +  " fromData Finished: " + fromData.IsFinished + " RT: " + Runner.LocalRenderTime);

                    projectile.OnRender(ref toData, ref fromData, bufferAlpha, renderTime, networkDeltaTime, localDeltaTime, tick);
                    pair.Value.LastData = toData;
                }
                else
                {

                    //Debug.Log("First Render " + Runner.SimulationTime);
                    // Use last data to Render when there are no data available in the buffer
                    projectile.OnRender(ref pair.Value.LastData, ref pair.Value.LastData, 0f, renderTime, networkDeltaTime, localDeltaTime, tick);
                }

                if (projectile.IsFinished == true)
                {
                    ReturnEntry(pair.Value, false);
                    _finishedViews.Add(pair.Key);
                }
            }

            for (int i = 0; i < _finishedViews.Count; i++)
            {
                _views.Remove(_finishedViews[i]);
            }

            _finishedViews.Clear();
            _viewCount = fromDataCount;
        }

        // PRIVATE METHODS

        private void ReturnEntry(ViewEntry entry, bool misprediction)
        {
            ReturnView(entry.Projectile, misprediction);
            ProjectileViewPool.Return(entry);
        }

        protected void ReturnView(RenderProjectile projectile, bool misprediction)
        {
            if (projectile == null)
                return;

            projectile.DeactivateRenderProjectile();
        }

        // Sets data on the server
        public FProjectileData GetProjectileData(FProjectileFireEvent fireEvent)
        {
            FProjectileData data = new FProjectileData();
            ProjectileDefinition definition = fireEvent.projectileDefinition;

            data.InstigatorID = fireEvent.instigator.NetActor.NetObjectID;
            data.HasStopped = false;
            data.IsFinished = false;
            data.DefinitionID = (ushort)definition.TableID;
            data.FireTick = fireEvent.fireTick;
            data.Position = fireEvent.spawnPosition;
            data.TargetPosition = fireEvent.targetPosition;

            ProjectileMovement projectileMovement = definition.ProjectileMovement;

            return data;
        }

        void OnDrawGizmos()
        {
            
            if (_fixedUpdateProjectiles == null) return;

            for (int i = 0; i < MAX_PROJECTILE_COUNT; i++)
            {
                FixedUpdateProjectile projectile = _fixedUpdateProjectiles[i];
                if (projectile == null || projectile.Definition == null) continue;

                Gizmos.color = Color.blue;

                // Ensure rotation values are correct
                Quaternion rotation = projectile.Rotation;

                // If the rotation is invalid (near zero), use identity to avoid errors
                float sqrMagnitude = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
                if (sqrMagnitude < 0.0001f)
                {
                    rotation = Quaternion.identity;
                }

                // Use correct transformation matrix assignment
                Gizmos.matrix = Matrix4x4.TRS(projectile.Position, rotation, Vector3.one);

                float simTimeSinceFired = Runner.SimulationTime - (projectile.FireTick * projectile.Runner.DeltaTime);

                float scale = ProjectilePhysicsUtility.GetScaleAtTime(projectile.Definition, simTimeSinceFired);

                switch (projectile.Definition.Shape)
                {
                    case EShapeType.Sphere:
                        Gizmos.DrawWireSphere(Vector3.zero, projectile.Definition.Extents.x * scale);
                        break;
                    case EShapeType.Raycast:
                        Gizmos.DrawWireSphere(Vector3.zero, projectile.Definition.Extents.x);
                        break;
                }

                // Restore default transformation
                Gizmos.matrix = Matrix4x4.identity;
            }
            
        }

        protected class ViewEntry
        {
            public RenderProjectile Projectile;
            public FProjectileData LastData;
        }
    }



} 
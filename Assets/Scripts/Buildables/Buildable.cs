using DWD.Pooling;
using LichLord.World;
using UnityEngine;

namespace LichLord.Buildables
{
    public class Buildable : DWDObjectPoolObject, IHitTarget, IChunkTrackable
    {
        private SceneContext _sceneContext;
        public SceneContext Context => _sceneContext;

        private BuildableZone _zone;
        public BuildableZone Zone => _zone;

        private Lair _lair;
        public Lair Lair => _lair;

        [SerializeField]
        protected BuildableRuntimeState _runtimeState;
        public BuildableRuntimeState RuntimeState => _runtimeState;

        [SerializeField] 
        private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        // IChunkTrackable
        private Chunk _chunk;
        public Chunk CurrentChunk { get => _chunk; set { } }
        public Vector3 Position => _cachedTransform.position;
        public Vector3 PredictedPosition => _cachedTransform.position;
        public virtual bool IsAttackable => false;
        public virtual float BonusRadius { get { return 0; } }
        public virtual Collider HurtBoxCollider { get { return null; } }

        // IHitTarget
        public IChunkTrackable ChunkTrackable => this;

        public HurtboxComponent Hurtbox;
        [SerializeField] protected BuildableSpawnTransformer _spawnTransformer;
        public BuildableSpawnTransformer SpawnTransformer => _spawnTransformer;

        public FNetObjectID NetObjectID
        {
            get
            {
                FNetObjectID newId = new FNetObjectID();

                if(RuntimeState == null)
                    return newId;

                switch (RuntimeState.LairID)
                {
                    case 0:
                        newId.SetObjectType(EObjectType.Buildable_Lair_0);
                        break;
                    case 1:
                        newId.SetObjectType(EObjectType.Buildable_Lair_1);
                        break;
                    case 2:
                        newId.SetObjectType(EObjectType.Buildable_Lair_2);
                        break;
                    case 3:
                        newId.SetObjectType(EObjectType.Buildable_Lair_3);
                        break;

                }
                
                newId.SetIndex(RuntimeState.Index);
                return newId;
            }
        }

        public virtual void OnSpawned(BuildableZone zone, BuildableRuntimeState runtimeState)
        {
            _runtimeState = runtimeState;
            _cachedTransform.position = runtimeState.Data.Position;
            _cachedTransform.rotation = runtimeState.Data.Rotation;

            _zone = zone;
            _lair = zone.Lair;
            _sceneContext = zone.Context;
            _chunk = Context.ChunkManager.GetChunkAtPosition(_cachedTransform.position);
            _chunk.AddObject(this);
            _chunk.AddHitTarget(this);

            _spawnTransformer.PlaySpawnAnimation();
        }

        public virtual void OnRender(BuildableRuntimeState runtimeState, float renderDeltaTime, int tick, bool hasAuthority) 
        {
            _runtimeState = runtimeState;
        }

        public virtual void StartRecycle()
        {
            _chunk.RemoveObject(this);
            _chunk.RemoveHitTarget(this);

            DWDObjectPool.Instance.Recycle(this);
        }

        public virtual void OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public virtual void ProcessHit(ref FHitUtilityData hit)
        {
        }
    }
}
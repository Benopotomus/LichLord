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
        public virtual bool IsAttackable => false;
        public virtual float BonusRadius { get { return 0; } }
        public virtual Collider HurtBoxCollider { get { return null; } }

        public HurtboxComponent Hurtbox;

        public virtual void OnSpawned(BuildableZone zone, BuildableRuntimeState runtimeState)
        {
            _runtimeState = runtimeState;
            _cachedTransform.position = runtimeState.position;
            _cachedTransform.rotation = runtimeState.rotation;

            _zone = zone;
            _sceneContext = zone.Context;
            _chunk = Context.ChunkManager.GetChunkAtPosition(_cachedTransform.position);
            _chunk.AddObject(this);
        }

        public virtual void OnRender(BuildableRuntimeState runtimeState, float renderDeltaTime, bool hasAuthority) 
        {
            _runtimeState = runtimeState;
        }

        public virtual void StartRecycle()
        {
            _chunk.RemoveObject(this);
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
using UnityEngine;
using DWD.Pooling;
using LichLord.World;

namespace LichLord.Props
{
    public class Prop : DWDObjectPoolObject, IChunkTrackable
    {
        protected PropManager _propManager;
        protected SceneContext _sceneContext;
        public SceneContext Context => _sceneContext;

        [SerializeField]
        protected PropRuntimeState _runtimeState;
        public PropRuntimeState RuntimeState => _runtimeState;

        public FChunkPosition ChunkID;
        public int Index;

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        // IChunkTrackable
        protected FChunkReference _chunk;
        public FChunkReference CurrentChunk { get { return _chunk; } set { } }

        public Vector3 Position => CachedTransform.position;

        public virtual bool IsAttackable => false;

        public virtual Collider HurtBoxCollider { get { return null; } }

        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _runtimeState = propRuntimeState;
            _propManager = propManager;
            _sceneContext = propManager.Context;

            CachedTransform.position = _runtimeState.position;
            CachedTransform.rotation = _runtimeState.rotation;
            CachedTransform.localScale = _runtimeState.scale;

            ChunkID = propRuntimeState.chunk.ChunkID;
            Index = propRuntimeState.index;

            _chunk.Chunk = propRuntimeState.chunk;
            if(_chunk.IsValid)
                _chunk.Chunk.AddObject(this);
        }

        public virtual void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            _runtimeState = propRuntimeState;
        }

        public virtual void StartRecycle()
        {
            DWDObjectPool.Instance.Recycle(this);

            if (_chunk.IsValid)
                _chunk.Chunk.RemoveObject(this);
        }
    }
}
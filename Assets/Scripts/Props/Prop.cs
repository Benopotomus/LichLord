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
        public Chunk CurrentChunk { get => RuntimeState.chunk; set => value = RuntimeState.chunk; }
        public Vector3 Position => _cachedTransform.position;
        public Vector3 PredictedPosition => _cachedTransform.position;

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

            CurrentChunk.AddObject(this);
        }

        public virtual void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            _runtimeState = propRuntimeState;
        }

        public virtual void StartRecycle()
        {
            DWDObjectPool.Instance.Recycle(this);
            CurrentChunk.RemoveObject(this);
        }
    }
}
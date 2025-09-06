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
        public int GUID;

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;


        // IChunkTrackable
        public Chunk CurrentChunk { get => RuntimeState.chunk; set => value = RuntimeState.chunk; }
        public Vector3 Position => CachedTransform.position;
        public virtual float BonusRadius { get; } // Extra radius added for npc maneuvers to determine if they're in range.
        public virtual bool IsAttackable
        {
            get
            {
                 return false;
            }
        }
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
            GUID = propRuntimeState.index;

            CurrentChunk.AddObject(this);
        }

        // This is the visuals for authority and client.
        // Read only - no logic should update here.
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
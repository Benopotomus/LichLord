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
        protected PropRuntimeState _propRuntimeState;
        public PropRuntimeState RuntimeState => _propRuntimeState;

        public FChunkPosition ChunkID;
        public int GUID;

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        // IChunkTrackable
        public Chunk CurrentChunk { get => RuntimeState.chunk; set => value = RuntimeState.chunk; }

        // Extra radius added for npc maneuvers to determine if they're in range.
        public float BonusRadius { get; }

        public Vector3 Position => CachedTransform.position;
        public virtual bool IsAttackable
        {
            get
            {
                 return false;
            }
        }
        
        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _propRuntimeState = propRuntimeState;
            _propManager = propManager;
            _sceneContext = propManager.Context;

            CachedTransform.position = _propRuntimeState.position;
            CachedTransform.rotation = _propRuntimeState.rotation;

            ChunkID = propRuntimeState.chunk.ChunkID;
            GUID = propRuntimeState.guid;

            CurrentChunk.AddObject(this);
        }

        // This is the visuals for authority and client.
        // Read only - no logic should update here.
        public virtual void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            _propRuntimeState = propRuntimeState;
        }

        public virtual void StartRecycle()
        {
            DWDObjectPool.Instance.Recycle(this);
            CurrentChunk.RemoveObject(this);
        }
    }
}
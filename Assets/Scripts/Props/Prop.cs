using UnityEngine;
using DWD.Pooling;
using LichLord.World;

namespace LichLord.Props
{
    public class Prop : DWDObjectPoolObject, IHitTarget, IChunkTrackable
    {
        [SerializeField]
        protected PropStateComponent _stateComponent;
        public PropStateComponent StateComponent => _stateComponent;

        [SerializeField]
        protected PropHealthComponent _healthComponent;
        public PropHealthComponent HealthComponent => _healthComponent;

        protected PropManager _propManager;
        protected SceneContext _sceneContext;
        public SceneContext Context => _sceneContext;

        [SerializeField]
        protected PropRuntimeState _propRuntimeState;
        public PropRuntimeState RuntimeState => _propRuntimeState;

        public FChunkPosition ChunkID;
        public int GUID;

        [SerializeField] 
        protected PropDefinition _propDefinition;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        // IChunkTrackable
        public Chunk CurrentChunk { get => RuntimeState.chunk; set => value = RuntimeState.chunk; }

        public Vector3 Position => CachedTransform.position;
        public virtual bool IsAttackable
        {
            get
            {
                 return false;
            }
        }
        
        public HurtboxComponent Hurtbox;

        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _propRuntimeState = propRuntimeState;
            _propManager = propManager;
            _sceneContext = propManager.Context;
            _propDefinition = propRuntimeState.Definition;

            _stateComponent.UpdateState(_propRuntimeState.GetState());
            _healthComponent.UpdateHealth(_propRuntimeState.GetHealth());

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
            _stateComponent.UpdateState(propRuntimeState.GetState());
            _healthComponent.UpdateHealth(propRuntimeState.GetHealth());
        }

        public virtual void StartRecycle()
        {
            DWDObjectPool.Instance.Recycle(this);
            CurrentChunk.RemoveObject(this);
        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {
        }
    }
}
using UnityEngine;
using DWD.Pooling;
using LichLord.World;

namespace LichLord.Props
{
    public class Prop : DWDObjectPoolObject, IHitTarget, IChunkTrackable
    {
        [SerializeField]
        private PropStateComponent _stateComponent;
        public PropStateComponent StateComponent => _stateComponent;

        [SerializeField]
        private PropHealthComponent _healthComponent;
        public PropHealthComponent HealthComponent => _healthComponent;

        protected PropManager _propManager;

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
        public bool IsAttackable
        {
            get
            {
                switch (_stateComponent.CurrentState)
                {
                    case EPropState.Destroyed:
                    case EPropState.Inactive:
                        return false;
                    default:
                        return true;
                }
            }
        }
        
        public HurtboxComponent Hurtbox;

        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _propRuntimeState = propRuntimeState;
            _propManager = propManager;
            _propDefinition = propRuntimeState.Definition;

            _stateComponent.UpdateState(_propRuntimeState.GetState());
            _healthComponent.UpdateHealth(_propRuntimeState.GetHealth());

            CachedTransform.position = _propRuntimeState.position;
            CachedTransform.rotation = _propRuntimeState.rotation;

            ChunkID = propRuntimeState.chunk.ChunkID;
            GUID = propRuntimeState.guid;
        }

        // This is the visuals for authority and client.
        // Read only - no logic should update here.
        public virtual void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            _propRuntimeState = propRuntimeState;
            _stateComponent.UpdateState(propRuntimeState.GetState());
            _healthComponent.UpdateHealth(propRuntimeState.GetHealth());
        }

        public void StartRecycle()
        {
            DWDObjectPool.Instance.Recycle(this);
        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {
        }
    }
}
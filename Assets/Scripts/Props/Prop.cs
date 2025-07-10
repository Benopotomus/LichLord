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

        [SerializeField] 
        protected PropDefinition _propDefinition;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        // IChunkTrackable
        private Chunk _currentChunk;
        public Chunk CurrentChunk { get => _currentChunk; set => _currentChunk = value; }

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

            _currentChunk = propRuntimeState.chunk;
        }

        // This is the visuals for authority and client.
        // Read only - no logic should update here.
        public virtual void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            _propRuntimeState = propRuntimeState;
            _stateComponent.UpdateState(_propRuntimeState.GetState());
            _healthComponent.UpdateHealth(_propRuntimeState.GetHealth());
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
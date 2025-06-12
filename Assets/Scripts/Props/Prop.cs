using UnityEngine;
using DWD.Pooling;

namespace LichLord.Props
{
    public class Prop : DWDObjectPoolObject, IHitTarget
    {
        [SerializeField]
        private PropStateComponent _propStateComponent;
        public PropStateComponent StateComponent => _propStateComponent;

        protected PropManager _propManager;

        [SerializeField]
        protected PropRuntimeState _propRuntimeState;
        public PropRuntimeState RuntimeState => _propRuntimeState;

        [SerializeField] 
        protected PropDefinition _propDefinition;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        public HurtboxComponent Hurtbox;

        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _propRuntimeState = propRuntimeState;
            _propManager = propManager;
            _propDefinition = propRuntimeState.Definition;

            CachedTransform.position = _propRuntimeState.position;
            CachedTransform.rotation = _propRuntimeState.rotation;
        }

        // This is the visuals for authority and client.
        // Read only - no logic should update here.
        public virtual void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            _propRuntimeState = propRuntimeState;
            _propStateComponent.UpdateState(_propRuntimeState.GetState());
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
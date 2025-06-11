using UnityEngine;
using DG.Tweening;
using DWD.Pooling;
using Fusion;

namespace LichLord.Props
{
    public class Prop : DWDObjectPoolObject, IHitTarget
    {
        protected PropManager _propManager;

        [SerializeField]
        protected PropRuntimeState _propRuntimeState;

        public PropRuntimeState RuntimeState => _propRuntimeState;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        public HurtboxComponent Hurtbox;

        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _propRuntimeState = propRuntimeState;
            _propManager = propManager;

            CachedTransform.position = _propRuntimeState.position;
            CachedTransform.rotation = _propRuntimeState.rotation;
        }

        public virtual void UpdateRuntimeState()
        { 
        
        }

        public virtual void UpdateProp(PropRuntimeState propState, float renderDeltaTime)
        {
            if (propState.stateData == 1)
            {
                gameObject.SetActive(false);
            }
            else
                gameObject.SetActive(true);

        }

        protected virtual void UpdateAllStates() { }

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
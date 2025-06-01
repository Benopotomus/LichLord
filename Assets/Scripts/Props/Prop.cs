using UnityEngine;
using DG.Tweening;
using DWD.Pooling;

namespace LichLord.Props
{
    public class Prop : DWDObjectPoolObject
    {
        protected PropManager _propManager;
        protected PropRuntimeState _propRuntimeState;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _propRuntimeState = propRuntimeState;
            _propManager = propManager;

            CachedTransform.position = _propRuntimeState.position;
            CachedTransform.rotation = _propRuntimeState.rotation;
        }

        public virtual void UpdateProp(PropRuntimeState propState, float renderDeltaTime)
        {
            /*
            // Update position and rotation
            Vector3 moveTarget = propData.Position;
            if (CachedTransform.position != moveTarget)
            {
                CachedTransform.position = Vector3.Lerp(CachedTransform.position, moveTarget, 10 * renderDeltaTime);
            }

            Quaternion rotTarget = Quaternion.LookRotation(propData.Forward, Vector3.up);
            if (CachedTransform.rotation != rotTarget)
            {
                CachedTransform.rotation = Quaternion.Slerp(CachedTransform.rotation, rotTarget, 10 * renderDeltaTime);
            }

            // Handle destruction
            if (propData.IsDestroyed)
            {
                StartRecycle();
            }
            */
        }

        protected virtual void UpdateAllStates() { }

        public void StartRecycle()
        {
            DWDObjectPool.Instance.Recycle(this);
        }
    }
}

namespace LichLord
{
    using UnityEngine;
    using DWD.Pooling;
    using System;
    using System.Collections;

    public class VisualEffectBase : DWDObjectPoolObject
    {
        [SerializeField] protected Transform _transform;
        public Transform CachedTransform => _transform;

        [SerializeField] protected float _recycleDelay = 0;

        // Called when the effect is initialized and drawn from the pool
        public Action<VisualEffectBase> onInitialized;

        // Called when the effect is updated
        public Action<VisualEffectBase> onUpdate;

        // Called when the effect is starting to recycle with delay
        public Action<VisualEffectBase> onRecycleDelayStart;

        // Called when the effect is recycled
        public Action<VisualEffectBase> onRecycled;

        // Called when the effect is recycled
        public Action<VisualEffectBase, bool> onToggled;

        public virtual void Initialize()
        {
            onInitialized?.Invoke(this);
        }

        public virtual void Toggle(bool isActive)
        {
            onToggled?.Invoke(this, isActive);
        }

        // Called externally.
        public virtual void StartRecycle()
        {
            if (_recycleDelay == 0)
                RecycleVisualEffect();
            else
                StartCoroutine(RecycleAfterDelay());
        }

        protected IEnumerator RecycleAfterDelay()
        {
            onRecycleDelayStart?.Invoke(this);
            yield return new WaitForSeconds(_recycleDelay);
            RecycleVisualEffect();
        }


        protected virtual void RecycleVisualEffect()
        {
            onRecycled?.Invoke(this);
            DWDObjectPool.Instance.Recycle(this);
        }
    }
}

using DWD.Pooling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.Buildables
{
    public class Buildable : DWDObjectPoolObject, IHitTarget
    {
        protected BuildableManager _buildableManager;
        protected BuildableRuntimeState _buildableRuntimeState;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        public HurtboxComponent Hurtbox;

        public virtual void OnSpawned(BuildableRuntimeState buildableRuntimeState, BuildableManager buildableManager)
        {
            _buildableRuntimeState = buildableRuntimeState;
            _buildableManager = buildableManager;

            CachedTransform.position = _buildableRuntimeState.position;
            CachedTransform.rotation = _buildableRuntimeState.rotation;
        }

        public virtual void UpdateBuildable(BuildableRuntimeState buildableRuntimeState, float renderDeltaTime)
        {
            if (buildableRuntimeState.stateData == 1)
            {
                gameObject.SetActive(false);
            }
            else
                gameObject.SetActive(true);

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
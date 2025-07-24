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
        private BuildableZone _zone;
        public BuildableZone Zone => _zone;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        public HurtboxComponent Hurtbox;

        public virtual void OnSpawned(BuildableZone zone, Vector3 position, Quaternion rotation, int data)
        {
            _zone = zone;

            CachedTransform.position = position;
            CachedTransform.rotation = rotation;
        }

        public virtual void UpdateBuildable(BuildableRuntimeState buildableRuntimeState, float renderDeltaTime)
        {

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
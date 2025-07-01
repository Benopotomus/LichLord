using UnityEngine;
using DWD.Pooling;
using Cinemachine;
using System;

namespace LichLord.Projectiles
{
    public class ProjectileVisualEffect : VisualEffectBase
    {
        [SerializeField] protected CinemachineImpulseSource _cameraShake;

        protected Vector3 _workingPosition = Vector3.zero;
        protected Vector3 _workingEulerAngles = Vector3.zero;
        protected Vector3 _workingScale = Vector3.one;

        public RenderProjectile Projectile { get; private set; }

        private bool _hasImpacted = false;
        public Action<VisualEffectBase> onImpacted;

        public virtual void InitializeVisuals(RenderProjectile projectile, ref FProjectileData data)
        {
            Projectile = projectile;

            if (Projectile == null)
            {
                StartRecycle();
                return;
            }

            UpdateVisuals(projectile, ref data);

            if (_cameraShake != null)
                _cameraShake.GenerateImpulse();

            onInitialized?.Invoke(this);
        }

        public virtual void UpdateVisuals(RenderProjectile projectile, ref FProjectileData data)
        {
            if (projectile.Definition == null)
            {
                StartRecycle();
                return;
            }

            UpdateVisualsRotation(projectile);
            UpdateVisualsPosition(projectile);

            onUpdate?.Invoke(this);


            if (!_hasImpacted && data.HasImpacted)
            {
                _hasImpacted = true;
                onImpacted?.Invoke(this);
            }
        }

        protected virtual void UpdateVisualsPosition(RenderProjectile projectile)
        {
            // Visuals Root is Shadow and the _visual is at projectile position
            CachedTransform.position = projectile.Position;

        }

        protected virtual Quaternion GetVisualsRotationDegrees(RenderProjectile projectile)
        {
            return projectile.Rotation;
        }

        protected virtual void UpdateVisualsRotation(RenderProjectile projectile)
        {
            _workingEulerAngles = GetVisualsRotationDegrees(projectile).eulerAngles;
            CachedTransform.eulerAngles = _workingEulerAngles;
        }

        protected override void RecycleVisualEffect()
        {
            _hasImpacted = false;
            base.RecycleVisualEffect();
        }
    }
}

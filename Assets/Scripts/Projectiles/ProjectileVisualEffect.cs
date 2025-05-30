using UnityEngine;
using DWD.Pooling;
using Cinemachine;

namespace LichLord.Projectiles
{
    public class ProjectileVisualEffect : VisualEffectBase
    {
        [SerializeField] protected CinemachineImpulseSource _cameraShake;

        protected Vector3 _workingPosition = Vector3.zero;
        protected Vector3 _workingEulerAngles = Vector3.zero;
        protected Vector3 _workingScale = Vector3.one;

        public RenderProjectile Projectile { get; private set; }

        public virtual void InitializeVisuals(RenderProjectile projectile)
        {
            Projectile = projectile;

            if (Projectile == null)
            {
                StartRecycle();
                return;
            }

            UpdateVisuals(projectile);

            if (_cameraShake != null)
                _cameraShake.GenerateImpulse();

            onInitialized?.Invoke(this);
        }

        public virtual void UpdateVisuals(RenderProjectile projectile)
        {
            if (projectile.Definition == null)
            {
                StartRecycle();
                return;
            }

            UpdateVisualsRotation(projectile);
            UpdateVisualsPosition(projectile);

            onUpdate?.Invoke(this);
        }

        protected virtual void UpdateVisualsPosition(RenderProjectile projectile)
        {
            // Visuals Root is Shadow and the _visual is at projectile position
            CachedTransform.position = projectile.RenderPosition;

        }

        protected virtual Quaternion GetVisualsRotationDegrees(RenderProjectile projectile)
        {
            return projectile.RenderRotation;
        }

        protected virtual void UpdateVisualsRotation(RenderProjectile projectile)
        {
            _workingEulerAngles = GetVisualsRotationDegrees(projectile).eulerAngles;
            CachedTransform.eulerAngles = _workingEulerAngles;
        }
    }
}

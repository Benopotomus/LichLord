using UnityEngine;
using DWD.Pooling;
using Cinemachine;

namespace LichLord.Projectiles
{
    public class ProjectileVisualEffect : VisualEffectBase
    {
        [SerializeField] protected Transform _visual;
        public Transform Visual => _visual;

        [SerializeField] protected bool _orientUp;
        [SerializeField] protected CinemachineImpulseSource _cameraShake;

        protected Vector3 _workingPosition = Vector3.zero;
        protected Vector3 _workingEulerAngles = Vector3.zero;
        protected Vector3 _workingScale = Vector3.one;

        public RenderProjectile Projectile { get; private set; }

        [SerializeField] string _renderProjectileInfo;

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
            float height = projectile.RenderHeight;
            //Debug.Log(height);
            // Set the shadow position
            _visualsSortingRoot.position = projectile.RenderPosition - new Vector2(0, height);
            _visual.position = projectile.RenderPosition;

        }

        protected virtual float GetVisualsRotationDegrees(RenderProjectile projectile)
        {
            return projectile.RenderRotation * Mathf.Rad2Deg;
        }

        protected virtual void UpdateVisualsRotation(RenderProjectile projectile)
        {
            _workingEulerAngles.z = GetVisualsRotationDegrees(projectile);
            _visual.eulerAngles = _workingEulerAngles;
        }
    }
}

using DWD.Pooling;
using UnityEngine;

namespace LichLord
{
    public class RockParticle : DWDObjectPoolObject
    {
        [SerializeField] private Rigidbody _rigidbody;
        private Transform _targetTransform;
        private bool isAttracted;
        private float attractionSpeed = 10f;
        private float attractionDelay;

        public void Initialize(Vector3 explosionForce, Transform targetTransform, float delay)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _targetTransform = targetTransform;
            attractionDelay = delay;
            isAttracted = false;
            _rigidbody.detectCollisions = true;
            // Apply initial explosion force for outward motion
            _rigidbody.AddForce(explosionForce, ForceMode.Impulse);

            // Schedule attraction to start after the specified delay
            Invoke(nameof(StartAttraction), attractionDelay);
            Invoke(nameof(RecycleDelay), attractionDelay + 0.25f);
        }

        private void RecycleDelay()
        {
            DWDObjectPool.Instance.Recycle(this);
        }

        private void StartAttraction()
        {
            isAttracted = true;
        }

        private void FixedUpdate()
        {
            if (isAttracted)
            {
                _rigidbody.detectCollisions = false;
                _rigidbody.useGravity = false;
                Vector3 targetPosition = _targetTransform.position + new Vector3(0f, 1f, 0f);

                _rigidbody.position = Vector3.Lerp(_rigidbody.position, targetPosition, Time.fixedDeltaTime * attractionSpeed);

                if (Vector3.SqrMagnitude(_rigidbody.position -  targetPosition) < 1f)
                {
                    _rigidbody.isKinematic = true;
                    isAttracted = false;
                    DWDObjectPool.Instance.Recycle(this);
   
                }
            }
        }
    }
}
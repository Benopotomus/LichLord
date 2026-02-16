using UnityEngine;

namespace LichLord
{
    public class PlayerCharacterIK : ContextBehaviour 
    {
        [SerializeField] private PlayerCharacter _pc;

        [Header("Animation Setup")]
        public Transform ChestTargetTransform;
        public Transform ChestBone;
        public Transform SpineBoneTop;
        public Transform SpineBoneBottom;
        public Transform CameraPivot;

        public Transform HeadTargetTransform;
        public Transform NeckBone;
        public Transform HeadBone;

        private float _lastPitch;

        /*
        private Animator _animator;

        [Header("Foot IK Setup")]
        [SerializeField] private Transform leftFootTarget; // Transform at the left foot's position
        [SerializeField] private Transform rightFootTarget; // Transform at the right foot's position
        [SerializeField] private LayerMask groundLayer; // Layer mask for ground detection
        [SerializeField] private float raycastHeightOffset = 0.5f; // Height above foot to start raycast
        [SerializeField] private float footIKWeight = 1f; // Weight for foot IK blending
        [SerializeField] private float footPositionLerpSpeed = 10f; // Speed for smoothing foot position

        private Vector3 _leftFootPosition;
        private Vector3 _rightFootPosition;
        private Quaternion _leftFootRotation;
        private Quaternion _rightFootRotation;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }
        */

        bool _isSpawned = false;
        public override void Spawned()
        {
            base.Spawned();
            _isSpawned = true;
        }

        private void LateUpdate()
        {
            if (!_isSpawned)
                return;

            // Existing upper body and head IK
            Quaternion pitchRotation = Quaternion.AngleAxis(_pc.Aim.PitchOffset, Vector3.right);
            Quaternion yawRotation = Quaternion.AngleAxis(_pc.Aim.YawOffset, Vector3.forward);
            Quaternion rollRotation = Quaternion.AngleAxis(_pc.Aim.RollOffset, Vector3.up);

            Quaternion offsetRotation = yawRotation * pitchRotation * rollRotation;
            Quaternion chestTargetRotation = ChestTargetTransform.rotation * offsetRotation;

            float upperBodyBlend = _pc.Aim.UpperBodyBlend;
            float blendAmount = HasStateAuthority ? 0.05f : 0.2f;

            var newPitch = HasStateAuthority ? _pc.Movement.WorldTransform.Pitch : Mathf.Lerp(_lastPitch, _pc.Movement.WorldTransform.Pitch, 10f * Time.deltaTime);

            CameraPivot.localRotation = Quaternion.Euler(newPitch, 0, 0);

            SpineBoneBottom.rotation = Quaternion.Lerp(SpineBoneBottom.rotation, chestTargetRotation, (upperBodyBlend * 0.25f));
            SpineBoneTop.rotation = Quaternion.Lerp(SpineBoneTop.rotation, chestTargetRotation, (upperBodyBlend * 0.5f));
            ChestBone.rotation = Quaternion.Lerp(ChestBone.rotation, chestTargetRotation, upperBodyBlend);
            NeckBone.rotation = Quaternion.Lerp(NeckBone.rotation, HeadTargetTransform.rotation, 0.5f);
            HeadBone.rotation = HeadTargetTransform.rotation;

            _lastPitch = newPitch;
        }

        /*
        private void OnAnimatorIK(int layerIndex)
        {
         
            Debug.Log("here" + layerIndex);
            // Foot IK
            UpdateFootIK(AvatarIKGoal.LeftFoot, leftFootTarget, ref _leftFootPosition, ref _leftFootRotation);
            UpdateFootIK(AvatarIKGoal.RightFoot, rightFootTarget, ref _rightFootPosition, ref _rightFootRotation);

            // Apply IK weights
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, footIKWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, footIKWeight);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, footIKWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, footIKWeight);

            // Set IK positions and rotations
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, _leftFootPosition);
            _animator.SetIKRotation(AvatarIKGoal.LeftFoot, _leftFootRotation);
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, _rightFootPosition);
            _animator.SetIKRotation(AvatarIKGoal.RightFoot, _rightFootRotation);
        }

        private void UpdateFootIK(AvatarIKGoal foot, Transform footTarget, ref Vector3 footPosition, ref Quaternion footRotation)
        {
            // Start raycast from above the foot
            Vector3 raycastOrigin = footTarget.position + Vector3.up * raycastHeightOffset;
            RaycastHit hit;

            if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, raycastHeightOffset + 0.5f, groundLayer))
            {
                // Smoothly interpolate to the new foot position
                Vector3 targetPosition = hit.point;
                footPosition = Vector3.Lerp(footPosition, targetPosition, footPositionLerpSpeed * Time.deltaTime);

                // Calculate rotation based on ground normal
                Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * footTarget.rotation;
                footRotation = Quaternion.Lerp(footRotation, targetRotation, footPositionLerpSpeed * Time.deltaTime);
            }
            else
            {
                // If no ground is hit, use the foot target's default position and rotation
                footPosition = Vector3.Lerp(footPosition, footTarget.position, footPositionLerpSpeed * Time.deltaTime);
                footRotation = Quaternion.Lerp(footRotation, footTarget.rotation, footPositionLerpSpeed * Time.deltaTime);
            }
        }
        */
    }
}
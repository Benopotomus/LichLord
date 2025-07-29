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

        float _lastPitch;

        private void LateUpdate()
        {
            Quaternion pitchRotation = Quaternion.AngleAxis(_pc.Aim.PitchOffset, Vector3.right);
            Quaternion yawRotation = Quaternion.AngleAxis(_pc.Aim.YawOffset, Vector3.forward);
            Quaternion rollRotation = Quaternion.AngleAxis(_pc.Aim.RollOffset, Vector3.up);

            // Apply yaw in world space, pitch in local chest space
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
    }
}
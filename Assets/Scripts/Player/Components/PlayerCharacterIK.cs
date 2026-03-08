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
        public Transform RightUpperArm;
        public Transform RightLowerArm;

        // ────────────────────────────────────────────────────────────────
        // Changed: now using Euler angles instead of raw Quaternion
        [Header("Forearm Offset (Euler Degrees)")]
        [SerializeField] private Vector3 _upperArmOffsetEuler = new Vector3(0f, 0f, 0);

        [SerializeField] private Vector3 _lowerArmOffsetEuler = new Vector3(0f, 0f, 0);

        [Range(0f, 1f)]
        [SerializeField] private float armOffsetWeight = 1f;

        private float _lastPitch;

        bool _isSpawned = false;

        public override void Spawned()
        {
            base.Spawned();
            _isSpawned = true;
        }
        private void LateUpdate()
        {
            if (!_isSpawned) return;

            // ────────────────────────────────────────────────────────────────
            // Existing upper body and head IK (unchanged)
            // ────────────────────────────────────────────────────────────────
            Quaternion pitchRotation = Quaternion.AngleAxis(_pc.Aim.PitchOffset, Vector3.right);
            Quaternion yawRotation = Quaternion.AngleAxis(_pc.Aim.YawOffset, Vector3.forward);
            Quaternion rollRotation = Quaternion.AngleAxis(_pc.Aim.RollOffset, Vector3.up);
            Quaternion offsetRotation = yawRotation * pitchRotation * rollRotation;

            Quaternion chestTargetRotation = ChestTargetTransform.rotation * offsetRotation;

            float upperBodyBlend = _pc.Aim.UpperBodyBlend;
            float blendAmount = HasStateAuthority ? 0.05f : 0.2f;

            var newPitch = HasStateAuthority
                ? _pc.Movement.WorldTransform.Pitch
                : Mathf.Lerp(_lastPitch, _pc.Movement.WorldTransform.Pitch, 10f * Time.deltaTime);

            CameraPivot.localRotation = Quaternion.Euler(newPitch, 0, 0);

            SpineBoneBottom.rotation = Quaternion.Lerp(SpineBoneBottom.rotation, chestTargetRotation, upperBodyBlend * 0.25f);
            SpineBoneTop.rotation = Quaternion.Lerp(SpineBoneTop.rotation, chestTargetRotation, upperBodyBlend * 0.5f);
            ChestBone.rotation = Quaternion.Lerp(ChestBone.rotation, chestTargetRotation, upperBodyBlend);

            NeckBone.rotation = Quaternion.Lerp(NeckBone.rotation, HeadTargetTransform.rotation, 0.5f);
            HeadBone.rotation = HeadTargetTransform.rotation;

            _lastPitch = newPitch;

            // ────────────────────────────────────────────────────────────────
            // Collect offsets
            // ────────────────────────────────────────────────────────────────
            Vector3 targetUpperEuler = Vector3.zero;
            Vector3 targetLowerEuler = Vector3.zero;

            var weaponDef = _pc.Weapons.GetWeaponRight();
            if (weaponDef != null)
            {
                targetUpperEuler = weaponDef.UpperArmOffsetEuler;
                targetLowerEuler = weaponDef.LowerArmOffsetEuler;

            }

            var maneuverDef = _pc.Maneuvers.GetActiveManeuver();
            if (maneuverDef != null)
            {
                var animationState = maneuverDef.GetUpperBodyAnimationTrigger(_pc);
                targetUpperEuler = animationState.UpperArmOffsetEuler;
                targetLowerEuler = animationState.LowerArmOffsetEuler;
            }

            _upperArmOffsetEuler = Vector3.Lerp(_upperArmOffsetEuler, targetUpperEuler, 5f * Time.deltaTime);
            _lowerArmOffsetEuler = Vector3.Lerp(_lowerArmOffsetEuler, targetLowerEuler, 5f * Time.deltaTime);


            // Apply weight (you can tie this to upperBodyBlend too if desired)
            armOffsetWeight = Mathf.Clamp01(1); // or keep as serialized + multiply

            // ────────────────────────────────────────────────────────────────
            // Apply additive offsets to bones (in local space)
            // ────────────────────────────────────────────────────────────────
            if (RightUpperArm != null && armOffsetWeight > 0f)
            {
                RightUpperArm.localRotation *= Quaternion.Euler(_upperArmOffsetEuler);
            }

            if (RightLowerArm != null && armOffsetWeight > 0f)
            {
                RightLowerArm.localRotation *= Quaternion.Euler(_lowerArmOffsetEuler);
            }
        }
    }

}
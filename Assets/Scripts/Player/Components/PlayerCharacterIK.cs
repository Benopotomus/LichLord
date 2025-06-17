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

        private int _animIDUpperBodyBlend = Animator.StringToHash("UpperBodyBlend");

        private void LateUpdate()
        {
            if (_pc.Health.IsAlive == false)
                return;

            float blendAmount = HasStateAuthority ? 0.05f : 0.2f;

            var pitchRotation = _pc.Movement.WorldTransform.Pitch;
            CameraPivot.localRotation = Quaternion.Euler(pitchRotation, 0, 0);

            SpineBoneBottom.rotation = Quaternion.Lerp(SpineBoneBottom.rotation, ChestTargetTransform.rotation, (_pc.Animator.GetFloat(_animIDUpperBodyBlend) * 0.25f));
            SpineBoneTop.rotation = Quaternion.Lerp(SpineBoneTop.rotation, ChestTargetTransform.rotation, (_pc.Animator.GetFloat(_animIDUpperBodyBlend) * 0.5f));

            ChestBone.rotation = Quaternion.Lerp(ChestBone.rotation, ChestTargetTransform.rotation, _pc.Animator.GetFloat(_animIDUpperBodyBlend));

            NeckBone.rotation = Quaternion.Lerp(NeckBone.rotation, HeadTargetTransform.rotation, 0.5f);
            HeadBone.rotation = HeadTargetTransform.rotation;

        }

    }
}
using UnityEngine;

namespace LichLord
{
    public class PlayerCharacterAimComponent : MonoBehaviour
    {
        [SerializeField] PlayerCharacter _pc;

        private float _pitchOffset = 0f;
        public float PitchOffset => _pitchOffset;

        private float _yawOffset = 0f;
        public float YawOffset => _yawOffset;

        private float _rollOffset = 0f;
        public float RollOffset => _rollOffset;

        private float _upperBodyBlend = 0f;
        public float UpperBodyBlend => _upperBodyBlend;

        public float TargetPitchOffset;
        public float TargetYawOffset;
        public float TargetRollOffset;

        [SerializeField]
        private float _rotationLerpSpeed = 5f;

        public void OnRender(float deltaTime)
        {
            _pitchOffset = Mathf.Lerp(_pitchOffset, TargetPitchOffset, _rotationLerpSpeed * deltaTime);
            _yawOffset = Mathf.Lerp(_yawOffset, TargetYawOffset, _rotationLerpSpeed * deltaTime);
            _rollOffset = Mathf.Lerp(_rollOffset, TargetRollOffset, _rotationLerpSpeed * deltaTime);

            UpdateUpperBodyBlend(deltaTime);
        }

        public void UpdateUpperBodyBlend(float deltaTime)
        {
            bool isUpperBody = false;

            if (_pc.FSM.StateMachine.ActiveState is SpellcastState)
            {
                var activeManeuver = _pc.Maneuvers.GetActiveManeuver();

                if (activeManeuver != null && !activeManeuver.Fullbody)
                {
                    isUpperBody = true;
                }
            }
            else if (_pc.FSM.StateMachine.ActiveState is InteractingState)
            {
                isUpperBody = true;
            }

            if (isUpperBody)
                _upperBodyBlend = Mathf.Lerp(_upperBodyBlend, 1f, _rotationLerpSpeed * deltaTime);
            else
                _upperBodyBlend = Mathf.Lerp(_upperBodyBlend, 0f, _rotationLerpSpeed * deltaTime);

            _pc.AnimationController.SetUpperBodyBlend(_upperBodyBlend);
        }
    }
}

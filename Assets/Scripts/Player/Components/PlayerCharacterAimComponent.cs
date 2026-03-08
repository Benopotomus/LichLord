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
        private float _rotationLerpSpeed = 8f;

        public void OnRender(float deltaTime)
        {
            UpdateUpperBodyBlend(deltaTime);

            float rotationSpeed = _rotationLerpSpeed;

            _pitchOffset = Mathf.Lerp(_pitchOffset, TargetPitchOffset, rotationSpeed * deltaTime);
            _yawOffset = Mathf.Lerp(_yawOffset, TargetYawOffset, rotationSpeed * deltaTime);
            _rollOffset = Mathf.Lerp(_rollOffset, TargetRollOffset, rotationSpeed * deltaTime);
        }

        public void UpdateUpperBodyBlend(float deltaTime)
        {
            bool isUpperBody = false;
            float rotationSpeed = _rotationLerpSpeed;

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
                _upperBodyBlend = Mathf.Lerp(_upperBodyBlend, 1f, rotationSpeed * deltaTime);
            else
                _upperBodyBlend = Mathf.Lerp(_upperBodyBlend, 0f, rotationSpeed * deltaTime);

            _pc.AnimationController.SetUpperBodyBlend(_upperBodyBlend);
        }
    }
}

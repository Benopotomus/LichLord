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

        private float _upperBodyBlend = 0f;
        public float UpperBodyBlend => _upperBodyBlend;

        public void OnRender(float deltaTime)
        {
            float maneuverPitch = _pc.Maneuvers.GetPitchOffset();
            float maneuverYaw = _pc.Maneuvers.GetYawOffset();

            _pitchOffset = Mathf.Lerp(_pitchOffset, maneuverPitch, 5 * deltaTime);

            _yawOffset = Mathf.Lerp(_yawOffset, maneuverYaw, 5 * deltaTime);

            UpdateUpperBodyBlend(deltaTime);
        }

        public void UpdateUpperBodyBlend(float deltaTime)
        {
            bool isUpperBody = false;

            var activeManeuver = _pc.Maneuvers.GetActiveManeuver();

            if (activeManeuver != null && !activeManeuver.Fullbody)
            { 
                isUpperBody = true;
            }

            if (isUpperBody)
                _upperBodyBlend = Mathf.Clamp01(_upperBodyBlend + (deltaTime * 8f));
            else
                _upperBodyBlend = Mathf.Clamp01(_upperBodyBlend - (deltaTime * 4f));

            _pc.AnimationController.SetUpperBodyBlend(_upperBodyBlend);
        }
    }
}

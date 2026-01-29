using UnityEngine;
using DWD.AnimationCurveAsset;
using System.Collections;
using TMPro;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UICommanderSquadFloatingTooltip : UIWidget
    {
        [SerializeField] protected RectTransform _rectTransform;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0, 1f, 0);
        [SerializeField] private AnimationCurveAsset _scaleDistanceCurve;
        [SerializeField] private TextMeshProUGUI _squadText;
        [SerializeField] protected Image _stanceIcon;

        [SerializeField] protected Sprite _attackIcon;
        [SerializeField] protected Sprite _defendIcon;

        private PlayerCharacter _pc;
        private Camera _camera;
        protected int _squadId;

        public void OnTrackerVisible(int squadId)
        {
            _squadId = squadId;
            _squadText.text = (squadId + 1).ToString();
            StartCoroutine(BindPlayerCharacter());
        }

        public void OnTrackerLateUpdate()
        {
            if (_camera == null) return;

            if (_pc == null) return;

            switch( _pc.Commander.GetStance(_squadId))
               {
                case ESquadStance.Attack:
                    _stanceIcon.sprite = _attackIcon;
                 break;
                    case ESquadStance.Defend:
                    _stanceIcon.sprite = _defendIcon;
                break;
            }

            UpdateScreenSpacePosition();
        }

        private void UpdateScreenSpacePosition()
        {
            if (_pc.Commander.IsCommandTargetValid(_squadId))
            {
                var squadTransform = _pc.Commander.GetCommandTransformForSquad(_squadId);
                Vector3 worldPos = squadTransform.Item1 + _worldOffset;
                Vector3 cameraSpacePos = _camera.WorldToViewportPoint(worldPos); // Use viewport for easier z-check

                // Check if the target is in front of the camera (z > 0)
                if (cameraSpacePos.z > 0)
                {
                    // Convert world position to screen point
                    Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);
                    _rectTransform.position = screenPos;
                    UpdateScreenSpaceScale(worldPos);
                }
            }
            else
            {
                _rectTransform.position = new Vector2(-200, -200); // Offscreen if no target
            }
        }

        private void UpdateScreenSpaceScale(Vector3 worldPos)
        {
            if (_scaleDistanceCurve == null) return;

            float sqrDist = Vector3.SqrMagnitude(_camera.transform.position - worldPos);
            float scale = _scaleDistanceCurve.Curve.Evaluate(sqrDist);
            _rectTransform.localScale = new Vector3(scale, scale, scale);
        }

        private IEnumerator BindPlayerCharacter()
        {
            if (Context.LocalPlayerCharacter == null)
                yield return null;

            _pc = Context.LocalPlayerCharacter;
            if (Camera.main == null)
                yield return null;

            _camera = Camera.main;
        }
    }
}

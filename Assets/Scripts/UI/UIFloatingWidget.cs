using DWD.AnimationCurveAsset;
using System;
using UnityEngine;

namespace LichLord.UI
{
    public class UIFloatingWidget : UIWidget
    {
        protected RectTransform _rectTransform;
        private Camera _camera;

        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0, 1f, 0);
        [SerializeField] private AnimationCurveAsset _scaleDistanceCurve;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void LateUpdate()
        {
            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null) return;

            UpdateScreenSpacePosition();
            UpdateScreenSpaceScale();
        }

        protected override void OnTick()
        {
            base.OnTick();


        }

        private void UpdateScreenSpaceScale()
        {
            if (_scaleDistanceCurve == null)
                return;

            float sqrDist = Vector3.SqrMagnitude(_camera.transform.position - (_target.position + _worldOffset));
            float scale = _scaleDistanceCurve.Curve.Evaluate(sqrDist);
            _rectTransform.localScale = new Vector3(scale, scale, scale);
        }

        private void UpdateScreenSpacePosition()
        {
            if (_target != null && _rectTransform != null)
            {


                Vector3 worldPos = _target.position + _worldOffset;
                Vector3 cameraSpacePos = _camera.WorldToViewportPoint(worldPos); // Use viewport for easier z-check

                // Check if the target is in front of the camera (z > 0)
                if (cameraSpacePos.z > 0)
                {
                    // Convert world position to screen point
                    Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);
                    _rectTransform.position = screenPos;
                }
                else
                {
                    // Option 1: Hide the UI element if behind the camera
                    _rectTransform.position = new Vector2(-200, -200); // Offscreen position

                    // Option 2: (Alternative) Clamp to screen edges
                    // Vector3 viewportPos = new Vector3(
                    //     Mathf.Clamp01(cameraSpacePos.x),
                    //     Mathf.Clamp01(cameraSpacePos.y),
                    //     0);
                    // _rectTransform.position = cam.ViewportToScreenPoint(viewportPos);
                }
            }
            else
            {
                _rectTransform.position = new Vector2(-200, -200); // Offscreen if no target
            }
        }

        public void SetTarget(Transform target)
        {
            _target = target;
        }


    }
}
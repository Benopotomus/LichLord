using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterAttitudeComponent : MonoBehaviour
    {
        [SerializeField] private EAttitude _attitude;

        [Header("Material Settings")]
        [SerializeField] private Renderer _indicatorRenderer;   // drag your plane's Renderer here
        [SerializeField] private string _colorProperty = "_Color"; // shader property name

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            //_indicatorRenderer.gameObject.SetActive(false);
            UpdateAttitudeChange(runtimeState);
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState)
        {
            UpdateAttitudeChange(runtimeState);
        }

        private void UpdateAttitudeChange(NonPlayerCharacterRuntimeState runtimeState)
        {
            EAttitude oldAttitude = _attitude;
            EAttitude newAttitude = runtimeState.GetAttitude();

            if (oldAttitude == newAttitude)
                return;

            _attitude = newAttitude;

            Color targetColor = Color.white;

            switch (_attitude)
            {
                case EAttitude.Defensive:
                    targetColor = Color.yellow;
                    break;
                case EAttitude.Passive:
                    targetColor = Color.green;
                    break;
                case EAttitude.Hostile:
                    targetColor = Color.red;
                    break;
            }

            if (_indicatorRenderer != null)
            {
                // Get a unique instance of the material so we don't overwrite sharedMaterial
                _indicatorRenderer.material.SetColor(_colorProperty, targetColor);
            }
        }
    }
}

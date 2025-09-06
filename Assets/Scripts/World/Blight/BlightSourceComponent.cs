using UnityEngine;

namespace LichLord.World
{
    public class BlightSourceComponent : MonoBehaviour
    {
        public System.Action<BlightSourceComponent> OnBlightSourceChanged;

        [Header("Blight Settings")]
        [SerializeField]
        private float _blightRadius;
        public float BlightRadius
        {
            get { return _blightRadius; }
            set 
            {
                if (_blightRadius != value)
                {
                    _blightRadius = value;
                    if (_blightRadius >= 0)
                    {
                        if (OnBlightSourceChanged != null)
                            OnBlightSourceChanged(this);
                    }
                }
            }
        }

        private Vector3 _position;

        public Vector4 ShaderData
        {
            get 
            { 
                _position = transform.position;
                return new Vector4(_position.x, _position.y, _position.z, Max(_blightRadius, 0.0f)); 
            }
        }

        private float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        private void OnEnable()
        {
            BlightManager.Instance.RegisterSource(this);
        }

        private void OnDisable()
        {
            BlightManager.Instance.UnregisterSource(this);
        }
    }
}
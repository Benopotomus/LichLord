using DWD.DOTweenSequencer;
using DWD.Pooling;
using UnityEngine;

namespace LichLord.Props
{
    public class PropStateComponent : MonoBehaviour
    {
        [SerializeField] private Prop _prop;
        public Prop Prop => _prop;

        [SerializeField] private EPropState _currentState = EPropState.Inactive;
        public EPropState CurrentState => _currentState;

        [SerializeField]
        private VisualEffectBase _hitReactPrefab;

        public void UpdateState(EPropState newState)
        {
            if (_currentState == newState)
                return;

            switch (newState)
            {
                case EPropState.Inactive:
                case EPropState.Destroyed:
                    gameObject.SetActive(false);
                    break;
                case EPropState.Idle:
                    gameObject.SetActive(true);
                    break;
                case EPropState.HitReact:
                    var effectInstance = DWDObjectPool.Instance.SpawnAt(_hitReactPrefab, 
                        Prop.CachedTransform.position, Prop.CachedTransform.rotation) as VisualEffectBase;
                    effectInstance.Initialize();
                    break;
            }

            _currentState = newState;
        }
    }
}

namespace LichLord
{
    using UnityEngine;

    public class HurtboxOwner : MonoBehaviour
    {
        [SerializeField] private GameObject _owner; // Assign the root object here
        public GameObject Owner => _owner;

        private IHitTarget _hitTarget;
        public IHitTarget HitTarget => _hitTarget;

        private void Awake()
        {
            _hitTarget = _owner.GetComponent<IHitTarget>();
        }
    }
}

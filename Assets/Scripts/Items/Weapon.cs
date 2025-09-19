using UnityEngine;

namespace LichLord.Items
{
    public class Weapon : Item
    {
        [SerializeField]
        private Transform _muzzle;
        public Transform Muzzle => _muzzle;

        [SerializeField]
        private Vector3 _localOffset;
        public Vector3 LocalOffset => _localOffset;

        [SerializeField]
        private Quaternion _localRotation;
        public Quaternion LocalRotation => _localRotation;

        public void OnSpawned()
        { 
            
        }
    }
}

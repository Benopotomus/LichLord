using AYellowpaper.SerializedCollections;
using LichLord.Projectiles;
using UnityEngine;

namespace LichLord.Items
{
    public class Weapon : Item
    {
        [SerializeField]
        [SerializedDictionary("MuzzleType", "Transform")]
        private SerializedDictionary<EMuzzle, Transform> _sockets;

        [SerializeField]
        private Vector3 _localOffset;
        public Vector3 LocalOffset => _localOffset;

        [SerializeField]
        private Quaternion _localRotation;
        public Quaternion LocalRotation => _localRotation;

        public void OnSpawned()
        { 
            
        }

        public Transform GetMuzzleTransform(EMuzzle muzzle)
        { 
            if(_sockets.TryGetValue(muzzle, out Transform t))
                return t;

            return null;
        }
    }
}

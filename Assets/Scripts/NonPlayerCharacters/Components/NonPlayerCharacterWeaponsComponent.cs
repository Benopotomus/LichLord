using LichLord.Items;
using LichLord.Projectiles;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{   
    public class NonPlayerCharacterWeaponsComponent : MonoBehaviour
    {
        [SerializeField]
        private int _weaponIndex;

        [SerializeField]
        private Weapon _weaponLeft;

        [SerializeField]
        private Weapon _weaponRight;

        [SerializeField]
        private Transform _handBoneLeft;

        [SerializeField]
        private Transform _handBoneRight;

        public void DropWeapons()
        { 
        
        }

        public int GetWeaponID()
        {
            return _weaponIndex;
        }

        public Vector3 GetMuzzlePosition(EMuzzle muzzleName)
        {
            switch (muzzleName)
            {
                case EMuzzle.LeftHand:
                    if (_weaponLeft != null)
                        return _weaponLeft.Muzzle.position;

                    return _handBoneLeft.position;

                case EMuzzle.RightHand:
                    if (_weaponRight != null)
                        return _weaponRight.Muzzle.position;

                    return _handBoneRight.position;

                case EMuzzle.LeftHand_RightHand_Blend:
                    Vector3 leftPos = _weaponLeft != null ?
                        _weaponLeft.Muzzle.position : _handBoneLeft.position;
                    Vector3 rightPos = _weaponRight != null ?
                        _weaponRight.Muzzle.position : _handBoneRight.position;

                    return Vector3.Lerp(leftPos, rightPos, 0.5f);
            }

            return transform.position;
        }

    }
}

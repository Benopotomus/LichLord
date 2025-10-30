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
                case EMuzzle.Left_WeaponSocket_1:
                case EMuzzle.Left_WeaponSocket_2:
                case EMuzzle.Left_WeaponSocket_3:
                    if (_weaponLeft != null)
                        return _weaponLeft.GetMuzzleTransform(muzzleName).position;

                    return _handBoneLeft.position;

                case EMuzzle.RightHand:
                case EMuzzle.Right_WeaponSocket_1:
                case EMuzzle.Right_WeaponSocket_2: 
                case EMuzzle.Right_WeaponSocket_3:
                    if (_weaponRight != null)
                        return _weaponRight.GetMuzzleTransform(muzzleName).position;

                    return _handBoneRight.position;

                case EMuzzle.LeftHand_RightHand_Blend:
                    Vector3 leftPos = _weaponLeft != null ?
                        _weaponLeft.GetMuzzleTransform(EMuzzle.LeftHand).position : _handBoneLeft.position;
                    Vector3 rightPos = _weaponRight != null ?
                        _weaponRight.GetMuzzleTransform(EMuzzle.RightHand).position : _handBoneRight.position;

                    return Vector3.Lerp(leftPos, rightPos, 0.5f);
            }

            return transform.position;
        }

    }
}

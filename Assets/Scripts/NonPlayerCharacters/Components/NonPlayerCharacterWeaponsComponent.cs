using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterWeaponsComponent : MonoBehaviour
    {
        [SerializeField]
        private int _weaponIndex;

        [SerializeField]
        private GameObject _leftHandItem;

        [SerializeField]
        private GameObject _rightHandItem;


        [SerializeField]
        private Transform _leftHandBone;

        [SerializeField]
        private Transform _rightHandBone;

        public void DropWeapons()
        { 
        
        }

        public int GetWeaponID()
        {
            return _weaponIndex;

        }

    }
}

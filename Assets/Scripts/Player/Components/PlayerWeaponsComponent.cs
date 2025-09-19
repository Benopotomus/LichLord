using DWD.Pooling;
using Fusion;
using LichLord.Items;
using LichLord.Projectiles;
using System;
using UnityEngine;

namespace LichLord
{
    public class PlayerWeaponsComponent : ContextBehaviour
    {
        private ItemSpawner _itemSpawnerLeft = new ItemSpawner();
        private ItemSpawner _itemSpawnerRight = new ItemSpawner();

        [Networked]
        private ref FEquippableItem _itemDataLeft => ref MakeRef<FEquippableItem>();

        [Networked]
        private ref FEquippableItem _itemDataRight => ref MakeRef<FEquippableItem>();

        [SerializeField]
        private int _weaponIndex;
        
        [SerializeField]
        private FWeaponLoadState _weaponLeft = new FWeaponLoadState();

        [SerializeField]
        private FWeaponLoadState _weaponRight = new FWeaponLoadState();

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

        public override void Spawned()
        {
            base.Spawned();

            _itemDataRight.DefinitionID = 1;
        }

        public void OnRender(float deltaTime)
        {
            if (_itemDataLeft.IsValid() && _weaponLeft.LoadState == ELoadState.None)
            {
                ItemDefinition item = Global.Tables.ItemTable.TryGetDefinition(_itemDataLeft.DefinitionID);
                _itemSpawnerLeft.OnLoadedAttached += OnItemLoadedLeft;
                _itemSpawnerLeft.SpawnWeaponAttached(_handBoneLeft, Quaternion.identity, item.Model);
                _weaponLeft.LoadState = ELoadState.Loading;
            }

            if (_itemDataRight.IsValid() && _weaponRight.LoadState == ELoadState.None)
            {
                ItemDefinition item = Global.Tables.ItemTable.TryGetDefinition(_itemDataRight.DefinitionID);
                _itemSpawnerRight.OnLoadedAttached += OnItemLoadedRight;
                _itemSpawnerRight.SpawnWeaponAttached(_handBoneRight, Quaternion.identity, item.Model);
                _weaponRight.LoadState = ELoadState.Loading;
            }
        }

        private void OnItemLoadedRight(GameObject gameobject, Transform attachment, Quaternion rotation)
        {
            _itemSpawnerRight.OnLoadedAttached -= OnItemLoadedRight;
            _weaponRight.Weapon = OnItemLoaded(gameobject, attachment, rotation);
            _weaponRight.LoadState = ELoadState.Loaded;
        }

        private void OnItemLoadedLeft(GameObject gameobject, Transform attachment, Quaternion rotation)
        {
            _itemSpawnerLeft.OnLoadedAttached -= OnItemLoadedLeft;
            _weaponLeft.Weapon = OnItemLoaded(gameobject, attachment, rotation);
            _weaponLeft.LoadState = ELoadState.Loaded;
        }

        private Weapon OnItemLoaded(GameObject loadedGameObject, Transform attachment, Quaternion rotation)
        {
            var poolObject = loadedGameObject.GetComponent<DWDObjectPoolObject>();

            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn Visuals Prefab for Impact");
                return null;
            }

            var instance = DWDObjectPool.Instance.SpawnAttached(poolObject, attachment.position, attachment.rotation, attachment);

            if (instance is Weapon weapon)
            {
                instance.transform.localPosition = weapon.LocalOffset;
                instance.transform.localRotation = weapon.LocalRotation;

                return weapon;
            }

            return null;
        }

        public Vector3 GetMuzzlePosition(EMuzzle muzzleName)
        {
            switch (muzzleName)
            {
                case EMuzzle.LeftHand:
                    if (_weaponLeft.LoadState == ELoadState.Loaded)
                        return _weaponLeft.Weapon.Muzzle.position;

                    return _handBoneLeft.position;

                case EMuzzle.RightHand:
                    if (_weaponRight.LoadState == ELoadState.Loaded)
                        return _weaponRight.Weapon.Muzzle.position;

                    return _handBoneRight.position;

                case EMuzzle.LeftHand_RightHand_Blend:
                    Vector3 leftPos = _weaponLeft.LoadState == ELoadState.Loaded ? 
                        _weaponLeft.Weapon.Muzzle.position : _handBoneLeft.position;
                    Vector3 rightPos = _weaponRight.LoadState == ELoadState.Loaded ? 
                        _weaponRight.Weapon.Muzzle.position : _handBoneRight.position;

                    return Vector3.Lerp(leftPos, rightPos, 0.5f);
            }

            return transform.position;
        }

        [Serializable]
        private struct FWeaponLoadState
        { 
            public ELoadState LoadState;
            public Weapon Weapon;
        }
    }


}

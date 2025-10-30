using DWD.Pooling;
using Fusion;
using LichLord.Items;
using LichLord.Projectiles;
using System;
using UnityEngine;
using DG.Tweening;

namespace LichLord
{
    public class PlayerWeaponsComponent : ContextBehaviour
    {
        [SerializeField]
        private PlayerCharacter _pc;

        private ItemSpawner _itemSpawnerLeft = new ItemSpawner();
        private ItemSpawner _itemSpawnerRight = new ItemSpawner();

        [Networked]
        private ref FItemData _itemDataLeft => ref MakeRef<FItemData>();
        private int _itemDefinitionLeft = -1;

        [Networked]
        private ref FItemData _itemDataRight => ref MakeRef<FItemData>();
        private int _itemDefinitionRight = -1;

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

        [SerializeField]
        private float _weaponScaleSpeed = 0.3f;

        [SerializeField]
        private ELoadoutSlot _weaponSlotLeft;

        [SerializeField]
        private ELoadoutSlot _weaponSlotRight;

        public void DropWeapons()
        {

        }

        public int GetWeaponID()
        {
            if (_weaponRight.LoadState == ELoadState.Loaded)
            { 
                return _weaponRight.WeaponDefinition.IdleAnimationID;
            }

            return 0;
        }

        public void WeaponSwitch()
        {
            if (!HasStateAuthority)
                return;

            switch (_weaponSlotLeft)
            {
                case ELoadoutSlot.Weapon_00_Left:
                    _weaponSlotLeft = ELoadoutSlot.Weapon_01_Left;
                    _weaponSlotRight = ELoadoutSlot.Weapon_01_Right;
                    break;
                case ELoadoutSlot.Weapon_01_Left:
                    _weaponSlotLeft = ELoadoutSlot.Weapon_02_Left;
                    _weaponSlotRight = ELoadoutSlot.Weapon_02_Right;
                    break;
                case ELoadoutSlot.Weapon_02_Left:
                    _weaponSlotLeft = ELoadoutSlot.Weapon_00_Left;
                    _weaponSlotRight = ELoadoutSlot.Weapon_00_Right;
                    break;
            }
            // Increment DefinitionID and cycle back to 0 when reaching 3 (max is 2)
           // _itemDataRight.DefinitionID = (_itemDataRight.DefinitionID + 1) % 3;
        }

        public override void Spawned()
        {
            base.Spawned();
            _weaponSlotLeft = ELoadoutSlot.Weapon_00_Left;
            _weaponSlotRight = ELoadoutSlot.Weapon_00_Right;
        }

        public void OnRender(float deltaTime)
        {
            if (HasStateAuthority)
            {
                _itemDataLeft = _pc.Inventory.GetItemAtLoadoutSlot(_weaponSlotLeft);
                _itemDataRight = _pc.Inventory.GetItemAtLoadoutSlot(_weaponSlotRight);
            }

            // Handle left hand weapon
            if (_itemDataLeft.IsValid())
            {
                int newDefinitionId = _itemDataLeft.DefinitionID;
                if (_weaponLeft.LoadState == ELoadState.None || _itemDefinitionLeft != newDefinitionId)
                {
                    // Unload current weapon if loaded and definition changed
                    if (_weaponLeft.LoadState == ELoadState.Loaded && _itemDefinitionLeft != newDefinitionId)
                    {
                        _weaponLeft.Weapon.StartRecycle();
                        _weaponLeft.LoadState = ELoadState.None;
                    }

                    ItemDefinition item = Global.Tables.ItemTable.TryGetDefinition(newDefinitionId);

                    _itemSpawnerLeft.OnLoadedAttached += OnItemLoadedLeft;
                    _weaponLeft.LoadState = ELoadState.Loading;
                    _weaponLeft.WeaponDefinition = item as WeaponDefinition;
                    _itemSpawnerLeft.SpawnItemAttached(_handBoneLeft, Quaternion.identity, item.Model);
                    _itemDefinitionLeft = newDefinitionId; // Update stored definition
                }
            }
            else
            {
                if (_weaponLeft.LoadState == ELoadState.Loaded)
                {
                    _weaponLeft.Weapon.StartRecycle();
                    _weaponLeft.LoadState = ELoadState.None;
                    _itemDefinitionLeft = -1; // Reset stored definition when no item
                }
            }

            // Handle right hand weapon
            if (_itemDataRight.IsValid())
            {
                int newDefinitionId = _itemDataRight.DefinitionID;
                if (_weaponRight.LoadState == ELoadState.None || _itemDefinitionRight != newDefinitionId)
                {
                    // Unload current weapon if loaded and definition changed
                    if (_weaponRight.LoadState == ELoadState.Loaded && _itemDefinitionRight != newDefinitionId)
                    {
                        _weaponRight.Weapon.StartRecycle();
                        _weaponRight.LoadState = ELoadState.None;
                    }

                    ItemDefinition item = Global.Tables.ItemTable.TryGetDefinition(newDefinitionId);

                    _itemSpawnerRight.OnLoadedAttached += OnItemLoadedRight;
                    _weaponRight.LoadState = ELoadState.Loading;
                    _weaponRight.WeaponDefinition = item as WeaponDefinition;
                    if (item != null)
                    {
                        _itemSpawnerRight.SpawnItemAttached(_handBoneRight, Quaternion.identity, item.Model);
                        _itemDefinitionRight = newDefinitionId; // Update stored definition
                    }
                }
            }
            else
            {
                if (_weaponRight.LoadState == ELoadState.Loaded)
                {
                    _weaponRight.Weapon.StartRecycle();
                    _weaponRight.LoadState = ELoadState.None;
                    _itemDefinitionRight = -1; // Reset stored definition when no item
                }
            }
        }

        private void OnItemLoadedLeft(GameObject gameobject, Transform attachment, Quaternion rotation)
        {
            _itemSpawnerLeft.OnLoadedAttached -= OnItemLoadedLeft;
            _weaponLeft.Weapon = OnItemLoaded(gameobject, attachment, rotation);
            _weaponLeft.LoadState = ELoadState.Loaded;

            // Apply DOTween scale animation
            if (_weaponLeft.Weapon != null)
            {
                _weaponLeft.Weapon.transform.localScale = Vector3.zero; 
                _weaponLeft.Weapon.transform.DOScale(Vector3.one, _weaponScaleSpeed) 
                    .SetEase(Ease.Linear) 
                    .SetUpdate(true);
            }
        }

        private void OnItemLoadedRight(GameObject gameobject, Transform attachment, Quaternion rotation)
        {
            _itemSpawnerRight.OnLoadedAttached -= OnItemLoadedRight;
            _weaponRight.Weapon = OnItemLoaded(gameobject, attachment, rotation);
            _weaponRight.LoadState = ELoadState.Loaded;

            // Apply DOTween scale animation
            if (_weaponRight.Weapon != null)
            {
                _weaponRight.Weapon.transform.localScale = Vector3.zero; 
                _weaponRight.Weapon.transform.DOScale(Vector3.one, _weaponScaleSpeed) 
                    .SetEase(Ease.Linear) 
                    .SetUpdate(true); 
            }
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

        public void ScaleDownWeapons()
        {
            // Apply DOTween scale animation
            if (_weaponLeft.Weapon != null)
            {
                _weaponLeft.Weapon.transform.localScale = Vector3.one;
                _weaponLeft.Weapon.transform.DOScale(Vector3.zero, _weaponScaleSpeed)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
            }

            // Apply DOTween scale animation
            if (_weaponRight.Weapon != null)
            {
                _weaponRight.Weapon.transform.localScale = Vector3.one;
                _weaponRight.Weapon.transform.DOScale(Vector3.zero, _weaponScaleSpeed)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
            }
        }

        public void OnHitFromAnimation()
        {
            Vector3 position = _weaponRight.Weapon.GetMuzzleTransform(EMuzzle.RightHand).position;
            var definition = _weaponRight.WeaponDefinition;

            var tick = Runner.Tick;

            Collider[] collidersPool = new Collider[6];
            
            int hitCount = Physics.OverlapSphereNonAlloc(
                position,
                1f,
                collidersPool,
                definition.OverlapCollisionLayer);


            foreach (var collider in collidersPool)
            {
                if(collider == null) 
                    continue;

                var gameObjectHit = collider.gameObject;

                if (gameObjectHit.tag == "Hurtbox")
                {
                    HurtboxOwner hitboxOwnerComp = gameObjectHit.GetComponent<HurtboxOwner>();
                    if (hitboxOwnerComp == null)
                        continue;

                    var hitTarget = hitboxOwnerComp.HitTarget;

                    if (!IsImpactObjectValid(gameObjectHit, hitTarget))
                        continue;

                    ApplyHitToTarget(hitTarget, tick);
                }
            }
        }

        public static bool IsImpactObjectValid(GameObject hitObject, IHitTarget hitTarget)
        {
            if (hitTarget is PlayerCharacter)
                return false;

            return true;
        }

        public void ApplyHitToTarget(IHitTarget hitTarget, int tick)
        {
            FDamageData damageData = new FDamageData();
            damageData.damageValue = _weaponRight.WeaponDefinition.Damage;

            FHitUtilityData hit = new FHitUtilityData
            {
                instigator = _pc,
                target = hitTarget,
                damageData = damageData,
                staggerRating = 0,
                knockbackStrength = 0,
                impactRotation = Quaternion.identity,
                impactPosition = Vector3.zero,
                tick = tick,
            };

            HitUtility.ProcessHit(ref hit, Context);
        }

        public Vector3 GetMuzzlePosition(EMuzzle muzzleName)
        {
            switch (muzzleName)
            {
                case EMuzzle.LeftHand:
                case EMuzzle.Left_WeaponSocket_1:
                case EMuzzle.Left_WeaponSocket_2:
                case EMuzzle.Left_WeaponSocket_3:
                    if (_weaponLeft.LoadState == ELoadState.Loaded)
                        return _weaponLeft.Weapon.GetMuzzleTransform(muzzleName).position;

                    return _handBoneLeft.position;

                case EMuzzle.RightHand:
                case EMuzzle.Right_WeaponSocket_1:
                case EMuzzle.Right_WeaponSocket_2:
                case EMuzzle.Right_WeaponSocket_3:
                    if (_weaponRight.LoadState == ELoadState.Loaded)
                        return _weaponRight.Weapon.GetMuzzleTransform(muzzleName).position;

                    return _handBoneRight.position;

                case EMuzzle.LeftHand_RightHand_Blend:
                    Vector3 leftPos = _weaponLeft.LoadState == ELoadState.Loaded ?
                        _weaponLeft.Weapon.GetMuzzleTransform(EMuzzle.LeftHand).position : _handBoneLeft.position;
                    Vector3 rightPos = _weaponRight.LoadState == ELoadState.Loaded ?
                        _weaponRight.Weapon.GetMuzzleTransform(EMuzzle.RightHand).position : _handBoneRight.position;

                    return Vector3.Lerp(leftPos, rightPos, 0.5f);
            }

            return transform.position;
        }

        [Serializable]
        private struct FWeaponLoadState
        { 
            public ELoadState LoadState;
            public Weapon Weapon;
            public WeaponDefinition WeaponDefinition;
        }
    }


}

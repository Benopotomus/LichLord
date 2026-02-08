using DWD.Pooling;
using DWD.Utility.Loading;
using Fusion;
using System;
using UnityEngine;

namespace LichLord
{
    public class PlayerStatsComponent : ContextBehaviour
    {
        [SerializeField]
        private PlayerCharacter _pc;

        // Health
        [Networked]
        private ref FPlayerStatData _maxHealth => ref MakeRef<FPlayerStatData>();
        public int MaxHealth => _maxHealth.GetValueAsInt();

        [Networked]
        private ref FPlayerStatData _currentHealth => ref MakeRef<FPlayerStatData>();
        public int CurrentHealth => _currentHealth.GetValueAsInt();

        public float HealthPercent { get { return Mathf.Clamp01((float)CurrentHealth / (float)MaxHealth); } }

        // Mana
        [Networked]
        private ref FPlayerStatData _maxMana => ref MakeRef<FPlayerStatData>();
        public int MaxMana => _maxMana.GetValueAsInt();

        [Networked]
        private ref FPlayerStatData _currentMana => ref MakeRef<FPlayerStatData>();
        public int CurrentMana => _currentMana.GetValueAsInt();

        public float ManaPercent { get { return Mathf.Clamp01((float)CurrentMana / (float)MaxMana); } }

        [BundleObject(typeof(GameObject))]
        [SerializeField]
        private BundleObject _hitEffect;
        public BundleObject HitEffect => _hitEffect;

        [SerializeField]
        private Transform _impactAttachment;

        private VisualEffectSpawner _visualSpawner = new VisualEffectSpawner();

        public Action<EStatName> OnStatChanged;

        public override void Spawned()
        {
            _visualSpawner.OnLoadedAttached += OnVisualsPrefabLoadedAttached;

            _maxHealth.SetValueAsInt(500);
            _currentHealth.SetValueAsInt(500);
            _maxMana.SetValueAsInt(1000);
            _currentMana.SetValueAsInt(1000);
        }

        public void ApplyDamage(int damage)
        { 
            _currentHealth.SetValueAsInt(Mathf.Clamp(CurrentHealth - damage, 0, MaxHealth));
            // Debug.Log("Damage Taken: " + damage + ", Health: " + _currentHealth);


            OnStatChanged(EStatName.HealthCurrent);

            SpawnImpactVisualEffect(0);

            if (CurrentHealth == 0)
            {
              //  Debug.Log("Player Died");
            }

            _pc.AnimationController.PlayFlinchAnimation();

            if (HasStateAuthority)
            {
                Context.Camera.Shake(ECameraShakeType.Damage);
            }



        }

        public void SpawnImpactVisualEffect(int animIndex)
        {

            if (HitEffect.Name != "")
                _visualSpawner.SpawnVisualEffectAttached(_impactAttachment, _impactAttachment.rotation, HitEffect);
        }

        private void OnVisualsPrefabLoadedAttached(GameObject loadedGameObject, Transform attachment, Quaternion rotation)
        {
            var poolObject = loadedGameObject.GetComponent<DWDObjectPoolObject>();

            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn Visuals Prefab for Impact");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAttached(poolObject, attachment.position, attachment.rotation, attachment);
            if (instance is StandaloneVisualEffect standaloneEffect)
                standaloneEffect.Initialize();
        }

        private void OnDestroy()
        {
            _visualSpawner.OnLoadedAttached -= OnVisualsPrefabLoadedAttached;
        }

    }

    public enum EStatName
    { 
        HealthMax,
        HealthCurrent,
        ManaMax,
        ManaCurrent,

    }
}

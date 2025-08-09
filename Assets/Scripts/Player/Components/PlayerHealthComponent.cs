using DWD.Pooling;
using DWD.Utility.Loading;
using Fusion;

using UnityEngine;

namespace LichLord
{
    public class PlayerHealthComponent : ContextBehaviour
    {
        [SerializeField]
        private PlayerCharacter _pc;

        [Networked]
        private int _currentHealth { get; set; } = 1000;
        public int CurrentHealth => _currentHealth;

        [Networked]
        private int _maxHealth { get; set; } = 1000;
        public int MaxHealth => _maxHealth;

        public float HealthPercent { get { return Mathf.Clamp01((float)CurrentHealth / (float)MaxHealth); } }

        [BundleObject(typeof(GameObject))]
        [SerializeField]
        private BundleObject _hitEffect;
        public BundleObject HitEffect => _hitEffect;

        [SerializeField]
        private Transform _impactAttachment;

        private ImpactSpawner _visualSpawner = new ImpactSpawner();

        private void Start()
        {
            _visualSpawner.OnImpactSpawnedAttached += OnVisualsPrefabLoadedAttached;
        }

        public void ApplyDamage(int damage)
        { 
            _currentHealth = Mathf.Clamp(_currentHealth - damage, 0, _maxHealth);
            Debug.Log("Damage Taken: " + damage + ", Health: " + _currentHealth);

            SpawnImpactVisualEffect(0);

            if (_currentHealth == 0)
            {
                Debug.Log("Player Died");
            }

            _pc.AnimationController.PlayFlinchAnimation();

        }

        public void SpawnImpactVisualEffect(int animIndex)
        {

            if (HitEffect.Name != "")
                _visualSpawner.SpawnImpactVisualAttached(_impactAttachment, _impactAttachment.rotation, HitEffect);
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
            _visualSpawner.OnImpactSpawnedAttached -= OnVisualsPrefabLoadedAttached;
        }

    }
}

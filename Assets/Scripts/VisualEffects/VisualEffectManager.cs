
using DWD.Pooling;
using DWD.Utility.Loading;
using Fusion;
using UnityEngine;

namespace LichLord
{
    public class VisualEffectManager : ContextBehaviour
    {
        private VisualEffectSpawner _effectSpawner = new VisualEffectSpawner();

        public override void Spawned()
        {
            base.Spawned();

            _effectSpawner.OnLoaded += OnVisualEffectLoaded;
            _effectSpawner.OnLoadedAttached += OnVisualEffectLoadedAttached;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _effectSpawner.OnLoaded -= OnVisualEffectLoaded;
            _effectSpawner.OnLoadedAttached -= OnVisualEffectLoadedAttached;
            base.Despawned(runner, hasState);
        }

        public void SpawnVisualEffect(Vector3 position, Quaternion rotation, BundleObject vfxBundle)
        {
            if (vfxBundle.Name == "")
                return;

            _effectSpawner.SpawnVisualEffect(position, rotation, vfxBundle);
        }

        public void SpawnVisualEffectAttached(Transform transform, Quaternion rotation, BundleObject vfxBundle)
        {
            if (vfxBundle.Name == "")
                return;

            _effectSpawner.SpawnVisualEffectAttached(transform, rotation, vfxBundle);
        }

        private void OnVisualEffectLoaded(GameObject loadedGameObject, Vector3 position, Quaternion rotation)
        {
            var poolObject = loadedGameObject.GetComponent<DWDObjectPoolObject>();

            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn Visuals Prefab");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAt(poolObject, position, rotation);
            if (instance is StandaloneVisualEffect standaloneEffect)
                standaloneEffect.Initialize();
        }

        private void OnVisualEffectLoadedAttached(GameObject loadedGameObject, Transform attachment, Quaternion rotation)
        {
            var poolObject = loadedGameObject.GetComponent<DWDObjectPoolObject>();

            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn Visuals Prefab attached");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAttached(poolObject, attachment.position, rotation, attachment);
            if (instance is StandaloneVisualEffect standaloneEffect)
                standaloneEffect.Initialize();
        }
    }
}

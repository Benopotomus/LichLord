
using DWD.Pooling;
using DWD.Utility.Loading;
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
        }

        public void SpawnVisualEffect(Vector3 position, Quaternion rotation, BundleObject vfxBundle)
        {
            if (vfxBundle.Name == "")
                return;

            _effectSpawner.SpawnVisualEffect(position, rotation, vfxBundle);
        }

        private void OnVisualEffectLoaded(GameObject loadedGameObject, Vector3 position, Quaternion rotation)
        {
            var poolObject = loadedGameObject.GetComponent<DWDObjectPoolObject>();

            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn Visuals Prefab for Impact");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAt(poolObject, position, rotation);
            if (instance is StandaloneVisualEffect standaloneEffect)
                standaloneEffect.Initialize();
        }
    }
}

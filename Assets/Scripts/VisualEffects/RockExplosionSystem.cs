using DWD.Pooling;
using UnityEngine;

namespace LichLord
{
    public class RockExplosionSystem : DWDObjectPoolObject
    {
        [SerializeField] private VisualEffectBase _visualEffectBase;

        public Transform player; // Assign the player GameObject in the Unity Editor
        public float explosionForce = 7f;
        public float spawnRadius = 0.5f;
        public float attractionDelayMin = 2f; // Minimum delay before attraction
        public float attractionDelayMax = 4f; // Maximum delay before attraction
        public float coneAngle = 22.5f; // Half-angle of the cone (45-degree total spread)

        public float scaleMin = 0.1f;
        public float scaleMax = 0.3f;

        public RockParticle rockParticle;

        private void Awake()
        {
            if (_visualEffectBase != null)
            {
                _visualEffectBase.onInitialized += OnInitialized;
            }
        }

        private void OnInitialized(VisualEffectBase gameplayEffectVisual)
        {
            Vector3 explosionPoint = transform.position;

            for (int i = 0; i < 20; i++)
            {
                // Generate random spherical coordinates for spawn position
                float distance = Random.Range(0f, spawnRadius);
                float theta = Random.Range(0f, 2f * Mathf.PI); // Random angle around sphere
                float phi = Random.Range(0f, Mathf.PI); // Random angle from vertical

                // Calculate spawn position in sphere
                float x = distance * Mathf.Sin(phi) * Mathf.Cos(theta);
                float y = distance * Mathf.Cos(phi);
                float z = distance * Mathf.Sin(phi) * Mathf.Sin(theta);

                Vector3 spawnOffset = new Vector3(x, y, z);
                Vector3 spawnPos = explosionPoint + spawnOffset;
                RockParticle particle = DWDObjectPool.Instance.SpawnAt(rockParticle, spawnPos, Quaternion.identity) as RockParticle;

                // Generate force direction within cone (up to 45 degrees from vertical)
                float angle = Random.Range(0f, coneAngle);
                float azimuth = Random.Range(0f, 360f); // Full circle around vertical axis

                // Convert to radians for trigonometric functions
                float angleRad = angle * Mathf.Deg2Rad;
                float azimuthRad = azimuth * Mathf.Deg2Rad;

                // Calculate force direction in cone
                float forceX = Mathf.Sin(angleRad) * Mathf.Cos(azimuthRad);
                float forceZ = Mathf.Sin(angleRad) * Mathf.Sin(azimuthRad);
                float forceY = Mathf.Cos(angleRad); // Upward component

                Vector3 explosionDirection = new Vector3(forceX, forceY, forceZ).normalized * explosionForce;

                // Randomize delay for each rock
                float randomDelay = Random.Range(attractionDelayMin, attractionDelayMax);

                float randomScale = Random.Range(scaleMin, scaleMax);
                particle.transform.localScale = new Vector3(randomScale, randomScale, randomScale);

                particle.Initialize(explosionDirection, player, randomDelay);
            }
        }

        private void OnDestroy()
        {
            if (_visualEffectBase != null)
            {
                _visualEffectBase.onInitialized -= OnInitialized;
            }
        }
    }
}
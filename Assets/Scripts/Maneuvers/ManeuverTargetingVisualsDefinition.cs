using DWD.Utility.Loading;
using Fusion;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(menuName = "LichLord/Maneuvers/TargetingDefinition", fileName = "ManeuverTargeting")]
    public class ManeuverTargetingDefinition : ScriptableObject
    {
        [SerializeField]
        private float _maxRange = -1f;
        public float MaxRange => _maxRange;

        [Header("Targeting")]
        [SerializeField]
        private bool _staticTargetsOnly;
        public bool StaticTargetsOnly => _staticTargetsOnly;

        [SerializeField]
        private bool _raycastToGround;
        public bool RaycastToGround => _raycastToGround;

        [SerializeField]
        private LayerMask _groundLayerMask = ~0;
        public LayerMask GroundLayerMask => _groundLayerMask;

        [SerializeField]
        private float _groundRaycastDistance = 50f;
        public float GroundRaycastDistance => _groundRaycastDistance;

        [Header("Visuals")]
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _visualsPrefab;
        public BundleObject VisualsPrefab => _visualsPrefab;

        public Vector3 GetTargetPosition(ManeuverDefinition manuever, PlayerCharacter pc, NetworkRunner runner)
        {
            if (pc == null || pc.Context?.Camera == null)
            {
                Debug.LogWarning("ManeuverTargetingDefinition: Missing PlayerCharacter or Camera reference");
                return pc != null ? pc.Position : Vector3.zero;
            }

            var cachedHit = pc.Context.Camera.CachedRaycastHit;

            Vector3 targetPosition = StaticTargetsOnly
                ? cachedHit.staticPosition
                : cachedHit.position;

            Vector3 playerPos = pc.Position;

            if (MaxRange > 0f)
            {
                Vector3 toTarget = targetPosition - playerPos;
                float distance = toTarget.magnitude;

                if (distance > MaxRange)
                {
                    Vector3 direction = toTarget.normalized;
                    targetPosition = playerPos + direction * MaxRange;
                }
            }

            if (RaycastToGround)
            {
                // Raycast straight down from the candidate position
                Vector3 rayStart = targetPosition + Vector3.up * 2f; // slight offset upward to avoid starting inside colliders
                Ray downRay = new Ray(rayStart, Vector3.down);

                if (Physics.Raycast(downRay, out RaycastHit groundHit, GroundRaycastDistance, GroundLayerMask, QueryTriggerInteraction.Ignore))
                {
                    targetPosition = groundHit.point;
                }
                else
                {
                    Debug.Log("ManeuverTargeting: No ground hit found when raycasting down");
                }
            }

            return targetPosition;
        }
    }
}
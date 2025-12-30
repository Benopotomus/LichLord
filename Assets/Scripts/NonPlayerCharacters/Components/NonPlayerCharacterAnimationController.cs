using System.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Rukhanka;
using AYellowpaper.SerializedCollections;
using LichLord.Projectiles;
using Unity.Mathematics;

namespace LichLord.NonPlayerCharacters
{
    public partial class NonPlayerCharacterAnimationController : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;

        [SerializeField]
        [SerializedDictionary]
        private SerializedDictionary<ProjectileDefinition, FAnimationCallbackData> _animationCallbacks;

        private Entity visualEntityPrefab;
        private Entity visualEntity;
        private EntityManager entityManager;

        // Fast parameters (static to avoid allocation)
        private static readonly FastAnimatorParameter Moving = new("Moving");
        private static readonly FastAnimatorParameter Blocking = new("Blocking");
        private static readonly FastAnimatorParameter Weapon = new("Weapon");
        private static readonly FastAnimatorParameter TriggerNumber = new("TriggerNumber");
        private static readonly FastAnimatorParameter Trigger = new("Trigger");
        private static readonly FastAnimatorParameter RightWeapon = new("RightWeapon");
        private static readonly FastAnimatorParameter LeftWeapon = new("LeftWeapon");
        private static readonly FastAnimatorParameter Side = new("Side");
        private static readonly FastAnimatorParameter Jumping = new("Jumping");
        private static readonly FastAnimatorParameter Action = new("Action");
        private static readonly FastAnimatorParameter AnimationSpeed = new("AnimationSpeed");
        private static readonly FastAnimatorParameter VelocityX = new("Velocity X");
        private static readonly FastAnimatorParameter VelocityZ = new("Velocity Z");
        private static readonly FastAnimatorParameter CycleOffset = new("CycleOffset");

        [Header("Animation Smoothing")]
        private float velocitySmoothTime = 0.1f;
        private Vector3 smoothedLocalVelocity;
        private float smoothedYawVelocity;

        public void OnSpawned()
        {
            CleanupPreviousVisualEntity();
            StartCoroutine(WaitForVisualPrefabAndSpawn());
        }

        private void CleanupPreviousVisualEntity()
        {
            if (visualEntity != Entity.Null && entityManager != null && entityManager.Exists(visualEntity))
            {
                entityManager.DestroyEntity(visualEntity);
                visualEntity = Entity.Null; // Reset so we don't try to destroy twice
            }
        }

        private IEnumerator WaitForVisualPrefabAndSpawn()
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;

            while (world == null || !world.IsCreated)
                yield return null;

            entityManager = world.EntityManager;

            while (entityManager == null)
                yield return null;

            var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<VisualEntityPrefab>());

            while (!query.HasSingleton<VisualEntityPrefab>())
                yield return null;

            visualEntityPrefab = query.GetSingleton<VisualEntityPrefab>().Prefab;

            if (visualEntityPrefab == Entity.Null)
            {
                Debug.LogError("AnimatorConnectorTest: Visual prefab is Null!");
                yield break;
            }

            visualEntity = world.EntityManager.Instantiate(visualEntityPrefab);

            // Reacquire buffer for parameter setting
            if (entityManager.HasComponent<AnimatorControllerParameterIndexTableComponent>(visualEntity))
            {
                var indexTable = entityManager.GetComponentData<AnimatorControllerParameterIndexTableComponent>(visualEntity);
                var parameterBuffer = entityManager.GetBuffer<AnimatorControllerParameterComponent>(visualEntity);
                var paramAspect = new AnimatorParametersAspect(parameterBuffer, indexTable);

                // Random offset: 0–1 (full cycle)
                float randomOffset = UnityEngine.Random.Range(0f, 1f);
                paramAspect.SetFloatParameter(CycleOffset, randomOffset);
            }

            SyncTransformToEntity();
        }

        public void SetAnimationForTrigger(FAnimationTrigger animationTrigger, bool forceWeaponId = false)
        {

            if (visualEntity == Entity.Null) return;

            int weaponId = forceWeaponId ? animationTrigger.Weapon : _npc.Weapons.GetWeaponID();

            if (TryGetParametersAspect(out var paramAspect))
            {
                paramAspect.SetBoolParameter(Moving, animationTrigger.IsMoving);
                paramAspect.SetIntParameter(Weapon, weaponId);
                paramAspect.SetIntParameter(Action, animationTrigger.Action);
                paramAspect.SetIntParameter(Jumping, 0);
                paramAspect.SetIntParameter(TriggerNumber, animationTrigger.TriggerNumber);
                paramAspect.SetIntParameter(RightWeapon, animationTrigger.RightWeapon);
                paramAspect.SetIntParameter(Side, animationTrigger.Side);
                paramAspect.SetBoolParameter(Blocking, animationTrigger.IsBlocking);
                paramAspect.SetIntParameter(LeftWeapon, 7);
                paramAspect.SetFloatParameter(AnimationSpeed, animationTrigger.PlaybackSpeed);
                
                paramAspect.SetTrigger(Trigger);
            }
        }

        public void SetAnimationForState(ENPCState oldState, ENPCState newState)
        {
            if (oldState == newState || visualEntity == Entity.Null) return;

            if (TryGetParametersAspect(out var paramAspect))
            {
                switch (newState)
                {
                    case ENPCState.Idle:
                        paramAspect.SetIntParameter(Weapon, _npc.Weapons.GetWeaponID());
                        paramAspect.SetIntParameter(TriggerNumber, 25);
                        paramAspect.SetFloatParameter(AnimationSpeed, 1f);

                        if (oldState == ENPCState.Inactive || oldState == ENPCState.Dead)
                            paramAspect.SetTrigger(Trigger);
                        break;

                    case ENPCState.Dead:
                        paramAspect.SetIntParameter(Weapon, _npc.Weapons.GetWeaponID());
                        paramAspect.SetIntParameter(TriggerNumber, 20);
                        paramAspect.SetTrigger(Trigger);
                        paramAspect.SetFloatParameter(AnimationSpeed, 1f);
                        break;
                }
            }
        }

        public void UpdateAnimatonForMovement(NonPlayerCharacterRuntimeState runtimeState, Vector3 localVelocity, float yawVelocity, float renderDeltaTime)
        {
            if (runtimeState.GetState() != ENPCState.Idle || visualEntity == Entity.Null) return;

            float smoothRate = Time.deltaTime / velocitySmoothTime;
            smoothedLocalVelocity = Vector3.Lerp(smoothedLocalVelocity, localVelocity, smoothRate);
            smoothedYawVelocity = Mathf.Lerp(smoothedYawVelocity, yawVelocity, smoothRate);

            float speed = localVelocity.magnitude;
            float walkSpeed = runtimeState.Definition.WalkSpeed;

            bool isMoving = speed > 0.1f || Mathf.Abs(yawVelocity) > 1f;

            if (speed < 0.1f)
            {
                smoothedLocalVelocity.z = 0f;
                smoothedLocalVelocity.x = smoothedYawVelocity * 2f; // Turn in place
            }

            if (TryGetParametersAspect(out var paramAspect))
            {
                int weaponId = _npc.Weapons.GetWeaponID();
                paramAspect.SetIntParameter(Weapon, weaponId);
                paramAspect.SetBoolParameter(Moving, isMoving);
                paramAspect.SetFloatParameter(VelocityX, smoothedLocalVelocity.x);
                paramAspect.SetFloatParameter(VelocityZ, smoothedLocalVelocity.z);
            }
        }

        // Cached for performance
        private Vector3 lastSyncedPosition;
        private Quaternion lastSyncedRotation;

        public void SyncTransformToEntity()
        {
            if (visualEntity == Entity.Null)
                return;

             Vector3 pos = transform.position;
            Quaternion rot = transform.rotation;

            if (Vector3.SqrMagnitude(pos - lastSyncedPosition) > 0.01f ||
                Quaternion.Angle(rot, lastSyncedRotation) > 0.1f)
            {
                // Create the LocalTransform with position, rotation, and uniform scale
                LocalTransform transform = LocalTransform.FromPositionRotationScale(pos, rot, 0.41f);

                entityManager.SetComponentData(visualEntity, transform);

                lastSyncedPosition = pos;
                lastSyncedRotation = rot;
            }
        }

        // Helper: safely get fresh parameters aspect every call
        private bool TryGetParametersAspect(out AnimatorParametersAspect aspect)
        {
            aspect = default;

            if (visualEntity == Entity.Null || entityManager == null || !entityManager.Exists(visualEntity))
                return false;

            if (!entityManager.HasComponent<AnimatorControllerParameterIndexTableComponent>(visualEntity))
                return false;

            var indexTable = entityManager.GetComponentData<AnimatorControllerParameterIndexTableComponent>(visualEntity);
            var buffer = entityManager.GetBuffer<AnimatorControllerParameterComponent>(visualEntity);

            aspect = new AnimatorParametersAspect(buffer, indexTable);
            return true;
        }
    }
}
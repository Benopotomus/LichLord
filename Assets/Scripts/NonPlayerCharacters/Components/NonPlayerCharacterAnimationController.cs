using System.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Rukhanka;
using AYellowpaper.SerializedCollections;
using LichLord.Projectiles;
using System.Collections.Generic;

namespace LichLord.NonPlayerCharacters
{
    public partial class NonPlayerCharacterAnimationController : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;

        [SerializeField]
        [SerializedDictionary]
        private SerializedDictionary<ProjectileDefinition, FAnimationCallbackData> _animationCallbacks;

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
        private static readonly FastAnimatorParameter Injured = new("Injured");
        private static readonly FastAnimatorParameter Crouch = new("Crouch");

        private static readonly FastAnimatorParameter CycleOffset = new("CycleOffset");

        private static readonly FastAnimatorParameter AdditiveTrigger = new("AdditiveTrigger");
        private static readonly FastAnimatorParameter AdditiveTriggerNumber = new("AdditiveTriggerNumber");

        private const int Hash_FootR = -1235114484; 
        private const int Hash_FootL = 1234503542;
        private const int Hash_Hit = 994339148;  
        private const int Hash_HitSweep = 0;
        private const int Hash_Special = -2083125633;
        private const int Hash_Shoot = 1068997707;

        [ContextMenu("Log Hashes")]
        private void LogHashes()
        {
            Debug.Log("FootR hash: " + Animator.StringToHash("FootR"));
            Debug.Log("FootL hash: " + Animator.StringToHash("FootL"));
            Debug.Log("Hit hash: " + Animator.StringToHash("Hit"));
            Debug.Log("HitSweep hash: " + Animator.StringToHash("HitSweep"));
            Debug.Log("Special hash: " + Animator.StringToHash("Special"));
        }

        [Header("Animation Smoothing")]
        private float velocitySmoothTime = 0.1f;
        private Vector3 smoothedLocalVelocity;
        private float smoothedYawVelocity;

        private float modelScale;

        // Caches split by side
        private List<GpuAttachmentElement> rightSideAttachments = new List<GpuAttachmentElement>();
        private List<GpuAttachmentElement> leftSideAttachments = new List<GpuAttachmentElement>();

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            // Exactly 10 scale values, evenly distributed across ALL NPCs
            const int TotalScaleSteps = 10;

            // Use FullIndex % 10 → gives 0 to 9, perfectly balanced no matter how many NPCs
            int scaleIndex = runtimeState.FullIndex % TotalScaleSteps; // 0,1,2,...,9 repeating

            // Map 0–9 to min–max scale
            modelScale = Mathf.Lerp(runtimeState.Definition.ModelScale.x, 
                runtimeState.Definition.ModelScale.y
                , scaleIndex / (TotalScaleSteps - 1f));

            CleanupPreviousVisualEntity();
            StartCoroutine(WaitForVisualPrefabAndSpawn(runtimeState));
            //LogHashes();

        }

        public void CleanupPreviousVisualEntity()
        {
            if (visualEntity != Entity.Null && entityManager != null && entityManager.Exists(visualEntity))
            {
                entityManager.DestroyEntity(visualEntity);
                visualEntity = Entity.Null; // Reset so we don't try to destroy twice
            }
        }

        private IEnumerator WaitForVisualPrefabAndSpawn(NonPlayerCharacterRuntimeState runtimeState)
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            while (world == null || !world.IsCreated)
                yield return null;

            entityManager = world.EntityManager;

            // Wait until the buffer exists and has data
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VisualEntityPrefabElement>());
            while (query.IsEmpty || entityManager.GetBuffer<VisualEntityPrefabElement>(query.GetSingletonEntity()).Length == 0)
                yield return null;

            var prefab = GetVisualPrefab(runtimeState);

            if (prefab == Entity.Null)
            {
                Debug.LogError("Selected visual prefab is Null!");
                yield break;
            }

            visualEntity = entityManager.Instantiate(prefab);

            // Rest of your setup (cycle offset, etc.)
            if (entityManager.HasComponent<AnimatorControllerParameterIndexTableComponent>(visualEntity))
            {
                var indexTable = entityManager.GetComponentData<AnimatorControllerParameterIndexTableComponent>(visualEntity);
                var parameterBuffer = entityManager.GetBuffer<AnimatorControllerParameterComponent>(visualEntity);
                var paramAspect = new AnimatorParametersAspect(parameterBuffer, indexTable);

                const int TotalScaleSteps = 10;
                // Use FullIndex % 10 → gives 0 to 9, perfectly balanced no matter how many NPCs
                int scaleIndex = runtimeState.FullIndex % TotalScaleSteps; // 0,1,2,...,9 repeating

                paramAspect.SetFloatParameter(CycleOffset, scaleIndex / (TotalScaleSteps - 1f));
                paramAspect.SetIntParameter(Weapon, _npc.RuntimeState.Definition.WeaponState);

                SetAnimationForState(ENPCState.Inactive, _npc.RuntimeState.GetState());
            }

            SyncTransformToEntity();
            CacheAttachments(runtimeState);

            int randomRight = UnityEngine.Random.Range(0, rightSideAttachments.Count);
            int randomLeft = UnityEngine.Random.Range(0, leftSideAttachments.Count);
            
            ShowOnlySpecificAttachment(0, randomRight);
            ShowOnlySpecificAttachment(1, randomLeft);
        }

        private void CacheAttachments(NonPlayerCharacterRuntimeState runtimeState)
        {
            rightSideAttachments.Clear();
            leftSideAttachments.Clear();

            var prefabSingletonQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VisualEntityPrefabElement>());
            if (prefabSingletonQuery.IsEmpty)
            {
                Debug.LogError("No VisualEntityPrefabElement buffer found! Cannot load attachments.");
            }
            else
            {
                var singletonEntity = prefabSingletonQuery.GetSingletonEntity();
                var attachmentBuffer = entityManager.GetBuffer<GpuAttachmentElement>(singletonEntity);

                int myTableId = runtimeState.Definition.TableID;

                for (int i = 0; i < attachmentBuffer.Length; i++)
                {
                    var entry = attachmentBuffer[i];
                    if (entry.DefinitionTableId != myTableId)
                        continue;

                    if (!entityManager.Exists(entry.AttachmentEntity))
                        continue;

                    if (entry.Side == 0) // Right side
                    {
                        rightSideAttachments.Add(entry);
                    }
                    else if (entry.Side == 1) // Left side
                    {
                        leftSideAttachments.Add(entry);
                    }
                }
            }
        }

        private Entity GetVisualPrefab(NonPlayerCharacterRuntimeState runtimeState)
        {
            int targetId = runtimeState.Definition.TableID;

            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<VisualEntityPrefabElement>());
            if (query.IsEmpty)
            {
                Debug.LogError("No visual prefabs authoring object found in scene!");
                return Entity.Null;
            }

            var authoringEntity = query.GetSingletonEntity();
            var prefabBuffer = entityManager.GetBuffer<VisualEntityPrefabElement>(authoringEntity);

            // Linear search — perfectly fine for <50 entries (your NPC types)
            for (int i = 0; i < prefabBuffer.Length; i++)
            {
                if (prefabBuffer[i].DefinitionTableId == targetId)
                {
                    return prefabBuffer[i].Prefab;
                }
            }

            Debug.LogWarning($"No visual prefab found for TableID {targetId} (Definition: {runtimeState.Definition.name}). Using fallback or null.");
            return Entity.Null;
        }

        public void SetAnimationForTrigger(FAnimationTrigger animationTrigger, bool forceWeaponId = false)
        {
            if (visualEntity == Entity.Null) return;

            if (TryGetParametersAspect(out var paramAspect))
            {
                paramAspect.SetBoolParameter(Moving, animationTrigger.IsMoving);
                paramAspect.SetIntParameter(Weapon, _npc.RuntimeState.Definition.WeaponState);
                paramAspect.SetIntParameter(Action, animationTrigger.Action);
                paramAspect.SetIntParameter(Jumping, 0);
                paramAspect.SetIntParameter(TriggerNumber, animationTrigger.TriggerNumber);
                paramAspect.SetIntParameter(RightWeapon, _npc.RuntimeState.Definition.WeaponRight);
                paramAspect.SetIntParameter(Side, animationTrigger.Side);
                paramAspect.SetBoolParameter(Blocking, animationTrigger.IsBlocking);
                paramAspect.SetIntParameter(LeftWeapon, _npc.RuntimeState.Definition.WeaponLeft);
                paramAspect.SetFloatParameter(AnimationSpeed, animationTrigger.PlaybackSpeed);

                paramAspect.SetTrigger(Trigger);
            }
        }

        public void SetAdditiveAnimationForTrigger(FAdditiveAnimationTrigger additiveAnimationTrigger)
        {
            if (visualEntity == Entity.Null) return;

            if (TryGetParametersAspect(out var paramAspect))
            {
                paramAspect.SetIntParameter(AdditiveTriggerNumber, additiveAnimationTrigger.TriggerNumber);
                paramAspect.SetTrigger(AdditiveTrigger);
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
                        paramAspect.SetIntParameter(Weapon, _npc.RuntimeState.Definition.WeaponState);
                        paramAspect.SetIntParameter(TriggerNumber, 25);
                        paramAspect.SetFloatParameter(AnimationSpeed, 1f);
                        paramAspect.SetIntParameter(RightWeapon, _npc.RuntimeState.Definition.WeaponRight);
                        paramAspect.SetIntParameter(LeftWeapon, _npc.RuntimeState.Definition.WeaponLeft);
                        paramAspect.SetTrigger(Trigger);

                        //if (oldState == ENPCState.Inactive || oldState == ENPCState.Dead)
                        //    paramAspect.SetTrigger(Trigger);
                        break;

                    case ENPCState.Dead:
                        paramAspect.SetIntParameter(Weapon, _npc.RuntimeState.Definition.WeaponState);
                        paramAspect.SetIntParameter(TriggerNumber, 20);
                        paramAspect.SetTrigger(Trigger);
                        paramAspect.SetFloatParameter(AnimationSpeed, 1f);
                        break;

                    case ENPCState.Spawning:
                        paramAspect.SetIntParameter(Weapon, _npc.RuntimeState.Definition.WeaponState);
                        paramAspect.SetIntParameter(TriggerNumber, 30);
                        paramAspect.SetFloatParameter(AnimationSpeed, 1f);
                        paramAspect.SetIntParameter(RightWeapon, _npc.RuntimeState.Definition.WeaponRight);
                        paramAspect.SetIntParameter(LeftWeapon, _npc.RuntimeState.Definition.WeaponLeft);
                        paramAspect.SetTrigger(Trigger);

                        break;
                }
            }
        }

        public void UpdateAnimatonForMovement(NonPlayerCharacterRuntimeState runtimeState, Vector3 localVelocity, float yawVelocity, float renderDeltaTime)
        {
            if (runtimeState.GetState() != ENPCState.Idle) 
                return;

            float walkSpeed = runtimeState.Definition.WalkSpeed;

            float smoothRate = Time.deltaTime / velocitySmoothTime;
            smoothedLocalVelocity = Vector3.Lerp(smoothedLocalVelocity, localVelocity / walkSpeed, smoothRate);
            smoothedYawVelocity = Mathf.Lerp(smoothedYawVelocity, yawVelocity, smoothRate);

            float speed = smoothedLocalVelocity.magnitude;

            bool isMoving = speed > 0.01f || Mathf.Abs(yawVelocity) > 1f;

            if (speed <= 0.01f)
            {
                smoothedLocalVelocity.z = 0f;
                smoothedLocalVelocity.x = smoothedYawVelocity * 2f; // Turn in place
            }

            if (TryGetParametersAspect(out var paramAspect))
            {
                paramAspect.SetIntParameter(Weapon, _npc.RuntimeState.Definition.WeaponState);
                paramAspect.SetIntParameter(RightWeapon, _npc.RuntimeState.Definition.WeaponRight);
                paramAspect.SetIntParameter(LeftWeapon, _npc.RuntimeState.Definition.WeaponLeft);
                paramAspect.SetBoolParameter(Injured, false);
                paramAspect.SetBoolParameter(Crouch, false);

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
                LocalTransform transform = LocalTransform.FromPositionRotationScale(pos, rot, modelScale);

                entityManager.SetComponentData(visualEntity, transform);

                lastSyncedPosition = pos;
                lastSyncedRotation = rot;
            }
        }

        public void UpdateAnimationEvents()
        {
            if (visualEntity == Entity.Null || !entityManager.Exists(visualEntity))
                return;

            // Safety: Only if buffer exists (enabled in authoring)
            if (!entityManager.HasBuffer<AnimationEventComponent>(visualEntity))
                return;

            var eventBuffer = entityManager.GetBuffer<AnimationEventComponent>(visualEntity);

            // Process all events that fired this frame
            foreach (var evt in eventBuffer)
            {
                // Cast uint to int (safe for Animator hashes)
                int nameHash = (int)evt.nameHash;

                switch (nameHash)
                {
                    case Hash_FootR:
                        //Debug.Log("Foot R triggered!");
                        // Your footstep sound/particles for right foot
                        break;

                    case Hash_FootL:
                        //Debug.Log("Foot L triggered!");
                        // Left foot logic
                        break;

                    case Hash_Hit:
                        _npc.Brain.OnHitFromAnimation();
                        break;

                    case Hash_HitSweep:
                        //Debug.Log("HitSweep event!");
                        // Your special logic
                        break;

                    case Hash_Special:
                        //Debug.Log("Special event!");
                        // Your special logic
                        break;

                    case Hash_Shoot:
                        //Debug.Log("Special event!");
                        // Your special logic
                        break;
                    default:
                        Debug.LogWarning($"Unhandled animation event hash: {nameHash}");
                        break;
                }
            }

            eventBuffer.Clear();
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

        // Side 0 = right, Side 1 = left
        public bool ShowOnlySpecificAttachment(int side, int elementIndex)
        {
            // Select the correct list
            var targetList = side == 0 ? rightSideAttachments :
                             (side == 1 ? leftSideAttachments : null);

            if (targetList == null)
                return false;

            if (elementIndex < 0 || elementIndex >= targetList.Count)
                return false;

            int shownCount = 0;
            int hiddenCount = 0;

            // Loop through all attachments on this side
            for (int i = 0; i < targetList.Count; i++)
            {
                var entry = targetList[i];
                var entity = entry.AttachmentEntity;

                // Skip if entity no longer exists (rare safety check)
                if (!entityManager.Exists(entity))
                    continue;

                if (i == elementIndex)
                {
                    // This is the one we want to show
                    if (entityManager.HasComponent<Disabled>(entity))
                    {
                        entityManager.RemoveComponent<Disabled>(entity);
                        shownCount++;
                    }
                }
                else
                {
                    // Hide all others
                    if (!entityManager.HasComponent<Disabled>(entity))
                    {
                        entityManager.AddComponent<Disabled>(entity);
                        hiddenCount++;
                    }
                }
            }

            if (shownCount > 0 || hiddenCount > 0)
            {
                var boneIndex = targetList[elementIndex].BoneIndex;
                return true;
            }

            return false;
        }
    }
}
using System.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Rukhanka;

public partial class AnimatorConnectorTest : MonoBehaviour
{
    [Header("Random Movement")]
    public float moveSpeed = 5f;
    private float randomRadius = 50f;
    public float arrivalThreshold = 0.2f;

    [Header("Rotation")]
    public float rotationSmoothTime = 0.15f;

    [Header("Animation Smoothing")]
    private float velocitySmoothTime = 0.1f; // Adjust in Inspector (0.05–0.2 is good)
    private Vector3 smoothedLocalVelocity;

    private Entity visualEntityPrefab;
    private Entity visualEntity;
    private EntityManager entityManager;

    private Vector3 startPosition;
    private Vector3 currentTarget;
    private Vector3 previousPosition;

    // Cached for performance
    private Vector3 lastSyncedPosition;
    private Quaternion lastSyncedRotation;


    // Animator parameters (static for zero allocation)
    private static readonly FastAnimatorParameter Moving = new("Moving");
    private static readonly FastAnimatorParameter VelocityX = new("Velocity X");
    private static readonly FastAnimatorParameter VelocityZ = new("Velocity Z");
    static readonly FastAnimatorParameter CycleOffset = new("CycleOffset");

    private void Start()
    {
        previousPosition = transform.position;
        startPosition = transform.position;
        lastSyncedPosition = startPosition;
        lastSyncedRotation = transform.rotation;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        StartCoroutine(WaitForVisualPrefabAndSpawn());
    }

    private IEnumerator WaitForVisualPrefabAndSpawn()
    {
        var world = World.DefaultGameObjectInjectionWorld;

        while (world == null || !world.IsCreated)
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
            float randomOffset = Random.Range(0f, 1f);
            paramAspect.SetFloatParameter(CycleOffset, randomOffset);
        }

        SyncTransformToEntity();
        ChooseNewRandomTarget();
    }

    private void Update()
    {
        // === Calculate velocity ===
        Vector3 worldVelocity = (transform.position - previousPosition) / Time.deltaTime;

        previousPosition = transform.position;

        // === Movement logic ===
        Vector3 directionToTarget = currentTarget - transform.position;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > arrivalThreshold)
        {
            transform.position = Vector3.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);

            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / rotationSmoothTime);
            }
        }
        else
        {
            if (currentTarget != startPosition)
                currentTarget = startPosition;
            else
                ChooseNewRandomTarget();
        }

        // After calculating localVelocity
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        smoothedLocalVelocity = Vector3.Lerp(smoothedLocalVelocity, localVelocity, Time.deltaTime / velocitySmoothTime);

        // Use smoothed version for parameters
        float speedNormalized = smoothedLocalVelocity.magnitude / moveSpeed;
        bool isMoving = speedNormalized > 0.05f; // Slightly higher threshold to avoid idle flicker

        // === Sync transform to ECS visual ===
        if (visualEntity != Entity.Null)
        {
            SyncTransformToEntity();
        }

        // === Update Rukhanka animator parameters (reacquire buffer every frame) ===
        if (visualEntity != Entity.Null && entityManager.Exists(visualEntity))
        {
            if (entityManager.HasComponent<AnimatorControllerParameterIndexTableComponent>(visualEntity))
            {
                var indexTable = entityManager.GetComponentData<AnimatorControllerParameterIndexTableComponent>(visualEntity);
                var parameterBuffer = entityManager.GetBuffer<AnimatorControllerParameterComponent>(visualEntity);

                // Recreate the aspect every frame to avoid invalidation
                var paramAspect = new AnimatorParametersAspect(parameterBuffer, indexTable);

                paramAspect.SetBoolParameter(Moving, isMoving);
                paramAspect.SetFloatParameter(VelocityX, smoothedLocalVelocity.x / moveSpeed);
                paramAspect.SetFloatParameter(VelocityZ, smoothedLocalVelocity.z / moveSpeed);
            }
        }
    }

    private void ChooseNewRandomTarget()
    {
        Vector2 circle = Random.insideUnitCircle * randomRadius;
        currentTarget = startPosition + new Vector3(circle.x, 0f, circle.y);
    }

    private void SyncTransformToEntity()
    {
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        if (Vector3.SqrMagnitude(pos - lastSyncedPosition) > 0.0001f ||
            Quaternion.Angle(rot, lastSyncedRotation) > 0.1f)
        {
            entityManager.SetComponentData(visualEntity, LocalTransform.FromPositionRotation(pos, rot));
            lastSyncedPosition = pos;
            lastSyncedRotation = rot;
        }
    }

    private void OnDestroy()
    {
        if (visualEntity != Entity.Null && World.DefaultGameObjectInjectionWorld != null && World.DefaultGameObjectInjectionWorld.IsCreated)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (em.Exists(visualEntity))
                em.DestroyEntity(visualEntity);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(startPosition, randomRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(currentTarget, 0.5f);
            Gizmos.DrawLine(transform.position, currentTarget);
        }
    }
#endif
}
using System.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class AnimatorConnectorTest : MonoBehaviour
{
    // Configurable settings
    [Header("Random Movement")]
    public float moveSpeed = 5f;
    public float randomRadius = 10f;      // Max distance from start for random targets
    public float arrivalThreshold = 0.2f; // How close to consider "arrived"

    [Header("Rotation")]
    public float rotationSmoothTime = 0.15f; // Smooth turning speed

    private Entity visualEntityPrefab;
    private Entity visualEntity;
    private EntityManager entityManager;

    private Vector3 startPosition;
    private Vector3 currentTarget;
    private Vector3 velocity; // For smooth rotation damping

    // Change detection for ECS sync
    private Vector3 lastSyncedPosition;
    private Quaternion lastSyncedRotation;

    private void Start()
    {
        startPosition = transform.position;
        lastSyncedPosition = startPosition;
        lastSyncedRotation = transform.rotation;

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Start waiting for the singleton instead of checking immediately
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

        //Debug.Log("Visual prefab loaded successfully!");

        visualEntity = world.EntityManager.Instantiate(visualEntityPrefab);
        SyncTransformToEntity();
        ChooseNewRandomTarget();
    }

    private void Update()
    {
        // Move toward current target
        Vector3 directionToTarget = (currentTarget - transform.position);
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget > arrivalThreshold)
        {
            // Move
            Vector3 moveStep = Vector3.MoveTowards(transform.position, currentTarget, moveSpeed * Time.deltaTime);
            transform.position = moveStep;

            // Smooth rotation to face movement direction (only if moving significantly)
            if (directionToTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / rotationSmoothTime);
            }
        }
        else
        {
            // Arrived — decide next action
            if (currentTarget != startPosition)
            {
                // Just arrived at random point → now go back home
                currentTarget = startPosition;
            }
            else
            {
                // Back home → pick new random target
                ChooseNewRandomTarget();
            }
        }

        // Sync to ECS visual immediately
        if (visualEntity != Entity.Null)
        {
            SyncTransformToEntity();
        }
    }

    private void ChooseNewRandomTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * randomRadius;
        currentTarget = startPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    private void SyncTransformToEntity()
    {

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        if (Vector3.SqrMagnitude(pos - lastSyncedPosition) > 0.0001f ||
            Quaternion.Angle(rot, lastSyncedRotation) > 0.1f)
        {
            var localTransform = LocalTransform.FromPositionRotation(pos, rot);
            entityManager.SetComponentData(visualEntity, localTransform);

            lastSyncedPosition = pos;
            lastSyncedRotation = rot;
        }
    }

    private void OnDestroy()
    {
        // Early exit if EntityManager is null or no visual entity
        if (visualEntity == Entity.Null || entityManager == null)
            return;

        // Use World directly via the static DefaultGameObjectInjectionWorld
        // This avoids touching the potentially disposed entityManager.World
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        // Re-get a valid EntityManager from the world (if still alive)
        var currentManager = world.EntityManager;
        if (currentManager.Exists(visualEntity))
        {
            currentManager.DestroyEntity(visualEntity);
        }

        // Optional: null out references to help GC
        visualEntity = Entity.Null;
    }

#if UNITY_EDITOR
    // Optional: Visualize path in editor
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
using LichLord.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterBrainComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField]
        private Vector3 _moveTarget;
        public Vector3 MoveTarget => _moveTarget;

        [SerializeField]
        private IChunkTrackable _attackTarget;
        public IChunkTrackable AttackTarget => _attackTarget;

        private float _findTimeMax = 0.5f;
        private float _findTimer;

        private float _destinationRefreshTime = 0.5f;
        private float _destinationRefreshTimer;

        [SerializeField]
        private List<NonPlayerCharacterManeuverState> _maneuvers = new List<NonPlayerCharacterManeuverState>();

        [SerializeField]
        private NonPlayerCharacterManeuverState _activeManeuver = null;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            if (NPC.State.CurrentState == ENonPlayerState.Inactive ||
                NPC.State.CurrentState == ENonPlayerState.Dead ||
                NPC.State.CurrentState == ENonPlayerState.HitReact)
                return;

            UpdateManeuverTimers(ref data, renderDeltaTime);

            // Detect if an active state is running 
            // We don't want to update the target during this
            // since we need to wait for the hit event from animation
            // but we do want to rotate to the target

            UpdateExecutingManeuver(ref data, renderDeltaTime);


            // We only tick if we're idle and ready
            if (data.State != ENonPlayerState.Idle)
                return;

            UpdateBrain(ref data, renderDeltaTime);
        }

        private void UpdateBrain(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            UpdateSenses(renderDeltaTime);
            SelectManeuver(renderDeltaTime);
            UpdateActiveManeuver(ref data, renderDeltaTime);
            UpdateWanderMovement();
        }

        private void UpdateWanderMovement()
        {
            if (_activeManeuver != null)
                return;

            NPC.Movement.AIFollower.stopDistance = 0.2f;
            NPC.Movement.SetFollowerEnabled(true);
            NPC.Movement.SetFollowUpdateRotation(true);

            if (Vector3.Distance(NPC.CachedTransform.position, _moveTarget) < 3)
            {
                _moveTarget = new Vector3(
                    UnityEngine.Random.Range(-20f, 20f),
                    0f, // Keep Y fixed
                    UnityEngine.Random.Range(-20f, 20f)
                );

                NPC.Movement.AIFollower.destination = _moveTarget;
            }
        }

        private void UpdateManeuverTimers(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            for (int i = 0; i < _maneuvers.Count; i++)
            {
                _maneuvers[i].UpdateCooldownTimer(renderDeltaTime);
            }

            if(_activeManeuver != null)
                _activeManeuver.UpdateStateTimer(NPC, ref data, renderDeltaTime);
        }

        private void SelectManeuver(float renderDeltaTime)
        {
            // Check if the active maneuver is no longer valid
            if (_activeManeuver != null)
            { 
                if(!_activeManeuver.CanBeSelected(this))
                    _activeManeuver = null;
            }

            // if there is no active maneuver, select another
            if (_activeManeuver == null)
            {
                for (int i = 0; i < _maneuvers.Count; i++)
                { 
                    var currentManeuver = _maneuvers[i];
                    if (currentManeuver.CanBeSelected(this))
                    { 
                        _activeManeuver = currentManeuver;
                    }
                }
            }
        }

        // Runs when active meneuver is executed (in state)
        private void UpdateExecutingManeuver(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            if (_activeManeuver == null)
                return;

            if (_activeManeuver.HasExpired())
            {
                if (_attackTarget == null || !_attackTarget.IsAttackable)
                {
                    // Refresh target
                    _findTimer = 0;
                    UpdateSenses(renderDeltaTime);
                }

                data.State = ENonPlayerState.Idle;
                NPC.Replicator.UpdateNPCData(data);
            }
            else
            {
                if (_attackTarget != null)
                {
                    UpdateDestination(renderDeltaTime, NPC.CachedTransform.position);

                    NPC.Movement.SetFollowUpdateRotation(false);
                    RotateTowardTarget(_attackTarget.Position, renderDeltaTime);
                }
            }
        }

        private void UpdateActiveManeuver(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            if (_activeManeuver == null)
                return;

            if (_attackTarget != null)
            {
                Vector3 attackTargetPosition = _attackTarget.Position;

                float sqrDist = (NPC.CachedTransform.position - attackTargetPosition).sqrMagnitude;

                if (sqrDist < _activeManeuver.Definition.MovementStopRangeSqrt)
                {
                    NPC.Movement.SetFollowerEnabled(false);
                    NPC.Movement.SetFollowUpdateRotation(false);

                    RotateTowardTarget(attackTargetPosition, renderDeltaTime);
                    float angle = GetAngleToTarget(attackTargetPosition);

                    if (angle < 5f)
                    {
                        _activeManeuver.ExecuteManeuver(NPC, ref data);
                    }
                }
                else
                {
                    UpdateDestination(renderDeltaTime, attackTargetPosition);
                }
            }
        }

        private void UpdateDestination(float renderDeltaTime, Vector3 newDestination)
        {
            //NPC.Movement.SetFollowerEnabled(true);

            // If our current destination hasn't changed much, we early out
            Vector3 delta = NPC.Movement.AIFollower.destination - newDestination;
            if (delta.sqrMagnitude < 0.01f)
                return;

            _destinationRefreshTime -= renderDeltaTime;
            if (_destinationRefreshTime < 0f)
            {
                NPC.Movement.AIFollower.destination = newDestination;
                _destinationRefreshTime = _destinationRefreshTimer;
            }
        }

        private IChunkTrackable FindCurrentTarget()
        {
            // Get current + nearby chunks
            List<Chunk> chunks = NPC.Manager.Context.ChunkManager.GetNearbyChunks(NPC.CurrentChunk.ChunkID);

            float closestDistance = Mathf.Infinity;
            IChunkTrackable currentTarget = null;

            foreach (var chunk in chunks)
            {
                var trackables = chunk.Trackables;

                for (int i = 0; i < trackables.Count; i++)
                {
                    IChunkTrackable trackable = trackables[i];

                    if (!trackable.IsAttackable)
                        continue;

                    if (trackable is NonPlayerCharacter targetNPC)
                    {
                        if (targetNPC.State.CurrentState == ENonPlayerState.Inactive ||
                            targetNPC.State.CurrentState == ENonPlayerState.Dead)
                            continue;

                        if (targetNPC.TeamID == NPC.TeamID)
                            continue;
                    }

                    float distance = Vector3.Distance(NPC.CachedTransform.position, trackable.Position);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        currentTarget = trackable;
                    }
                }
            }

            return currentTarget;
        }

        private void RotateTowardTarget(Vector3 targetPosition, float renderDeltaTime)
        {
            Vector3 directionToTarget = targetPosition - NPC.CachedTransform.position;
            directionToTarget.y = 0f; // Zero out vertical component to keep it on the XZ plane

            if (directionToTarget.sqrMagnitude > 0.01f) // Avoid zero-length direction
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                Quaternion currentRotation = NPC.CachedTransform.rotation;

                // Optionally smooth with Lerp or Slerp
                NPC.CachedTransform.rotation = Quaternion.RotateTowards(
                    currentRotation,
                    targetRotation,
                    360f * renderDeltaTime // Adjust speed (degrees per second)
                );
            }
        }

        private float GetAngleToTarget(Vector3 targetPosition)
        {
            Vector3 directionToTarget = targetPosition - NPC.CachedTransform.position;
            directionToTarget.y = 0f; // Flatten to horizontal plane
            float unsignedAngle = 180;
            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                Vector3 forward = NPC.CachedTransform.forward;
                forward.y = 0f;

                unsignedAngle = Vector3.Angle(forward, directionToTarget);
                //Debug.Log("Unsigned yaw angle to target: " + unsignedAngle);
            }

            return unsignedAngle;
        }

        private void UpdateSenses(float renderDeltaTime)
        {
            _findTimer -= renderDeltaTime;
            if (_findTimer < 0)
            {
                _attackTarget = FindCurrentTarget();

                if (_attackTarget != null)
                {
                    if (_attackTarget is NonPlayerCharacter npc)
                    {
                        _moveTarget = npc.CachedTransform.position;
                        NPC.Movement.AIFollower.destination = _moveTarget;
                    }
                }
                //Debug.Log(_attackTarget.ToString());

                _findTimer = _findTimeMax;
            }
        }

        public void SetAnimationForManeuver(ENonPlayerState state, int animIndex) 
        {
            var maneuverState = GetManeuverFromState(state);
            if (maneuverState != null)
            {
                var animationTriggers = maneuverState.Definition.AnimationTriggers;

                if (animIndex > animationTriggers.Count)
                    return;

                var animationTrigger = animationTriggers[animIndex];

                NPC.Animator.SetBool("Moving", animationTrigger.IsMoving);
                NPC.Animator.SetBool("Blocking", animationTrigger.IsBlocking);
                NPC.Animator.SetInteger("Action", animationTrigger.Action);
                NPC.Animator.SetInteger("Weapon", animationTrigger.Weapon);
                NPC.Animator.SetInteger("TriggerNumber", animationTrigger.TriggerNumber);
                NPC.Animator.SetTrigger("Trigger");
            }

        }

        public NonPlayerCharacterManeuverState GetManeuverFromState(ENonPlayerState state)
        {
            for (int i = 0; i < _maneuvers.Count; i++)
            {
                if(_maneuvers[i].ActiveState == state)
                    return _maneuvers[i];
            }

            return null;
        }

        public void OnHitFromAnimation()
        {
            if (_attackTarget != null)
            { 
                NonPlayerCharacter npc = _attackTarget as NonPlayerCharacter;
                if(npc != null) 
                {
                    npc.Replicator.ApplyDamage(npc.GUID, 21);
                }
            }
        }

        /*
        public Transform findCurrentTarget()
        {
            //find all potential targets (enemies of this character)
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(attackTag);
            if (enemies.Length == 0)
                return null;

            Transform target = null;

            //if we want this character to communicate with his allies
            if (spread)
            {
                //get all enemies
                List<GameObject> availableEnemies = enemies.ToList();
                int count = 0;

                //make sure it doesn't get stuck in an infinite loop
                while (count < 300)
                {
                    //for all enemies
                    for (int i = 0; i < enemies.Length; i++)
                    {
                        //distance between character and its nearest enemy
                        float closestDistance = Mathf.Infinity;

                        foreach (GameObject potentialTarget in availableEnemies)
                        {
                            //check if there are enemies left to attack and check per enemy if its closest to this character
                            if (Vector3.Distance(transform.position, potentialTarget.transform.position) < closestDistance && potentialTarget != null)
                            {
                                //if this enemy is closest to character, set closest distance to distance between character and enemy
                                closestDistance = Vector3.Distance(transform.position, potentialTarget.transform.position);
                                target = potentialTarget.transform;
                            }
                        }

                        //if it is valid, return this target
                        if (target && canAttack(target))
                        {
                            return target;
                        }
                        else
                        {
                            //if it's not, remove it from the list and try again
                            availableEnemies.Remove(target.gameObject);
                        }
                    }

                    //after checking all enemies, allow one more ally to also attack the same enemy and try again
                    maxAlliesPerEnemy++;
                    availableEnemies.Clear();
                    availableEnemies = enemies.ToList();

                    count++;
                }

                //show a loop error
                Debug.LogError("Infinite loop");
            }
            else
            {
                //if we're using the simple method:
                float closestDistance = Mathf.Infinity;

                foreach (GameObject potentialTarget in enemies)
                {
                    //check if there are enemies left to attack and check per enemy if its closest to this character
                    if (Vector3.Distance(transform.position, potentialTarget.transform.position) < closestDistance && potentialTarget != null)
                    {
                        //if this enemy is closest to character, set closest distance to distance between character and enemy
                        closestDistance = Vector3.Distance(transform.position, potentialTarget.transform.position);
                        target = potentialTarget.transform;
                    }
                }

                //check if there's a target and return it
                if (target)
                    return target;
            }

            //otherwise return null
            return null;
        }
        */
    }
}

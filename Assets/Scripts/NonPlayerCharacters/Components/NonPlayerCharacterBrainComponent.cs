using Fusion;
using LichLord.Buildables;
using LichLord.Props;
using LichLord.World;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterBrainComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField] 
        private Vector3 _moveTarget;

        private IChunkTrackable _attackTarget;
        public IChunkTrackable AttackTarget => _attackTarget;

        // Modulus of ticks x/32
        private int _updateSensesTick = 16;
        private int _updateDestinationTick = 8;
        private int _updateSpeedTick = 8;
        private int _updateRangesTick = 8;

        [SerializeField] 
        private bool _isInMovementStopRange = false;

        [SerializeField] 
        private bool _isInFaceTargetRange = false;

        [SerializeField] 
        private ENPCState _activeManeuverState = ENPCState.Inactive;

        [SerializeField]  
        private bool _hasAttackTarget = false;

        [SerializeField] private bool _hasLineOfSight = false;

        [SerializeField]
        private List<NonPlayerCharacterManeuverState> _maneuvers = new List<NonPlayerCharacterManeuverState>();

        private NonPlayerCharacterManeuverState _activeManeuver = null;

        public PlayerCharacter TargetPlayer;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
            _isInMovementStopRange = false;
            _isInFaceTargetRange = false;
            _hasAttackTarget = false;
            _activeManeuverState = ENPCState.Inactive;
        }

        public void AuthorityUpdate(NonPlayerCharacterRuntimeState runtimeState, float renderDeltaTime, int tick)
        {
            var currentState = runtimeState.GetState();
            if  (currentState == ENPCState.Inactive ||
                currentState == ENPCState.Dead ||
                currentState == ENPCState.HitReact)
                return;

            UpdateAuthorityTick(runtimeState, tick);

            int targetPlayerIndex = runtimeState.GetTargetPlayerIndex();
            TargetPlayer = (targetPlayerIndex > 0) ? _npc.Context.NetworkGame.GetPlayerByIndex(targetPlayerIndex) : null;

            // Detect if an active state is running 
            // We don't want to update the target during this
            // since we need to wait for the hit event from animation
            // but we do want to rotate to the target

            UpdateExecutingManeuver(runtimeState, renderDeltaTime);

            if (_npc.State.CurrentState != ENPCState.Idle)
                return;

            UpdateActiveManeuver(runtimeState, renderDeltaTime, tick);
        }

        public void RemoteUpdate(NonPlayerCharacterRuntimeState runtimeState)
        {
            if (_npc.State.CurrentState == ENPCState.Dead || _npc.State.CurrentState == ENPCState.Inactive)
                return;

            int targetPlayerIndex = runtimeState.GetTargetPlayerIndex();
            TargetPlayer = (targetPlayerIndex > 0) ? _npc.Context.NetworkGame.GetPlayerByIndex(targetPlayerIndex) :  null;
        }

        int _lastTick = -1;
        private void UpdateAuthorityTick(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            if (_lastTick == tick)  return;

            _lastTick = tick;

            UpdateExecutingTimer(runtimeState, tick);

            // Modify the tick by the GUID so not everyone updates at once
            tick += _npc.Index;

            UpdateRangesTick(tick);
            UpdateMoveSpeedTick(runtimeState, tick);
            UpdateDestinationTick(tick);

            // We only tick if we're idle and ready
            if (runtimeState.GetState() != ENPCState.Idle)
                return;

            UpdateSenses(tick);
            SelectManeuver(tick);
            UpdateWanderMovement(runtimeState, tick);
        }

        public void UpdateExecutingTimer(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            var executingManuever = GetManeuverFromState(runtimeState.GetState());

            if (executingManuever == null)
                return;

            if (executingManuever.HasExpired(tick))
            {
                SetActiveManuever(null);
                runtimeState.SetState(ENPCState.Idle);
                _npc.Replicator.ReplicateRuntimeState(runtimeState);
                return;
            }

            executingManuever.UpdateManeuverTick(_npc, tick);
        }

        private void UpdateRangesTick(int tick)
        {
            if (tick % _updateRangesTick != 0)
                return;

            UpdateRanges();

            if (_isInFaceTargetRange)
            {
                if (_attackTarget != null)
                    _hasLineOfSight = GetLineOfSight(_npc.CachedTransform.position, _attackTarget.Position);
                else
                    _hasLineOfSight = false;

                if (_hasLineOfSight)
                {
                    FindBetterLOSPosition();
                }
            }
        }

        private void UpdateMoveSpeedTick(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            if (tick % _updateSpeedTick != 0)
                return;

            if (!_isInFaceTargetRange)
            {
                _npc.Movement.SetFollowerUpdateRotation(true);
                _npc.Movement.SetFollowerMaxSpeed(runtimeState.Definition.WalkSpeed);
            }
            else
            {
                _npc.Movement.SetFollowerUpdateRotation(false);
                _npc.Movement.SetFollowerMaxSpeed(runtimeState.Definition.WalkSpeed * 0.6f);
            }
        }

        [SerializeField] private bool _wanderPositionSet;
        private void UpdateWanderMovement(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            if (_hasAttackTarget)
            {
                _wanderPositionSet = false;
                return;
            }


           // NPC.Movement.AIFollower.stopDistance = 0.2f;
          //  NPC.Movement.SetFollowerUpdatePosition(true);
          //  NPC.Movement.SetFollowerUpdateRotation(true);

            // if we are an invasion npc, the target nexus position is the fallback
            if (runtimeState.IsInvasionNPC())
            {
                if (_wanderPositionSet)
                    return;

                var stronghold = _npc.Context.InvasionManager.TargetStronghold;
                if (stronghold != null)
                {
                    var targetPosition = stronghold.CachedTransform.position;
                    // If our current destination hasn't changed much, we early out
                    Vector3 delta = _moveTarget - targetPosition;
                    if (delta.sqrMagnitude < 0.01f)
                        return;

                    _moveTarget = targetPosition;
                    _npc.Movement.AIFollower.destination = _moveTarget;
                    _wanderPositionSet = true;
                    return;
                }
            }
            else if(runtimeState.IsWorker())
            {
                if (_wanderPositionSet)
                    return;

                Crypt crypt = _npc.Context.WorkerManager.GetCrypt(runtimeState.GetWorkerIndex());

                if (crypt != null)
                {
                    _moveTarget = crypt.CachedTransform.position;
                    _npc.Movement.AIFollower.destination = _moveTarget;
                    _wanderPositionSet = true;
                    Debug.Log("crypt set");
                }

            }
            else
            { 
                if ((tick + _npc.Index) % 64 == 0)
                    return;

                _moveTarget = new Vector3(
                    Random.Range(-20f, 20f),
                    0f, // Keep Y fixed
                    Random.Range(-20f, 20f)
                );

                _moveTarget += _npc.CachedTransform.position;

                _npc.Movement.AIFollower.destination = _moveTarget;
            }
        }

        // Only called when in idle
        private void SelectManeuver(int tick)
        {
            // Check if the active maneuver is no longer valid
            if (HasActiveManeuver())
            { 
                if(!_activeManeuver.CanBeSelected(this, tick))
                    SetActiveManuever(null);
            }

            List<NonPlayerCharacterManeuverState> availableStates = new List<NonPlayerCharacterManeuverState>();

            // if there is no active maneuver, select another
            if (!HasActiveManeuver())
            {
                for (int i = 0; i < _maneuvers.Count; i++)
                { 
                    var currentManeuver = _maneuvers[i];
                    if (currentManeuver.CanBeSelected(this, tick))
                    {
                        availableStates.Add(currentManeuver);
                    }
                }

                if (availableStates.Count == 0)
                {
                    SetActiveManuever(null);
                    return;
                }

                int selectedIndex = Random.Range(0, availableStates.Count);
                SetActiveManuever(availableStates[selectedIndex]);
            }
        }

        private void SetActiveManuever(NonPlayerCharacterManeuverState newManeuver)
        {
            if (newManeuver == _activeManeuver)
                return;

            if (newManeuver == null)
            {
                _activeManeuverState = ENPCState.Inactive;
            }
            else
            {
                _activeManeuverState = newManeuver.ActiveState;
            }

            _activeManeuver = newManeuver;

            // Update ranges after assignment
            UpdateRanges();
        }

        // Runs when active meneuver is executed (in state)
        private void UpdateExecutingManeuver(NonPlayerCharacterRuntimeState runtimeState, float renderDeltaTime)
        {
            var executingManuever = GetManeuverFromState(runtimeState.GetState());

            if (executingManuever == null)
                return;

            if (_hasAttackTarget)
            {
                _npc.Movement.SetFollowerUpdateRotation(false);
                RotateTowardTarget(_attackTarget.Position, renderDeltaTime);
            }
        }

        private void UpdateRanges()
        {
            if (!HasActiveManeuver() ||
                !_hasAttackTarget)
                return;


            float sqrDist;
            Collider targetCollider = _attackTarget.HurtBoxCollider;

            if (targetCollider != null) // Target has a collider
            {
                // Distance from my position to the closest point on target's collider
                Vector3 closestPoint = targetCollider.ClosestPoint(_npc.CachedTransform.position);
                sqrDist = (_npc.CachedTransform.position - closestPoint).sqrMagnitude - _attackTarget.BonusRadius;
            }
            else
            {
                // Standard distance check with bonus radius
                sqrDist = (_npc.CachedTransform.position - _attackTarget.Position).sqrMagnitude - _attackTarget.BonusRadius;
            }

            _isInMovementStopRange = sqrDist < _activeManeuver.Definition.MovementStopRangeSqrt;
            _isInFaceTargetRange = sqrDist < _activeManeuver.Definition.FaceTargetRangeSqrt;
        }

        private void UpdateActiveManeuver(NonPlayerCharacterRuntimeState runtimeState, float renderDeltaTime, int tick)
        {
            if (!HasActiveManeuver() ||
                !_hasAttackTarget ||
                !IsTargetValid(_attackTarget))
                return;

            if (_isInFaceTargetRange)
            { 
                Vector3 attackTargetPosition = _attackTarget.Position;

                _npc.Movement.SetFollowerUpdateRotation(false);
                RotateTowardTarget(attackTargetPosition, renderDeltaTime);

                float angle = GetAngleToTarget(attackTargetPosition);

                if (_isInMovementStopRange)
                {
                    if (angle < 5f)
                    {
                        if (_activeManeuver.Definition.RequiresLOS)
                        {
                            if (_hasLineOfSight)
                                _activeManeuver.ExecuteManeuver(_npc, runtimeState, tick);
                        }
                        else
                            _activeManeuver.ExecuteManeuver(_npc, runtimeState, tick);
                    }
                }
            }
        }

        private void UpdateDestinationTick(int tick)
        {
            if (tick % _updateDestinationTick != 0)
                return;

            if (!_hasAttackTarget)
                return;

            Vector3 targetPosition = _attackTarget.Position;

            // If our current destination hasn't changed much, we early out
            Vector3 delta = _moveTarget - targetPosition;
            if (delta.sqrMagnitude < 0.01f)
                return;

            _moveTarget = targetPosition;
            _npc.Movement.AIFollower.destination = _moveTarget;
        }

        public void FindCurrentTarget()
        {
            if(_npc.CurrentChunk == null) return;

            // Get current + nearby chunks

            float closestDistance = Mathf.Infinity;
            IChunkTrackable currentTarget = null;

            foreach (var chunk in _npc.CachedChunks)
            {
                var trackables = chunk.Trackables;

                for (int i = 0; i < trackables.Count; i++)
                {
                    IChunkTrackable trackable = trackables[i];
                    
                    if (!IsTargetValid(trackable))
                        continue;

                    float distance = Vector3.Distance(_npc.CachedTransform.position, trackable.Position);

                    if (distance < closestDistance)
                    { 
                        closestDistance = distance;
                        currentTarget = trackable;
                    }
                }
            }

            SetAttackTarget(currentTarget);           
        }

        private bool IsTargetValid(IChunkTrackable trackable)
        {
            if (trackable == null) 
                return false;

            if (!trackable.IsAttackable)
                return false;

            if (trackable is NonPlayerCharacter npc)
            {
                if (npc.TeamID == _npc.TeamID)
                    return false;
            }

            if (trackable is PlayerCharacter player)
            {
                if (_npc.TeamID == ETeamID.PlayerTeam)
                    return false;
            }

            if (trackable is Buildable buildable)
            {
                if (_npc.TeamID == ETeamID.PlayerTeam)
                {
                    return false;
                }
            }

            if (trackable is Stronghold strongHold)
            {
                if (_npc.TeamID == ETeamID.PlayerTeam)
                {
                    return false;
                }
            }

            return true;
        }

        private void RotateTowardTarget(Vector3 targetPosition, float renderDeltaTime)
        {
            Vector3 directionToTarget = targetPosition - _npc.CachedTransform.position;
            directionToTarget.y = 0f; // Zero out vertical component to keep it on the XZ plane

            if (directionToTarget.sqrMagnitude > 0.01f) // Avoid zero-length direction
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
                Quaternion currentRotation = _npc.CachedTransform.rotation;

                // Optionally smooth with Lerp or Slerp
                _npc.CachedTransform.rotation = Quaternion.RotateTowards(
                    currentRotation,
                    targetRotation,
                    360f * renderDeltaTime // Adjust speed (degrees per second)
                );
            }
        }

        private float GetAngleToTarget(Vector3 targetPosition)
        {
            Vector3 directionToTarget = targetPosition - _npc.CachedTransform.position;
            directionToTarget.y = 0f; // Flatten to horizontal plane
            float unsignedAngle = 180;
            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                Vector3 forward = _npc.CachedTransform.forward;
                forward.y = 0f;

                unsignedAngle = Vector3.Angle(forward, directionToTarget);
                //Debug.Log("Unsigned yaw angle to target: " + unsignedAngle);
            }

            return unsignedAngle;
        }

        private void UpdateSenses(int tick)
        {
            if (tick % _updateSensesTick == 0)
            {
                FindCurrentTarget();

                if (_hasAttackTarget)
                {
                    if (_attackTarget is NonPlayerCharacter npc)
                    {
                        _moveTarget = npc.CachedTransform.position;
                    }
                }
            }
        }

        private bool HasActiveManeuver()
        {
            if (_activeManeuverState == ENPCState.Inactive)
                return false;

            if (_activeManeuver == null)
                return false;

            return true;
        }

        public void SetAnimationForManeuver(ENPCState state, int animIndex) 
        {
            var maneuverState = GetManeuverFromState(state);
            if (maneuverState != null)
            {
                var animationTriggers = maneuverState.Definition.AnimationTriggers;

                if (animIndex < 0 || animIndex >= animationTriggers.Count)
                    return;

                var animationTrigger = animationTriggers[animIndex];

                _npc.AnimationController.SetAnimationForTrigger(animationTrigger);
            }
        }

        public NonPlayerCharacterManeuverState GetManeuverFromState(ENPCState state)
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
            if (TargetPlayer != null)
            {
                if (_npc.Context.LocalPlayerCharacter == TargetPlayer)
                {
                    float distance = Vector3.Distance(TargetPlayer.CachedTransform.position, _npc.CachedTransform.position);

                    var currentManeuver = GetManeuverFromState(_npc.State.CurrentState);
                    if (currentManeuver != null)
                    {
                        if (distance < currentManeuver.Definition.AttackRange)
                        {
                            TargetPlayer.RPC_TakeHitNPC(0, currentManeuver.Definition.Damage);
                        }
                    } 
                }

                return;
             }

            if (_hasAttackTarget &&
                HasActiveManeuver())
            {
                var hitTarget = _attackTarget as IHitTarget;

                if(hitTarget != null) 
                {
                    ApplyHitToTarget(hitTarget, _activeManeuver.Definition, _npc.Context.Runner.Tick);
                }
            }
        }

        public void ApplyHitToTarget(IHitTarget hitTarget, NonPlayerCharacterManeuverDefinition definition, int tick)
        {
            FDamageData damageData = new FDamageData();
            damageData.damageValue = definition.Damage;

            FHitUtilityData hit = new FHitUtilityData
            {
                instigator = _npc,
                target = hitTarget,
                damageData = damageData,
                staggerRating = 0,
                knockbackStrength = 0,
                impactRotation = Quaternion.identity,
                impactPosition = Vector3.zero,
                tick = tick,
            };

            HitUtility.ProcessHit(ref hit, _npc.Context);
        }

        [SerializeField]
        GameObject targetGO;

        private void SetAttackTarget(IChunkTrackable target)
        {
            if (target == null)
            {
                _hasAttackTarget = false;
                targetGO = null;
                return;
            }

            _hasAttackTarget = true;

            _attackTarget = target;

            if (_attackTarget is NonPlayerCharacter npc)
                targetGO = npc.gameObject;

            if (_attackTarget is PlayerCharacter pc)
                targetGO = pc.gameObject;

            if (_attackTarget is Nexus nexus)
                targetGO = nexus.gameObject;
            
            if (_attackTarget is Stronghold stronghold)
                targetGO = stronghold.gameObject;

            if (_attackTarget is Buildable buildable)
                targetGO = buildable.gameObject;

            _moveTarget = _attackTarget.Position;
            _npc.Movement.AIFollower.destination = _moveTarget;
            UpdateRanges();
        }

        private void FindBetterLOSPosition()
        {
            Vector3 origin = _npc.CachedTransform.position;
            float checkRadius = 5f; // distance to search around current position
            float stepDegrees = 30f; // spacing of test points

            Vector3 bestSpot = origin;
            bool found = false;

            for (float angle = 0; angle < 360; angle += stepDegrees)
            {
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 candidate = origin + dir * checkRadius;

                // Keep ground level
                candidate.y = origin.y;

                if (GetLineOfSight(candidate, _attackTarget.Position))
                {
                    bestSpot = candidate;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                _moveTarget = bestSpot;
                _npc.Movement.AIFollower.destination = _moveTarget;
            }
        }

        private bool GetLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 dir = (to - from).normalized;
            float distance = Vector3.Distance(from, to);

            // Slight eye-level offset
            from.y += 1.6f;
            to.y += 1.6f;

            if (Physics.Raycast(from, dir, out RaycastHit hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // If first thing we hit is ourselves, LOS is still fine — but should never happen unless origin is inside collider
                if (hit.collider.transform.IsChildOf(_npc.gameObject.transform))
                    return true;

                // If first thing we hit is the target, LOS is fine
                if (targetGO != null && hit.collider.transform.IsChildOf(targetGO.transform))
                    return true;

                return false; // Hit something else
            }

            // No hit at all means nothing is blocking
            return true;
        }

    }
}

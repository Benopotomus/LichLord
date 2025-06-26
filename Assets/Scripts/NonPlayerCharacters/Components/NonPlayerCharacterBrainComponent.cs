using JetBrains.Annotations;
using LichLord.World;
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
        private ENonPlayerState _activeManeuverState = ENonPlayerState.Inactive;

        [SerializeField]
        private bool _hasAttackTarget = false;

        [SerializeField]
        private List<NonPlayerCharacterManeuverState> _maneuvers = new List<NonPlayerCharacterManeuverState>();

        private NonPlayerCharacterManeuverState _activeManeuver = null;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime, int tick)
        {
            if (NPC.State.CurrentState == ENonPlayerState.Inactive ||
                NPC.State.CurrentState == ENonPlayerState.Dead ||
                NPC.State.CurrentState == ENonPlayerState.HitReact)
                return;

            // Detect if an active state is running 
            // We don't want to update the target during this
            // since we need to wait for the hit event from animation
            // but we do want to rotate to the target

            UpdateExecutingManeuver(ref data, renderDeltaTime);

            if (NPC.State.CurrentState != ENonPlayerState.Idle)
                return;

            UpdateActiveManeuver(ref data, renderDeltaTime, tick);
        }

        public void OnFixedUpdate(ref FNonPlayerCharacterData data, int tick)
        {
            UpdateExecutingTimer(ref data, tick);

            // Modify the tick by the GUID so not everyone updates at once
            tick += data.GUID;

            UpdateRangesTick(tick);
            UpdateMoveSpeedTick(ref data, tick);
            UpdateDestinationTick(tick);

            // We only tick if we're idle and ready
            if (data.State != ENonPlayerState.Idle)
                return;

            UpdateSenses(tick);
            SelectManeuver(tick);
            UpdateWanderMovement();
        }

        public void UpdateExecutingTimer(ref FNonPlayerCharacterData data, int tick)
        {
            var executingManuever = GetManeuverFromState(data.State);

            if (executingManuever == null)
                return;

            if (executingManuever.HasExpired(tick))
            {
                // if the target is no longer valid, search for a new one
                FindCurrentTarget();

                // Im removing this maneuver immediatly 
                SetActiveManuever(null);
                SelectManeuver(tick);

                data.State = ENonPlayerState.Idle;
                NPC.Replicator.UpdateNPCData(data);
                return;
            }

            executingManuever.UpdateManeuverTick(_npc, ref data, tick);
        }

        private void UpdateRangesTick(int tick)
        {
            if (tick % _updateRangesTick != 0)
                return;

            UpdateRanges();
        }

        private void UpdateMoveSpeedTick(ref FNonPlayerCharacterData data, int tick)
        {
            if (tick % _updateSpeedTick != 0)
                return;

            if (!_isInFaceTargetRange)
            {
                NPC.Movement.SetFollowerUpdateRotation(true);
                NPC.Movement.SetFollowerMaxSpeed(NPC.GetDefinition(ref data).WalkSpeed);
            }
            else
            {
                NPC.Movement.SetFollowerUpdateRotation(false);
                NPC.Movement.SetFollowerMaxSpeed(NPC.GetDefinition(ref data).WalkSpeed * 0.6f);
            }
        }

        private void UpdateWanderMovement()
        {
            if (_hasAttackTarget)
                return;

            NPC.Movement.AIFollower.stopDistance = 0.2f;
            NPC.Movement.SetFollowerUpdatePosition(true);
            NPC.Movement.SetFollowerUpdateRotation(true);

            if (Vector3.Distance(NPC.CachedTransform.position, _moveTarget) < 3)
            {
                _moveTarget = new Vector3(
                    Random.Range(-20f, 20f),
                    0f, // Keep Y fixed
                    Random.Range(-20f, 20f)
                );

                NPC.Movement.AIFollower.destination = _moveTarget;
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
            if (_activeManeuver == null)
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
            if (newManeuver == null)
                _activeManeuverState = ENonPlayerState.Inactive;
            else
                _activeManeuverState = newManeuver.ActiveState;

            _activeManeuver = newManeuver;
        }

        // Runs when active meneuver is executed (in state)
        private void UpdateExecutingManeuver(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            var executingManuever = GetManeuverFromState(data.State);

            if (executingManuever == null)
                return;

            if (_hasAttackTarget)
            {
                NPC.Movement.SetFollowerUpdateRotation(false);
                RotateTowardTarget(_attackTarget.Position, renderDeltaTime);
            }
        }

        private void UpdateRanges()
        {
            if (!HasActiveManeuver() ||
                !_hasAttackTarget)
                return;

            float sqrDist = (NPC.CachedTransform.position - _attackTarget.Position).sqrMagnitude;
            _isInMovementStopRange = sqrDist < _activeManeuver.Definition.MovementStopRangeSqrt;
            _isInFaceTargetRange = sqrDist < _activeManeuver.Definition.FaceTargetRangeSqrt;
        }

        private void UpdateActiveManeuver(ref FNonPlayerCharacterData data, float renderDeltaTime, int tick)
        {
            if (!HasActiveManeuver() ||
                !_hasAttackTarget ||
                !IsTargetValid(_attackTarget))
                return;

            if (_isInFaceTargetRange)
            { 
                Vector3 attackTargetPosition = _attackTarget.Position;

                NPC.Movement.SetFollowerUpdateRotation(false);
                RotateTowardTarget(attackTargetPosition, renderDeltaTime);

                float angle = GetAngleToTarget(attackTargetPosition);

                if (_isInMovementStopRange)
                {
                    if (angle < 5f)
                    {
                        _activeManeuver.ExecuteManeuver(NPC, ref data, tick);
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
            Vector3 delta = NPC.Movement.AIFollower.destination - targetPosition;
            if (delta.sqrMagnitude < 0.01f)
                return;

            _moveTarget = targetPosition;
            NPC.Movement.AIFollower.destination = _moveTarget;
        }

        public void FindCurrentTarget()
        {
            // Get current + nearby chunks
            List<Chunk> chunks = NPC.Context.ChunkManager.GetNearbyChunks(NPC.CurrentChunk.ChunkID);

            float closestDistance = Mathf.Infinity;
            IChunkTrackable currentTarget = null;

            foreach (var chunk in chunks)
            {
                var trackables = chunk.Trackables;

                for (int i = 0; i < trackables.Count; i++)
                {
                    IChunkTrackable trackable = trackables[i];

                    if (!IsTargetValid(trackable))
                        continue;

                    float distance = Vector3.Distance(NPC.CachedTransform.position, trackable.Position);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        currentTarget = trackable;
                    }
                }
            }

            SetAttackTarget(currentTarget);           
        }

        [SerializeField]
        GameObject targetGO;

        private bool IsTargetValid(IChunkTrackable trackable)
        {
            if (trackable == null) 
                return false;

            if (!trackable.IsAttackable)
                return false;

            if (trackable is NonPlayerCharacter targetNPC)
            {
                if (targetNPC.State.CurrentState == ENonPlayerState.Inactive ||
                    targetNPC.State.CurrentState == ENonPlayerState.Dead)
                    return false;

                if (targetNPC.TeamID == NPC.TeamID)
                    return false;
            }

            return true;
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
            return _activeManeuverState != ENonPlayerState.Inactive;
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

                NPC.AnimationController.SetAnimationForTrigger(animationTrigger);
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
            if (_hasAttackTarget &&
                HasActiveManeuver())
            { 
                NonPlayerCharacter npc = _attackTarget as NonPlayerCharacter;
                if(npc != null) 
                {
                    npc.Replicator.ApplyDamage(npc.GUID, _activeManeuver.Definition.Damage);
                }
            }
        }

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

            _moveTarget = _attackTarget.Position;
            NPC.Movement.AIFollower.destination = _moveTarget;
            UpdateRanges();
        }

    }
}

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
        public float DistanceToAttackTarget;

        private IChunkTrackable _harvestTarget;
        public IChunkTrackable HarvestTarget => _harvestTarget;
        public float DistanceToHarvestTarget;

        private IChunkTrackable _depositTarget;
        public IChunkTrackable DepositTarget => _depositTarget;
        public float DistanceToDepositTarget;

        // Modulus of ticks x/32
        private int _updateSensesTick = 32; // 1 per second
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

        [SerializeField]
        private bool _hasHarvestTarget = false;

        [SerializeField]
        private bool _hasDepositTarget = false;

        [SerializeField] private bool _hasLineOfSight = false;

        [SerializeField]
        private List<NonPlayerCharacterManeuverState> _maneuvers = new List<NonPlayerCharacterManeuverState>();

        private NonPlayerCharacterManeuverState _activeManeuver = null;

        public PlayerCharacter TargetPlayer;

        [SerializeField] 
        private LayerMask _losLayerMask;

        [SerializeField]
        GameObject attackTargetGO;

        [SerializeField]
        GameObject harvestTargetGO;

        [SerializeField]
        GameObject depositTargetGO;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            _isInMovementStopRange = false;
            _isInFaceTargetRange = false;
            _hasAttackTarget = false;
            _hasHarvestTarget = false;
            _wanderPositionSet = false;
            _activeManeuverState = ENPCState.Inactive;
            _moveTarget = Vector3.zero;
        }

        public void StartRecycle()
        {
            _hasAttackTarget = false;
            _hasHarvestTarget = false;
            _hasDepositTarget = false;
            _wanderPositionSet = false;
            _moveTarget = Vector3.zero;
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
            if (HasActiveManeuver())
            {
                _wanderPositionSet = false;
                return;
            }

            if (runtimeState.IsInvader())
            {
                InvasionManager invasionManager = NPC.Context.InvasionManager;

                if (invasionManager.InvasionID == 0)
                    return;

                Vector3 formationOffset = runtimeState.GetInvaderFormationOffset();
                _moveTarget = NPC.Context.InvasionManager.GetInvasionTargetPosition(formationOffset);
                _wanderPositionSet = true;
            }
            else if (runtimeState.IsWarrior())
            {
                PlayerCharacter pc = runtimeState.GetFollowPlayer();

                if (pc != null)
                {
                    Vector3 direction = pc.CachedTransform.forward;


                    Vector3 formationOffset = runtimeState.GetInvaderFormationOffset();
                    formationOffset.z += 20f;

                    _moveTarget = pc.Formation.GetFormationPosition(runtimeState.GetFormationID(),
                        runtimeState.GetFormationIndex());
                    //_wanderPositionSet = true;
                }
            }
            else if (runtimeState.IsWorker())
            {
                if (_wanderPositionSet)
                    return;

                Crypt crypt = _npc.Context.WorkerManager.GetCrypt(runtimeState.GetWorkerIndex());

                if (crypt != null)
                {
                    _moveTarget = crypt.CachedTransform.position;
                    _wanderPositionSet = true;
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
            }

            _npc.Movement.SetMoveTargetPosition(_moveTarget);
        }

        private void SelectManeuver(int tick)
        {
            // Drop invalid active maneuver
            if (HasActiveManeuver())
            {
                if (!_activeManeuver.CanBeSelected(this, tick))
                    SetActiveManuever(null);
            }

            // Gather valid maneuvers
            List<NonPlayerCharacterManeuverState> availableStates = new List<NonPlayerCharacterManeuverState>();
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

            // Step 1: find nearest maneuver type
            EManeuverType nearestTargetType = EManeuverType.None;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < availableStates.Count; i++)
            {
                var maneuver = availableStates[i];
                float dist = DistanceToManeuverTarget(maneuver.Definition.ManeuverType);

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    nearestTargetType = maneuver.Definition.ManeuverType;
                }
            }

            // Step 2: build a list of maneuvers of that type
            List<NonPlayerCharacterManeuverState> filteredStates = new List<NonPlayerCharacterManeuverState>();
            if (nearestTargetType != EManeuverType.None)
            {
                for (int i = 0; i < availableStates.Count; i++)
                {
                    if (availableStates[i].Definition.ManeuverType == nearestTargetType)
                        filteredStates.Add(availableStates[i]);
                }
            }

            // Step 3: if none matched, fall back to all available
            List<NonPlayerCharacterManeuverState> finalStates =
                filteredStates.Count > 0 ? filteredStates : availableStates;

            // Step 4: roll randomly within that type
            int selectedIndex = Random.Range(0, finalStates.Count);
            SetActiveManuever(finalStates[selectedIndex]);
        }

        private float DistanceToManeuverTarget(EManeuverType maneuverType)
        {
            switch (maneuverType)
            {
                case EManeuverType.Deposit:
                    return _hasDepositTarget ? DistanceToDepositTarget : float.MaxValue;

                case EManeuverType.Harvest:
                    return _hasHarvestTarget ? DistanceToHarvestTarget : float.MaxValue;

                case EManeuverType.Attack:
                    return _hasAttackTarget ? DistanceToAttackTarget : float.MaxValue;

                default:
                    return float.MaxValue; // No valid target
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
            IChunkTrackable currentTarget = GetTargetForActiveManeuver();

            if (currentTarget == null)
                return;

            _npc.Movement.SetFollowerUpdateRotation(false);
            RotateTowardTarget(currentTarget.Position, renderDeltaTime);         
        }

        private void UpdateRanges()
        {
            _isInMovementStopRange = false;
            _isInFaceTargetRange = false;

            float movementStopRange = 1 * 1;
            float faceTargetRange = 20 * 20;
            float sqrDist = 40 * 40;

            if (HasActiveManeuver())
            {
                IChunkTrackable currentTarget = GetTargetForActiveManeuver();
                if (currentTarget == null)
                {
                    _isInMovementStopRange = false;
                    _isInFaceTargetRange = false;
                    return;
                }

                movementStopRange = _activeManeuver.Definition.MovementStopRangeSqrt;
                faceTargetRange = _activeManeuver.Definition.FaceTargetRangeSqrt;
                Collider targetCollider = currentTarget.HurtBoxCollider;

                if (targetCollider != null) // Target has a collider
                {
                    // Distance from my position to the closest point on target's collider
                    Vector3 closestPoint = targetCollider.ClosestPoint(_npc.CachedTransform.position);
                    sqrDist = (_npc.CachedTransform.position - closestPoint).sqrMagnitude;
                }
                else
                {
                    // Standard distance check with bonus radius
                    sqrDist = (_npc.CachedTransform.position - currentTarget.Position).sqrMagnitude;
                }
            }
            else
            {
                _isInMovementStopRange = false;
                _isInFaceTargetRange = false;
                return;

                //sqrDist = (_npc.CachedTransform.position - _moveTarget).sqrMagnitude;
            }

            _isInMovementStopRange = sqrDist < movementStopRange;
            _isInFaceTargetRange = sqrDist < faceTargetRange;
        }

        private void UpdateActiveManeuver(NonPlayerCharacterRuntimeState runtimeState, float renderDeltaTime, int tick)
        {
            if (!HasActiveManeuver())
                return;

            IChunkTrackable currentTarget = GetTargetForActiveManeuver();
            if (currentTarget == null)
                return;

            Vector3 targetPosition = currentTarget.Position;

            if (_isInFaceTargetRange)
            {
                Vector3 attackTargetPosition = targetPosition;

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

            if (!HasActiveManeuver())
                return;

            switch (_activeManeuver.Definition.ManeuverType)
            {
                case EManeuverType.Attack:
                    if (_hasAttackTarget)
                        _moveTarget = _attackTarget.Position;
                    break;
                case EManeuverType.Harvest:
                    if (_hasHarvestTarget)
                        _moveTarget = _harvestTarget.Position;
                    break;
                case EManeuverType.Deposit:
                    if (_hasDepositTarget)
                        _moveTarget = _depositTarget.Position;
                    break;
            }

            if (_isInFaceTargetRange)
            {
                _hasLineOfSight = HasLineOfSight(_npc.CachedTransform.position, _moveTarget);

                if (!_hasLineOfSight)
                    FindBetterLOSPosition(_moveTarget);
            }

            if (_isInMovementStopRange)
            {
                if(_hasLineOfSight)
                    _moveTarget = _npc.CachedTransform.position;
            }

            _npc.Movement.SetMoveTargetPosition(_moveTarget);
        }

        public void FindCurrentTargets()
        {
            if(_npc.CurrentChunk == null) return;

            // Get current + nearby chunks
            
            float closestAttackTargetDistance = Mathf.Infinity;
            float closestHarvestTargetDistance = Mathf.Infinity;
            float closestDepositTargetDistance = Mathf.Infinity;

            IChunkTrackable currentAttackTarget = null;
            IChunkTrackable currentHarvestTarget = null;
            IChunkTrackable currentDepositTarget = null;

            bool isWorker = _npc.RuntimeState.IsWorker();

            foreach (var chunk in _npc.CachedChunks)
            {
                var trackables = chunk.Trackables;

                for (int i = 0; i < trackables.Count; i++)
                {
                    IChunkTrackable trackable = trackables[i];

                    if (IsAttackTargetValid(trackable))
                    {
                        float sqrDistance = Vector3.SqrMagnitude(_npc.CachedTransform.position - trackable.Position);

                        if (sqrDistance < closestAttackTargetDistance)
                        {
                            closestAttackTargetDistance = sqrDistance;
                            currentAttackTarget = trackable;
                        }
                    }

                    if (isWorker)
                    {
                        if (IsHarvestTargetValid(trackable))
                        {
                            float sqrDistance = Vector3.SqrMagnitude(_npc.CachedTransform.position - trackable.Position);

                            if (sqrDistance < closestHarvestTargetDistance)
                            {
                                closestHarvestTargetDistance = sqrDistance;
                                currentHarvestTarget = trackable;
                            }
                        }

                        if (IsDepositTargetValid(trackable))
                        {
                            float sqrDistance = Vector3.SqrMagnitude(_npc.CachedTransform.position - trackable.Position);

                            if (sqrDistance < closestDepositTargetDistance)
                            {
                                closestDepositTargetDistance = sqrDistance;
                                currentDepositTarget = trackable;
                            }
                        }
                    }
                }
            }

            DistanceToAttackTarget = closestAttackTargetDistance;
            SetAttackTarget(currentAttackTarget);

            if (isWorker)
            {
                DistanceToHarvestTarget = closestHarvestTargetDistance;
                SetHarvestTarget(currentHarvestTarget);

                DistanceToDepositTarget = closestDepositTargetDistance;
                SetDepositTarget(currentDepositTarget);
            }
        }

        private bool IsAttackTargetValid(IChunkTrackable trackable)
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

        private bool IsHarvestTargetValid(IChunkTrackable trackable)
        {
            if (trackable == null)
                return false;

            if (trackable is HarvestNode harvestNode)
            {
                if (harvestNode.RuntimeState.GetHarvestPoints() > 0)
                    return true;
            }

            return false;
        }

        private bool IsDepositTargetValid(IChunkTrackable trackable)
        {
            if (trackable == null)
                return false;

            // if we're not carrying anything, nothing is valid
            var currencyType = _npc.RuntimeState.GetCarriedCurrencyType();
            if (currencyType == ECurrencyType.None)
                return false;

            var currencyAmount = _npc.RuntimeState.GetCarriedCurrencyAmount();

            if (trackable is Stockpile stockpile)
            {
                int stockpileIndex = stockpile.RuntimeState.GetStockpileIndex();

                if (stockpileIndex >= 0)
                {
                    var stockpileData = _npc.Context.ContainerManager.GetStockPile(stockpileIndex);

                    if (stockpileData.CanFit(currencyType, currencyAmount))
                        return true;
                }
            }

            return false;
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
                FindCurrentTargets();
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
            var currentManeuver = GetManeuverFromState(_npc.State.CurrentState);

            if (currentManeuver == null)
                return;

            if (currentManeuver.Definition is NonPlayerCharacterAttackManeuverDefinition attackManeuverDefinition)
            {
                if (TargetPlayer != null)
                {
                    if (_npc.Context.LocalPlayerCharacter == TargetPlayer)
                    {
                        float distance = Vector3.Distance(TargetPlayer.CachedTransform.position, _npc.CachedTransform.position);

                        if (distance < attackManeuverDefinition.AttackRange)
                        {
                            TargetPlayer.RPC_TakeHitNPC(0, attackManeuverDefinition.Damage);
                        }
                    }

                    return;
                }

                var hitTarget = _attackTarget as IHitTarget;

                if (hitTarget != null)
                {
                    ApplyHitToTarget(hitTarget, attackManeuverDefinition, _npc.Context.Runner.Tick);
                }
            }
            else if(currentManeuver.Definition is NonPlayerCharacterHarvestManeuverDefinition harvestManeuverDefinition)
            {
                if (NPC.Replicator.HasStateAuthority)
                {
                    if(_harvestTarget is HarvestNode harvestNode)
                        harvestNode.ProgressHarvest(NPC);

                    FindCurrentTargets();
                }
            }
            else if (currentManeuver.Definition is NonPlayerCharacterDepositManeuverDefinition depositManeuverDefinition)
            {
                if (NPC.Replicator.HasStateAuthority)
                {
                    if (_depositTarget is Stockpile stockpile)
                        stockpile.DropOffCurrency(_npc);

                    FindCurrentTargets();
                }
            }
        }

        public void ApplyHitToTarget(IHitTarget hitTarget, NonPlayerCharacterAttackManeuverDefinition definition, int tick)
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

        private void SetAttackTarget(IChunkTrackable target)
        {
            if (target == null)
            {
                _hasAttackTarget = false;
                attackTargetGO = null;
                return;
            }

            _hasAttackTarget = true;

            _attackTarget = target;

            if (_attackTarget is NonPlayerCharacter npc)
                attackTargetGO = npc.gameObject;

            if (_attackTarget is PlayerCharacter pc)
                attackTargetGO = pc.gameObject;

            if (_attackTarget is Nexus nexus)
                attackTargetGO = nexus.gameObject;
            
            if (_attackTarget is Stronghold stronghold)
                attackTargetGO = stronghold.gameObject;

            if (_attackTarget is Buildable buildable)
                attackTargetGO = buildable.gameObject;
        }

        private void SetHarvestTarget(IChunkTrackable target)
        {
            if (target == null)
            {
                _hasHarvestTarget = false;
                return;
            }

            // harvest target changed
            if (target != _harvestTarget)
            {
                _npc.RuntimeState.SetHarvestProgress(0);
            }

            _hasHarvestTarget = true;
            _harvestTarget = target;

            if (_harvestTarget is HarvestNode harvestNode)
                harvestTargetGO = harvestNode.gameObject;
        }

        private void SetDepositTarget(IChunkTrackable target)
        {
            if (target == null)
            {
                _hasDepositTarget = false;
                return;
            }

            // deposit target changed
            if (target != _depositTarget)
            {
              
            }

            _hasDepositTarget = true;
            _depositTarget = target;

            if (_depositTarget is Stockpile stockpile)
                depositTargetGO = stockpile.gameObject;

            UpdateRanges();
        }

        private void FindBetterLOSPosition(Vector3 targetPosition)
        {
            // Search around the target instead of the NPC
            Vector3 origin = targetPosition;
            float checkRadius = 10f;   // how far from the target we’ll sample
            float stepDegrees = 30f;  // angular step

            Vector3 bestSpot = _npc.CachedTransform.position; // fallback: current position
            bool found = false;

            // Start from NPC’s direction relative to target
            Vector3 toNpc = (_npc.CachedTransform.position - targetPosition).normalized;
            Vector3 startDir = Quaternion.AngleAxis(90f, Vector3.up) * toNpc; // "right" relative to NPC-target line

            for (float angle = 0; angle < 360; angle += stepDegrees)
            {
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * startDir;
                Vector3 candidate = origin + dir * checkRadius;

                // Keep on same ground plane as NPC
                candidate.y = _npc.CachedTransform.position.y;

                // Make sure candidate has LOS back to the target
                if (HasLineOfSight(candidate, targetPosition))
                {
                    bestSpot = candidate;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                _moveTarget = bestSpot;
            }
            else
            {
                // fallback: just move toward target if no clear spot found
                _moveTarget = targetPosition;
            }
        }

        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            // Slight eye-level offset
            from.y += 1.6f;
            to.y += 1.6f;

            if (Physics.Linecast(from, to, out RaycastHit hit, _losLayerMask, QueryTriggerInteraction.Collide))
            {
                // Ignore self
                if (hit.collider.transform.IsChildOf(_npc.transform))
                    return true;

                // Target is fine
                if (attackTargetGO != null && hit.collider.transform.IsChildOf(attackTargetGO.transform))
                    return true;

                //Debug.Log($"LOS blocked by {hit.collider.name}");
                return false;
            }

            // No hit at all = nothing blocking
            return true;
        }

        public IChunkTrackable GetTargetForActiveManeuver()
        {
            if (!HasActiveManeuver())
                return null;

            IChunkTrackable currentTarget = null;

            switch (_activeManeuver.Definition.ManeuverType)
            {
                case EManeuverType.Attack:
                    if (!_hasAttackTarget || !IsAttackTargetValid(_attackTarget))
                        return null;

                    currentTarget = _attackTarget;
                    break;
                case EManeuverType.Harvest:
                    if (!_hasHarvestTarget || !IsHarvestTargetValid(_harvestTarget))
                        return null;

                    currentTarget = _harvestTarget;
                    break;
                case EManeuverType.Deposit:
                    if (!_hasDepositTarget || !IsDepositTargetValid(_depositTarget))
                        return null;

                    currentTarget = _depositTarget;
                    break;
            }

            return currentTarget;
        }
    }
}

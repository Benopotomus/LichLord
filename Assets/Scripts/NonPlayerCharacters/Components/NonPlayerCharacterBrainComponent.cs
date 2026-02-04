using LichLord.Buildables;
using LichLord.Items;
using LichLord.Props;
using LichLord.World;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterBrainComponent : MonoBehaviour
    {
        public class BrainTarget
        {
            public IChunkTrackable Target;
            public bool HasTarget;
            public float DistanceToTarget;
        }

        public BrainTarget AttackTarget;
        public BrainTarget HarvestTarget;
        public BrainTarget DepositTarget;
        public BrainTarget NullTarget;

        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField] 
        private Vector3 _moveTarget;
        [SerializeField]
        private Vector3 _losTarget;

        // Modulus of ticks x/32
        private int _updateSensesTick = 32; // 1 per second
        private int _updateDestinationTick = 8;
        private int _updateSpeedTick = 8;
        private int _updateRangesTick = 8;

        [SerializeField] 
        private bool _isInActivationRange = false;

        [SerializeField] 
        private bool _isInFaceTargetRange = false;

        [SerializeField] 
        private ENPCState _activeManeuverState = ENPCState.Inactive;

        [SerializeField] private bool _hasLineOfSight = false;

        [SerializeField]
        private List<NonPlayerCharacterManeuverState> _maneuvers = new List<NonPlayerCharacterManeuverState>();

        private NonPlayerCharacterManeuverState _activeManeuver = null;
        public NonPlayerCharacterManeuverState ActiveManuver => _activeManeuver;

        [SerializeField]
        private PlayerCharacter _targetPlayer;
        public PlayerCharacter TargetPlayer => _targetPlayer;

        [SerializeField] 
        private LayerMask _losLayerMask;

        [SerializeField]
        GameObject attackTargetGO;

        [SerializeField]
        GameObject harvestTargetGO;

        [SerializeField]
        GameObject depositTargetGO;

        public void Awake()
        {
            AttackTarget = new BrainTarget();
            AttackTarget.Target = null;
            AttackTarget.DistanceToTarget = 200f;
            AttackTarget.HasTarget = false;

            HarvestTarget = new BrainTarget();
            HarvestTarget.Target = null;
            HarvestTarget.DistanceToTarget = 200f;
            HarvestTarget.HasTarget = false;

            DepositTarget = new BrainTarget();
            DepositTarget.Target = null;
            DepositTarget.DistanceToTarget = 200f;
            DepositTarget.HasTarget = false;

            NullTarget = new BrainTarget();
            NullTarget.Target = null;
            NullTarget.DistanceToTarget = 200f;
            NullTarget.HasTarget = false;
        }

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority)
        {
            _isInActivationRange = false;
            _isInFaceTargetRange = false;
            AttackTarget.HasTarget = false;
            HarvestTarget.HasTarget = false;
            _wanderPositionSet = false;
            _activeManeuverState = ENPCState.Inactive;
            _moveTarget = Vector3.zero;
            _activeManeuver = null;

            if(hasAuthority)
                FindCurrentTargets();
        }

        public void StartRecycle()
        {
            AttackTarget.HasTarget = false;
            HarvestTarget.HasTarget = false;
            DepositTarget.HasTarget = false;
            _wanderPositionSet = false;
            _activeManeuver = null;
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
            _targetPlayer = (targetPlayerIndex > 0) ? _npc.Context.NetworkGame.GetPlayerByIndex(targetPlayerIndex) : null;

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
            if (_npc.State.CurrentState == ENPCState.Dead ||
                _npc.State.CurrentState == ENPCState.Inactive)
                return;

            int targetPlayerIndex = runtimeState.GetTargetPlayerIndex();

            _targetPlayer = (targetPlayerIndex > 0) ? _npc.Context.NetworkGame.GetPlayerByIndex(targetPlayerIndex) :  null;
        }

        int _lastTick = -1;
        private void UpdateAuthorityTick(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            if (_lastTick == tick)  
                return;

            _lastTick = tick;

            UpdateExecutingTimer(runtimeState, tick);

            // Modify the tick by the GUID so not everyone updates at once
            tick += _npc.LocalIndex;

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
            else if (runtimeState.IsCommandedUnit())
            {
                PlayerCharacter pc = runtimeState.GetFollowPlayer();

                if (pc != null)
                {
                    Vector3 direction = pc.CachedTransform.forward;

                    Vector3 formationOffset = runtimeState.GetInvaderFormationOffset();
                    formationOffset.z += 20f;

                    _moveTarget = pc.Commander.GetFormationPosition(runtimeState.GetSquadId(),
                        runtimeState.GetFormationIndex());
                    //Debug.Log("Move Target Changed: " + _moveTarget);
                    //_wanderPositionSet = true;
                }
            }
            else if (runtimeState.IsWorker())
            {
                if (_wanderPositionSet)
                    return;

                Lair stronghold = runtimeState.GetWorkerStronghold();

                if (stronghold != null)
                {
                    _moveTarget = stronghold.CachedTransform.position;
                    _wanderPositionSet = true;
                }
            }
            else
            { 
                if ((tick + _npc.LocalIndex) % 64 == 0)
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
                    return DepositTarget.HasTarget ? DepositTarget.DistanceToTarget : float.MaxValue;

                case EManeuverType.Harvest:
                    return HarvestTarget.HasTarget ? HarvestTarget.DistanceToTarget : float.MaxValue;

                case EManeuverType.Attack:
                    return AttackTarget.HasTarget ? AttackTarget.DistanceToTarget : float.MaxValue;

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
            BrainTarget currentTarget = GetTargetForActiveManeuver();

            if (!currentTarget.HasTarget)
                return;

            _npc.Movement.SetFollowerUpdateRotation(false);
            RotateTowardTarget(currentTarget.Target.Position, renderDeltaTime);         
        }

        private void UpdateRanges()
        {
            _isInActivationRange = false;
            _isInFaceTargetRange = false;

            float faceTargetRange = 20 * 20;
            float sqrDist = 40 * 40;

            if (HasActiveManeuver())
            {
                BrainTarget currentTarget = GetTargetForActiveManeuver();
                if (!currentTarget.HasTarget)
                {
                    _isInActivationRange = false;
                    _isInFaceTargetRange = false;
                    return;
                }

                faceTargetRange = _activeManeuver.Definition.FaceTargetRangeSqrt;
                var target = currentTarget.Target;
                Collider targetCollider = target.HurtBoxCollider;

                Vector3 targetPoint = target.Position;

                if (targetCollider != null && targetCollider.enabled) // Target has a collider
                {
                    targetPoint = targetCollider.ClosestPoint(_npc.Position);

                    sqrDist = (_npc.Position - targetPoint).sqrMagnitude;                
                }
                else
                {
                    // Standard distance check with bonus radius
                    sqrDist = (_npc.CachedTransform.position - targetPoint).sqrMagnitude;
                }
            }
            else
            {
                _isInActivationRange = false;
                _isInFaceTargetRange = false;
                return;
            }

            _isInActivationRange = _activeManeuver.Definition.IsInActivationRange(sqrDist);
            _isInFaceTargetRange = sqrDist < faceTargetRange;
        }

        private void UpdateActiveManeuver(NonPlayerCharacterRuntimeState runtimeState, float renderDeltaTime, int tick)
        {
            if (!HasActiveManeuver())
                return;

            var currentTarget = GetTargetForActiveManeuver();
            if (!currentTarget.HasTarget)
                return;

            Vector3 targetPosition = currentTarget.Target.Position;

            if (_isInFaceTargetRange)
            {
                Vector3 attackTargetPosition = targetPosition;

                _npc.Movement.SetFollowerUpdateRotation(false);
                RotateTowardTarget(attackTargetPosition, renderDeltaTime);

                float angle = GetAngleToTarget(attackTargetPosition);

                if (_isInActivationRange)
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
                    if (AttackTarget.HasTarget)
                    {
                        _moveTarget = _activeManeuver.Definition.GetMovementToActivationRange(NPC, AttackTarget.Target);
                        _losTarget = AttackTarget.Target.Position;
                    }
                    break;
                case EManeuverType.Harvest:
                    if (HarvestTarget.HasTarget)
                        _losTarget = _moveTarget = HarvestTarget.Target.Position;
                    break;
                case EManeuverType.Deposit:
                    if (DepositTarget.HasTarget)
                        _losTarget = _moveTarget = DepositTarget.Target.Position;
                    break;
            }

            if (_isInFaceTargetRange)
            {
                _hasLineOfSight = HasLineOfSight(_npc.CachedTransform.position, _losTarget);

                if (!_hasLineOfSight)
                    FindBetterLOSPosition(_losTarget);
            }

            if (_isInActivationRange)
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
            
            FItemData carriedItem = _npc.CarriedItem.CarriedItem;
            
            bool hasCommandTargetNode = false;

            if (isWorker)
            {
                var commandTargetNode = GetCommandTargetNode();
                IChunkTrackable commandTarget = commandTargetNode.Item2;

                if (IsHarvestTargetValid(commandTarget, carriedItem))
                {
                    hasCommandTargetNode = true;
                    currentHarvestTarget = commandTarget;
                }
            }

            foreach (var chunk in _npc.CachedChunks)
            {
                var trackables = chunk.Trackables;

                if (isWorker)
                {
                    for (int i = 0; i < trackables.Count; i++)
                    {
                        IChunkTrackable trackable = trackables[i];

                        if (!hasCommandTargetNode)
                        {
                            if (IsHarvestTargetValid(trackable, carriedItem))
                            {
                                float sqrDistance = Vector3.SqrMagnitude(_npc.CachedTransform.position - trackable.Position);

                                if (sqrDistance < closestHarvestTargetDistance)
                                {
                                    closestHarvestTargetDistance = sqrDistance;
                                    currentHarvestTarget = trackable;
                                }
                            }
                        }

                        if (IsDepositTargetValid(trackable, carriedItem))
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

                var hitTargets = chunk.HitTargets;

                for (int i = 0; i < hitTargets.Count; i++)
                {
                    IChunkTrackable trackable = hitTargets[i].ChunkTrackable;

                    if (IsAttackTargetValid(trackable))
                    {
                        float sqrDistance = Vector3.SqrMagnitude(_npc.CachedTransform.position - trackable.Position);

                        if (sqrDistance < closestAttackTargetDistance)
                        {
                            closestAttackTargetDistance = sqrDistance;
                            currentAttackTarget = trackable;
                        }
                    }
                }
            }

            AttackTarget.DistanceToTarget = closestAttackTargetDistance;
            SetAttackTarget(currentAttackTarget);

            if (isWorker)
            {
                HarvestTarget.DistanceToTarget = closestHarvestTargetDistance;
                SetHarvestTarget(currentHarvestTarget);

                DepositTarget.DistanceToTarget = closestDepositTargetDistance;
                SetDepositTarget(currentDepositTarget);
            }
        }

        private (bool, IChunkTrackable) GetCommandTargetNode()
        {
            // If we have a current harvest target
            var targetPosition = _npc.RuntimeState.GetWorkerTargetNodePosition();

            if (!targetPosition.Item1)
                return (false, null);

            Chunk chunk = _npc.Context.ChunkManager.GetChunk(targetPosition.Item2.ChunkPosition);

            if (chunk == null)
                return (false, null);

            FPropLoadState loadstate = chunk.PropLoadStates[targetPosition.Item2.PropIndex];

            if (loadstate.LoadState != ELoadState.Loaded)
                return (false, null);

            return (true, loadstate.Prop);
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

            if (trackable is Lair strongHold)
            {
                if (_npc.TeamID == ETeamID.PlayerTeam)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsHarvestTargetValid(IChunkTrackable trackable, FItemData carriedItem)
        {
            if (trackable == null)
                return false;

            // if we're carrying anything, nothing is valid
            if (carriedItem.IsValid())
                return false;

            if (trackable is HarvestNode harvestNode)
            {
                var harvestNodeRuntimeState = harvestNode.RuntimeState;
                if (harvestNodeRuntimeState.GetHarvestPoints() <= 0)
                    return false;

                 return(_npc.RuntimeState.IsHarvestNodeValid(harvestNodeRuntimeState));
            }

            return false;
        }

        private bool IsDepositTargetValid(IChunkTrackable trackable, FItemData carriedItem)
        {
            if (trackable == null)
                return false;

            // if we're not carrying anything, nothing is valid
            if (!carriedItem.IsValid())
                return false;

            if (trackable is Stockpile stockpile)
            {
                int containerIndex = stockpile.RuntimeState.GetContainerIndex();

                if (containerIndex >= 0)
                {
                    ContainerManager containerManager = NPC.Context.ContainerManager;

                    var containerData = containerManager.GetContainerDataAtIndex(containerIndex);

                    if (containerManager.CanStackAndFitContainer(containerIndex, carriedItem))
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

            var definition = currentManeuver.Definition;
            if (definition == null)
                return;

            definition.ExecuteHitEvents(_npc, GetTargetForActiveManeuver().Target);
        }

        public void OnSpecialEventFromAnimation()
        {
            var currentManeuver = GetManeuverFromState(_npc.State.CurrentState);
            if (currentManeuver == null)
                return;

            var definition = currentManeuver.Definition;
            if (definition == null)
                return;

            definition.ExecuteSpecialEvents(_npc, GetTargetForActiveManeuver().Target);
        }

        public void OnSweepChangeFromAnimation(bool isSweeping)
        {
            var currentManeuver = GetManeuverFromState(_npc.State.CurrentState);
            if (currentManeuver == null)
                return;

            var definition = currentManeuver.Definition;
            if (definition == null)
                return;

            
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
                AttackTarget.HasTarget = false;
                attackTargetGO = null;
                return;
            }

            AttackTarget.HasTarget = true;

            AttackTarget.Target = target;

            if (target is NonPlayerCharacter npc)
                attackTargetGO = npc.gameObject;

            if (target is PlayerCharacter pc)
                attackTargetGO = pc.gameObject;

            if (target is Nexus nexus)
                attackTargetGO = nexus.gameObject;
            
            if (target is Lair stronghold)
                attackTargetGO = stronghold.gameObject;

            if (target is Buildable buildable)
                attackTargetGO = buildable.gameObject;
        }

        private void SetHarvestTarget(IChunkTrackable target)
        {
            if (target == null)
            {
                HarvestTarget.HasTarget = false;
                return;
            }

            // harvest target changed
            if (target != HarvestTarget.Target)
            {
                _npc.RuntimeState.SetHarvestProgress(0);
            }

            HarvestTarget.HasTarget = true;
            HarvestTarget.Target = target;

            if (target is HarvestNode harvestNode)
                harvestTargetGO = harvestNode.gameObject;
        }

        private void SetDepositTarget(IChunkTrackable target)
        {
            if (target == null)
            {
                DepositTarget.HasTarget = false;
                return;
            }

            DepositTarget.HasTarget = true;
            DepositTarget.Target = target;

            if (target is Stockpile stockpile)
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

        public BrainTarget GetTargetForActiveManeuver()
        {
            if (!HasActiveManeuver())
                return NullTarget;

            FItemData carriedItem = _npc.CarriedItem.CarriedItem;

            switch (_activeManeuver.Definition.ManeuverType)
            {
                case EManeuverType.Attack:
                    if (!AttackTarget.HasTarget || !IsAttackTargetValid(AttackTarget.Target))
                        return NullTarget;

                    return AttackTarget;
                case EManeuverType.Harvest:
                    if (!HarvestTarget.HasTarget || !IsHarvestTargetValid(HarvestTarget.Target, carriedItem))
                        return NullTarget;

                    return HarvestTarget;
                case EManeuverType.Deposit:
                    if (!DepositTarget.HasTarget || !IsDepositTargetValid(DepositTarget.Target, carriedItem))
                        return NullTarget;

                    return DepositTarget;
            }

            return NullTarget;
        }

        // 1. Classic Gizmos way (shows in Scene view when the object is selected)
        void OnDrawGizmos()
        {
            // Only draw when we actually have a meaningful move target
            if (_moveTarget.sqrMagnitude > 0.1f) // rough check that it's not zero
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_moveTarget, 0.7f);           // outer ring
                Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
                Gizmos.DrawSphere(_moveTarget, 0.7f);               // semi-transparent filled sphere

                // Optional: line from NPC to target
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _moveTarget);
            }
        }
    }
}

using DWD.Pooling;
using Fusion;
using LichLord.Buildables;
using LichLord.Items;
using LichLord.Projectiles;
using LichLord.Props;
using LichLord.World;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{

    public class NonPlayerCharacter : DWDObjectPoolObject, IHitTarget, IHitInstigator, INetActor, IChunkTrackable
    {
        private NonPlayerCharacterRuntimeState _runtimeState;
        public NonPlayerCharacterRuntimeState RuntimeState => _runtimeState;

        protected NonPlayerCharacterReplicator _replicator;
        public NonPlayerCharacterReplicator Replicator => _replicator;

        [SerializeField] private NonPlayerCharacterMovementComponent _movementComponent;
        public NonPlayerCharacterMovementComponent Movement => _movementComponent;

        [SerializeField] private NonPlayerCharacterStateComponent _stateComponent;
        public NonPlayerCharacterStateComponent State => _stateComponent;

        [SerializeField] private NonPlayerCharacterBrainComponent _brainComponent;
        public NonPlayerCharacterBrainComponent Brain => _brainComponent;

        [SerializeField] private NonPlayerCharacterHitReactComponent _hitReactComponent;
        public NonPlayerCharacterHitReactComponent HitReact => _hitReactComponent;

        [SerializeField] private NonPlayerCharacteHealthComponent _healthComponent;
        public NonPlayerCharacteHealthComponent Health => _healthComponent;

        [SerializeField] private NonPlayerCharacterWeaponsComponent _weaponsComponent;
        public NonPlayerCharacterWeaponsComponent Weapons => _weaponsComponent;

        [SerializeField] private NonPlayerCharacterAnimationController _animationController;
        public NonPlayerCharacterAnimationController AnimationController => _animationController;

        [SerializeField] private NonPlayerCharacterCarriedItemComponent _currencyComponent;
        public NonPlayerCharacterCarriedItemComponent CurrencyComponent => _currencyComponent;

        [SerializeField] private NonPlayerCharacterAttitudeComponent _attitudeComponent;
        public NonPlayerCharacterAttitudeComponent AttitudeComponent => _attitudeComponent;

        [SerializeField] private NonPlayerCharacterDialogComponent _dialogComponent;
        public NonPlayerCharacterDialogComponent DialogComponent => _dialogComponent;

        [SerializeField] private NonPlayerCharacterSpawningComponent _spawningComponent;
        public NonPlayerCharacterSpawningComponent SpawningComponent => _spawningComponent;

        [SerializeField] private NonPlayerCharacterLifetimeComponent _lifetimeComponent;
        public NonPlayerCharacterLifetimeComponent LifetimeComponent => _lifetimeComponent;

        [SerializeField]
        private InteractableComponent _interactableComponent;
        public InteractableComponent Interactable => _interactableComponent;

        [SerializeField] private MuzzleComponent _muzzleComponent;
        public MuzzleComponent Muzzle => _muzzleComponent;

        [SerializeField] private Animator _animator;
        public Animator Animator => _animator;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        [SerializeField] private HurtboxComponent _hurtbox;
        public HurtboxComponent Hurtbox => _hurtbox;

        [SerializeField] private CapsuleCollider _collider;
        public CapsuleCollider Collider => _collider;

        private SceneContext _context;
        public SceneContext Context => _context;

        public INetActor NetActor => this;

        private FNetObjectID _netObjectID = new FNetObjectID();
        public FNetObjectID NetObjectID => _netObjectID;

        public float BonusRadius { get { return 1; } }

        [SerializeField]
        private int _localIndex;
        public int LocalIndex => _localIndex;

        [SerializeField]
        private int _fullIndex;
        public int FullIndex => _fullIndex;

        private ETeamID _teamId;
        public ETeamID TeamID => _teamId;

        // Cached list of Chunks for current and neighboring chunks
        private List<Chunk> _cachedChunks = new List<Chunk>();
        public IReadOnlyList<Chunk> CachedChunks => _cachedChunks.AsReadOnly();

        // IChunkTrackable
        private Chunk _currentChunk;
        public Chunk CurrentChunk { get => _currentChunk; set => _currentChunk = value; }
        public virtual Collider HurtBoxCollider { get { return Hurtbox.HurtBoxes[0]; } }

        public Vector3 Position => CachedTransform.position;
        public bool IsAttackable 
        { 
            get 
            {
                switch (_stateComponent.CurrentState)
                {
                    case ENPCState.Dead:
                    case ENPCState.Inactive:
                        return false;
                    default:
                        return true;
                }
            } 
        }

        [Header("Worker Data")]
        [SerializeField]
        private int _strongholdId = -1;
        public int StrongholdId => _strongholdId;

        [SerializeField]
        private Stronghold _stronghold;
        public Stronghold Stronghold => _stronghold;

        [SerializeField]
        private int _workerIndex = -1;
        public int WorkerIndex => _workerIndex;

        [SerializeField]
        private GameObject redHat;

        [SerializeField]
        private GameObject blueHat;

        [SerializeField]
        private int _formationId;

        [SerializeField]
        private int _formationIndex;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, NonPlayerCharacterReplicator replicator, bool hasAuthority, int tick)
        {
            _runtimeState = runtimeState;
            _context = replicator.Context;
            _replicator = replicator;
            _movementComponent.OnSpawned(runtimeState);
            _brainComponent.OnSpawned(runtimeState, hasAuthority);
            _currencyComponent.OnSpawned();
            _attitudeComponent.OnSpawned(runtimeState);
            _dialogComponent.OnSpawned(runtimeState);
            _lifetimeComponent.OnSpawned(runtimeState, tick);
            _spawningComponent.OnSpawned(runtimeState);
            _stateComponent.OnSpawned(runtimeState, hasAuthority, tick);

            _interactableComponent.Activate(
                this,
                IsPotentialInteractor,
                IsInteractionValid,
                GetInteractionText,
                GetTicksToComplete,
                GetInteractType,
                GetInteractDistance
            );

            _interactableComponent.onInteractStart += OnInteractStart;
            _interactableComponent.onInteractEnd += OnInteractEnd;
            _interactableComponent.onInteractionComplete += OnInteractionComplete;

            _localIndex = runtimeState.LocalIndex;
            _fullIndex = runtimeState.FullIndex;

            UpdateChunk(_context.ChunkManager);

            _netObjectID.SetObjectType(EObjectType.NonPlayerCharacter);
            _netObjectID.SetIndex(runtimeState.LocalIndex);

            if (runtimeState.IsWorker() && runtimeState.IsWorkerValid())
            {
                UpdateWorkerData();
                _stronghold.WorkerComponent.AddWorkerCharacter(this, _workerIndex);
            }
            
            if (runtimeState.IsWarrior())
            {
                var pc = runtimeState.GetFollowPlayer();

                if (pc != null)
                {
                    pc.Formation.AddCharacter(this, 
                        runtimeState.GetFormationID(), 
                        runtimeState.GetFormationIndex());
                }
            }
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState, 
            bool hasAuthority, 
            float renderDeltaTime, 
            int tick)
        {
            _runtimeState = runtimeState;

            UpdateChunk(_context.ChunkManager);
            UpdateTeam(runtimeState);
            _stateComponent.UpdateState(runtimeState, hasAuthority, tick);
            _currencyComponent.OnRender(runtimeState);
            _attitudeComponent.OnRender( runtimeState);
            _dialogComponent.OnRender(runtimeState);
            _lifetimeComponent.UpdateLifetime(runtimeState, hasAuthority, tick);

            _formationId = runtimeState.GetFormationID();
            _formationIndex = runtimeState.GetFormationIndex();

            if (hasAuthority)
            {
                _movementComponent.AuthorityUpdate(runtimeState, renderDeltaTime, tick);
                _brainComponent.AuthorityUpdate(runtimeState, renderDeltaTime, tick);

                if (runtimeState.IsWorker() && runtimeState.IsWorkerValid())
                {
                    var workerData = _stronghold.WorkerComponent.GetWorkerData(_workerIndex);
                    if (!workerData.IsAssigned)
                    {
                        switch (runtimeState.GetState())
                        {
                            case ENPCState.Dead:
                            case ENPCState.Inactive:
                                break;
                            default:
                                Debug.Log("setting state to dead");
                                _runtimeState.SetState(ENPCState.Dead);
                                break;
                        }

                        return;
                    }
                }
            }
            else
            {
                _brainComponent.RemoteUpdate(runtimeState);
                _movementComponent.RemoteUpdate(runtimeState, renderDeltaTime, tick);
            }
        }

        public void UpdateWorkerData()
        {
            if (!RuntimeState.IsWorker())
                return;

            _strongholdId = RuntimeState.GetWorkerStrongholdId();
            _workerIndex = RuntimeState.GetWorkerIndex();

            if(_strongholdId >= 0)
                _stronghold = Context.StrongholdManager.GetStronghold(_strongholdId);
        }

        private void UpdateTeam(NonPlayerCharacterRuntimeState runtimeState)
        {
            ETeamID newTeam = runtimeState.GetTeam();

            if (_teamId == newTeam)
                return;

            if (redHat != null && blueHat != null)
            {
                switch (newTeam)
                {
                    case ETeamID.EnemiesTeamA:
                        redHat.SetActive(false);
                        blueHat.SetActive(true);
                        break;
                    case ETeamID.EnemiesTeamB:
                        blueHat.SetActive(false);
                        redHat.SetActive(true);
                        break;
                }
            }

            _teamId = newTeam;
        }

        public void ProcessHit(ref FHitUtilityData hit) { }

        public void OnHitTaken(ref FHitUtilityData hit) { }

        void INetActor.ProjectileSpawnedCallback(Projectile projectile, ProjectileDefinition definition, ref FProjectileData data)
        {
            if (Replicator.HasStateAuthority)
                return;

            if (definition == null)
                return;

            if (!definition.ForcesRemoteAiming)
                return;

            if (projectile == null) 
                return;

            AnimationController.SetProjectileFrame(definition);
        }

        void IHitInstigator.HitPerformed(ref FHitUtilityData hit)
        {
            NetworkRunner runner = Context.Runner;
            if (hit.target is NonPlayerCharacter npc)
            {
                int currentAnimIndex = npc.State.CurrentAnimIndex;
                int hitReactIndex = Random.Range(0, 4);

                // If the new index is the same as the current, increment and wrap around
                if (hitReactIndex == currentAnimIndex)
                {
                    hitReactIndex = (currentAnimIndex + 1) % 4;
                }

                npc.Replicator.RPC_DealDamageToNPC(npc.LocalIndex, hit.damageData.damageValue, hitReactIndex);

                if (!runner.IsSharedModeMasterClient)
                    npc.Replicator.Predict_DealDamageToNPC(npc.LocalIndex, hit.damageData.damageValue, hitReactIndex);
            }
            else if (hit.target is Prop prop)
            {
                Context.PropManager.RPC_DealDamage(prop.RuntimeState.chunk.ChunkID, prop.RuntimeState.index, hit.damageData.damageValue);

                if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                    Context.PropManager.Predict_DealDamage(prop.RuntimeState.chunk.ChunkID, prop.RuntimeState.index, hit.damageData.damageValue);
            }
            else if (hit.target is Stronghold stronghold)
            {
                stronghold.RPC_DealDamage(hit.damageData.damageValue);
            }
            else if (hit.target is PlayerCharacter pc)
            {
                if (pc.HasStateAuthority)
                {

                }
            }
            else if (hit.target is Buildable buildable)
            {
                buildable.Zone.RPC_DealDamage(buildable.RuntimeState.Index, hit.damageData.damageValue);
            }
        }

        public void UpdateChunk(ChunkManager chunkManager)
        {
            var lastChunk = CurrentChunk;
            var newChunk = chunkManager.GetChunkAtPosition(CachedTransform.position);

            if (lastChunk != newChunk)
            {
                _currentChunk = newChunk;

                if(_currentChunk != null)
                    _cachedChunks = _context.ChunkManager.GetNearbyChunks(_currentChunk.ChunkID);

                if (lastChunk != null)
                    lastChunk.RemoveObject(this);

                if (newChunk != null)
                    newChunk.AddObject(this);
            }
        }

        public void StartRecycle()
        {
            _interactableComponent.onInteractStart -= OnInteractStart;
            _interactableComponent.onInteractEnd -= OnInteractEnd;
            _interactableComponent.onInteractionComplete -= OnInteractionComplete;

            Hurtbox.SetHitBoxesActive(false);
            _movementComponent.StartRecycle();
            _brainComponent.StartRecycle();
            _stateComponent.StartRecycle();
            DWDObjectPool.Instance.Recycle(this);
            UpdateChunk(Context.ChunkManager);

            int dialogIndex = _runtimeState.GetDialogIndex();

            if (dialogIndex >= 0)
                _context.DialogManager.ClearDialog(dialogIndex);

            if (_runtimeState.IsWorker() && _runtimeState.IsWorkerValid())
            {
                _runtimeState.SetCarriedItem(new FItemData());
                _stronghold.WorkerComponent.RemoveWorkerCharacter(this, _workerIndex);
            }  

            if (_runtimeState.IsWarrior())
            {
                var pc = _runtimeState.GetFollowPlayer();

                if (pc != null)
                {
                    pc.Formation.RemoveCharacter(this,
                        _runtimeState.GetFormationID(),
                        _runtimeState.GetFormationIndex());
                }
            }
        }

        private NonPlayerCharacterDefinition _definition;
        public NonPlayerCharacterDefinition GetDefinition(ref FNonPlayerCharacterData data) 
        {
            if (data.DefinitionID == 0)
                return null;

            if (_definition == null || 
                _definition.TableID != data.DefinitionID)
            {
                _definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(data.DefinitionID);
            }

            return _definition;
        }

        // Interactable
        private bool IsPotentialInteractor(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            if (_runtimeState.GetHealth() > 0)
            {
                if (_runtimeState.HasDialog())
                {

                    if (_runtimeState.GetAttitude() == EAttitude.Hostile)
                        return false;

                    if (_runtimeState.IsInvader())
                    {
                        if (Context.InvasionManager.InvasionID == 0)
                            return false;

                        if (Context.InvasionManager.InvasionState == EInvasionState.Retreating)
                            return false;
                    }

                    return true;
                }

                if (_runtimeState.IsWorker())
                {
                    if (_runtimeState.GetState() == ENPCState.Dead)
                        return false;

                    return true;
                }
            }

            return false;
        }
        
        private bool IsInteractionValid(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            if (_runtimeState.GetHealth() > 0)
            {
                if (_runtimeState.HasDialog())
                {

                    if (_runtimeState.GetAttitude() == EAttitude.Hostile)
                        return false;

                    if (_runtimeState.IsInvader())
                    {
                        if (Context.InvasionManager.InvasionID == 0)
                            return false;

                        if (Context.InvasionManager.InvasionState == EInvasionState.Retreating)
                            return false;
                    }

                    return true;
                }

                if (_runtimeState.IsWorker())
                {
                    if(_runtimeState.GetState() == ENPCState.Dead)
                        return false;

                    return true;
                }
            }

            return false;
        }

        private string GetInteractionText(InteractorComponent interactor)
        {
            return "NPC";
        }

        private int GetTicksToComplete(InteractorComponent interactor)
        {
            switch (GetInteractType(interactor))
            {
                case EInteractType.Dialog:
                    return -1;
                case EInteractType.HarvestNode:
                    break;
            }

            return 32;
        }

        private EInteractType GetInteractType(InteractorComponent interactor)
        {
            if (_runtimeState.HasDialog())
                return EInteractType.Dialog;

            if(_runtimeState.IsWorker())
                return EInteractType.HarvestNode;

            return EInteractType.None;
        }

        private float GetInteractDistance(InteractorComponent interactor)
        {
            if (_runtimeState.HasDialog())
                return 30;

            if (_runtimeState.IsWorker())
                return 30;

            return 5;
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            if (_runtimeState.HasDialog())
            {
                Debug.Log("Interaction started with NPC.");
                if (DialogComponent.CurrentDialog == null)
                    return;

                Context.DialogManager.SetActiveDialogOwner(_runtimeState.Definition.DialogOwnerInfo);
                Context.DialogManager.SetActiveDialogDefinition(DialogComponent.CurrentDialog);
                Context.DialogManager.SetActiveDialogNode(DialogComponent.CurrentDialog.StartingNode);
            }

        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            if (_runtimeState.HasDialog())
            {
                Debug.Log("Interaction ended with NPC.");

                // If my current open dialog is the one I'm ending interact with, close it.
                if (Context.DialogManager.ActiveDialogDefinition == DialogComponent.CurrentDialog)
                {
                    Context.DialogManager.SetActiveDialogOwner(_runtimeState.Definition.DialogOwnerInfo);
                    Context.DialogManager.SetActiveDialogDefinition(null);
                    Context.DialogManager.SetActiveDialogNode(null);
                }
            }
        }
        
        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction complete with NPC.");

            if (_runtimeState.IsWorker())
            {
                var stronghold = _runtimeState.GetWorkerStronghold();

                if (stronghold == null)
                    return;

                stronghold.WorkerComponent.RPC_PickupWorker((byte)interactor.PC.PlayerIndex, (ushort)_runtimeState.FullIndex);
                

            }
        }
    }
}

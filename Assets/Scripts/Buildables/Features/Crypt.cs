using Fusion;
using UnityEngine;

namespace LichLord.Buildables
{
    public class Crypt : Buildable
    {
        [SerializeField] private Transform _spawnTransform;

        public override float BonusRadius { get { return 0; } }
        public override bool IsAttackable
        {
            get
            {
                if (_healthComponent.CurrentHealth == 0)
                    return false;

                return true;
            }
        }

        public override Collider HurtBoxCollider { get { return Hurtbox.HurtBoxes[0]; } }

        [SerializeField] protected BuildableHealthComponent _healthComponent;
        [SerializeField] protected BuildableStateComponent _stateComponent;
        [SerializeField]  private InteractableComponent _interactableComponent;

        [SerializeField] private VisualEffectBase _interactEffect;
        [SerializeField] private int _workerIndex;

        [SerializeField] private EWorkerState _workerState;

        [SerializeField] private int _spawnEndTick;
        public int SpawnEndTick => _spawnEndTick;

        [SerializeField] private int _cooldownEndTick;
        public int CooldownEndTick => _cooldownEndTick;

        public override void OnSpawned(BuildableZone zone, BuildableRuntimeState runtimeState)
        {
            base.OnSpawned(zone, runtimeState);

            _workerIndex = runtimeState.GetWorkerIndex();

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());

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
        }

        public override void OnRender(BuildableRuntimeState runtimeState, float renderDeltaTime, int tick, bool hasAuthority)
        {
            base.OnRender(runtimeState, renderDeltaTime, tick, hasAuthority);

            _workerIndex = runtimeState.GetWorkerIndex();

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());

            UpdateWorkerStateChange(tick, hasAuthority);
            UpdateWorkerState(tick, hasAuthority);
        }

        private void UpdateWorkerStateChange(int tick, bool hasAuthority)
        {
            EWorkerState newWorkerState = RuntimeState.GetWorkerState();

            if (newWorkerState == _workerState)
                return;

            _workerState = newWorkerState;

            switch (newWorkerState)
            {
                case EWorkerState.Spawning:
                    _spawnEndTick = tick + RuntimeState.GetWorkerSpawnTicks();
                    break;
                case EWorkerState.WorkerActive:
                    break;
                case EWorkerState.Cooldown:
                    _cooldownEndTick = tick + 160;
                    break;
            }
        }

        private void UpdateWorkerState(int tick, bool hasAuthority)
        {
            switch (_workerState)
            {
                case EWorkerState.None:
                    RuntimeState.SetWorkerState(EWorkerState.Spawning);
                    break;
                case EWorkerState.Spawning:
                    if(hasAuthority)
                    {
                        /*
                        if (Context.WorkerManager.ActiveWorkerCount >= Context.WorkerManager.MaxWorkerCount)
                            return;

                        //Debug.Log("Spawning " + (_spawnEndTick - tick));
                        if (tick > _spawnEndTick)
                        {
                            SpawnWorker();
                            RuntimeState.SetWorkerState(EWorkerState.WorkerActive);
                        }
                        */

                    }
                    break;
                case EWorkerState.WorkerActive:
                    if (hasAuthority)
                    {
                        /*
                        if (!Context.WorkerManager.HasActiveWorker(_workerIndex))
                        {
                            //Debug.Log("Worker Active but also failed");
                            RuntimeState.SetWorkerState(EWorkerState.Cooldown);
                           
                        }
                        */
                    }
                        break;
                case EWorkerState.Cooldown:
                    if (hasAuthority)
                    {
                        if (tick > _cooldownEndTick)
                        {
                            RuntimeState.SetWorkerState(EWorkerState.Spawning);
                        }
                    }
                    break;   
            }
        }

        public override void StartRecycle()
        {
            _interactableComponent.onInteractStart -= OnInteractStart;
            _interactableComponent.onInteractEnd -= OnInteractEnd;
            _interactableComponent.onInteractionComplete -= OnInteractionComplete;

            base.StartRecycle();
        }

        public override void OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public override void ProcessHit(ref FHitUtilityData hit)
        {
        }

        // Interactable

        private bool IsPotentialInteractor(InteractorComponent interactor)
        {
            if (RuntimeState.GetIsInteracting())
                return false;

            return interactor != null;
        }

        private bool IsInteractionValid(InteractorComponent interactor)
        {
            return true;
        }

        private string GetInteractionText(InteractorComponent interactor)
        {
            return "Crypt";
        }

        private int GetTicksToComplete(InteractorComponent interactor)
        {
            return 32;
        }

        private EInteractType GetInteractType(InteractorComponent interactor)
        {
            return EInteractType.HarvestNode;
        }
        
        private float GetInteractDistance(InteractorComponent interactor)
        {
            return 5;
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction started with Crypt.");

            NetworkRunner runner = interactor.Runner;
            SceneContext context = interactor.Context;
            /*
            Context.PropManager.RPC_SetInteracting(ChunkID, GUID, true);

            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                Context.PropManager.Predict_SetInteracting(ChunkID, GUID, true);
            */
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with Crypt.");
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Crypt Interaction complete.");
            // Trigger effects, state changes, or events
        }

        private void SpawnWorker()
        {
            if (RuntimeState.DataDefinition is not CryptDataDefinition dataDefinition)
                return;

            var workerIndex = RuntimeState.GetWorkerIndex();
            var workerDefinition = RuntimeState.GetWorkerDefinition();

            // check if there is already a worker for this
            //Context.WorkerManager.TrySpawnWorker(workerIndex, workerDefinition, _spawnTransform.position);
        }
    }
}
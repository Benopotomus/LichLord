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
                GetInteractionTime
            );

            _interactableComponent.onInteractStart += OnInteractStart;
            _interactableComponent.onInteractEnd += OnInteractEnd;
            _interactableComponent.onInteractionComplete += OnInteractionComplete;
        }

        public override void OnRender(BuildableRuntimeState runtimeState, float renderDeltaTime, bool hasAuthority)
        {
            base.OnRender(runtimeState, renderDeltaTime, hasAuthority);

            _workerIndex = runtimeState.GetWorkerIndex();

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());
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

        private float GetInteractionTime(InteractorComponent interactor)
        {
            return 3.0f;
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

            if (RuntimeState.DataDefinition is not CryptDataDefinition dataDefinition)
                return;

            var workerIndex = RuntimeState.GetWorkerIndex();

            // check if there is already a worker for this
            
            if (Context.WorkerManager.HasWorker(workerIndex) ||
                Context.WorkerManager.ActiveWorkerCount >= Context.WorkerManager.MaxWorkerCount)
                return;
            
            Context.NonPlayerCharacterManager.SpawnNPCWorker(_spawnTransform.position, 
                dataDefinition.WorkerDefinition, 
                ETeamID.PlayerTeam, 
                workerIndex);
        }
    }
}
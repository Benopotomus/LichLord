
using Fusion;
using UnityEngine;

namespace LichLord.Buildables
{
    public class TaskMaster : Buildable
    {
        [SerializeField] protected BuildableHealthComponent _healthComponent;
        [SerializeField] protected BuildableStateComponent _stateComponent;
        [SerializeField] private InteractableComponent _interactableComponent;

        [SerializeField]
        private VisualEffectBase _interactEffect;

        [SerializeField]
        private bool _isInteracting;

        public override void OnSpawned(BuildableZone zone, BuildableRuntimeState runtimeState)
        {
            base.OnSpawned(zone, runtimeState);

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

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());

            bool newIsInteracting = RuntimeState.GetIsInteracting();

            if (_isInteracting != newIsInteracting)
            {
                OnInteractingChanged(newIsInteracting);
                _isInteracting = newIsInteracting;
            }
        }

        private void OnInteractingChanged(bool newIsInteracting)
        {

        }

        public override void StartRecycle()
        {
            _interactableComponent.onInteractStart -= OnInteractStart;
            _interactableComponent.onInteractEnd -= OnInteractEnd;
            _interactableComponent.onInteractionComplete -= OnInteractionComplete;


            base.StartRecycle();
        }

        // Interactable

        private bool IsPotentialInteractor(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            if (RuntimeState.GetIsInteracting())
                return false;

            return interactor != null;
        }

        private bool IsInteractionValid(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            return true;
        }

        private string GetInteractionText(InteractorComponent interactor)
        {
            return "Storage Chest";
        }

        private int GetTicksToComplete(InteractorComponent interactor)
        {
            return -1;
        }

        private EInteractType GetInteractType(InteractorComponent interactor)
        {
            return EInteractType.Container;
        }

        private float GetInteractDistance(InteractorComponent interactor)
        {
            return 5;
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction started with " + this);

            if (RuntimeState.DataDefinition is not ContainerDataDefinition dataDefinition)
                return;

            RuntimeState.SetInteracting(true, Context.Runner.Tick);
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with " + this);

            if (RuntimeState.DataDefinition is not ContainerDataDefinition dataDefinition)
                return;

            RuntimeState.SetInteracting(false, Context.Runner.Tick);
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction complete with " + this);
            // Trigger effects, state changes, or events

            if (RuntimeState.DataDefinition is not ContainerDataDefinition dataDefinition)
                return;

            NetworkRunner runner = Context.Runner;
            PlayerCharacter pc = interactor.PC;

            // Open the container UI
        }
    }
}

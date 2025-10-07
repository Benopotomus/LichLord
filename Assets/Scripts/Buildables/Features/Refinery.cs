using Fusion;
using UnityEngine;
using LichLord.Items;
using System.Collections.Generic;

namespace LichLord.Buildables
{
    public class Refinery: Buildable
    {
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
        [SerializeField] private InteractableComponent _interactableComponent;

        [SerializeField]
        private VisualEffectBase _interactEffect;

        [SerializeField]
        private RefineryStateComponent _refineryStateComponent;
        public RefineryStateComponent RefineryStateComponent => _refineryStateComponent;

        [SerializeField]
        private int _containerIndex = -1;

        [SerializeField]
        private int _itemSlotIndexStart = -1;

        [SerializeField]
        private int _itemSlotIndexEnd = -1;

        [SerializeField]
        private bool _isInteracting;



        public override void OnSpawned(BuildableZone zone, BuildableRuntimeState runtimeState)
        {
            base.OnSpawned(zone, runtimeState);

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());

            _containerIndex = RuntimeState.GetContainerIndex();
            var indexes = RuntimeState.GetItemSlotIndexes();
            _itemSlotIndexStart = indexes.start;
            _itemSlotIndexEnd = indexes.end;

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

            _refineryStateComponent.UpdateRefineryState(runtimeState, tick, hasAuthority);

            bool newIsInteracting = RuntimeState.GetIsInteracting();

            if (_isInteracting != newIsInteracting)
            {
                OnInteractingChanged(newIsInteracting);
                _isInteracting = newIsInteracting;
            }
        }

       
        private void OnInteractingChanged(bool newIsInteracting)
        {
            /*
            if (_lidTransform == null)
                return;

            // Kill any existing tween to prevent conflicts
            _lidTransform.DOKill();

            if (newIsInteracting)
            {
                // Rotate lid to 40 degrees on local X axis when interacting
                _lidTransform.DOLocalRotate(new Vector3(40f, 0f, 0f), 0.5f, RotateMode.Fast)
                    .SetEase(Ease.OutQuad);
            }
            else
            {
                // Rotate lid back to 0 degrees on local X axis when interaction ends
                _lidTransform.DOLocalRotate(Vector3.zero, 0.5f, RotateMode.Fast)
                    .SetEase(Ease.OutQuad);
            }
            */
        }

        public override void StartRecycle()
        {
            _interactableComponent.onInteractStart -= OnInteractStart;
            _interactableComponent.onInteractEnd -= OnInteractEnd;
            _interactableComponent.onInteractionComplete -= OnInteractionComplete;

            /*
            // Clean up any ongoing tweens
            if (_lidTransform != null)
            {
                _lidTransform.DOKill();
            }
            */
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
            return "Refinery";
        }

        private int GetTicksToComplete(InteractorComponent interactor)
        {
            return -1;
        }

        private EInteractType GetInteractType(InteractorComponent interactor)
        {
            return EInteractType.Refinery;
        }

        private float GetInteractDistance(InteractorComponent interactor)
        {
            return 5;
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction started with " + this);

            if (RuntimeState.Definition is not RefineryDefinition refinery)
                return;

            RuntimeState.SetInteracting(true, Context.Runner.Tick);
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with " + this);

            if (RuntimeState.Definition is not RefineryDefinition refinery)
                return;

            RuntimeState.SetInteracting(false, Context.Runner.Tick);
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction complete with " + this);
            // Trigger effects, state changes, or events

            if (RuntimeState.Definition is not RefineryDefinition refinery)
                return;

            NetworkRunner runner = Context.Runner;
            PlayerCharacter pc = interactor.PC;

            // Open the container UI
        }
    }
}
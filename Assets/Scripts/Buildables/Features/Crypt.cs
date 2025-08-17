using DWD.Pooling;
using Fusion;
using LichLord.Props;
using UnityEngine;

namespace LichLord.Buildables
{
    public class Crypt : Buildable
    {
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

        [SerializeField]
        private VisualEffectBase _interactEffect;

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
                GetInteractionTime
            );

            _interactableComponent.onInteractStart += OnInteractStart;
            _interactableComponent.onInteractEnd += OnInteractEnd;
            _interactableComponent.onInteractionComplete += OnInteractionComplete;
        }

        public override void OnRender(BuildableRuntimeState runtimeState, float renderDeltaTime, bool hasAuthority)
        {
            base.OnRender(runtimeState, renderDeltaTime, hasAuthority);

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

            if (RuntimeState.DataDefinition is not StockpileDataDefinition dataDefinition)
                return;
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with Crypt.");
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Crypt Interaction complete.");
            // Trigger effects, state changes, or events

            if (RuntimeState.DataDefinition is not StockpileDataDefinition dataDefinition)
                return;

            int stockpileIndex = RuntimeState.GetStockpileIndex();
            NetworkRunner runner = interactor.Runner;
            SceneContext context = interactor.Context;
            PlayerCharacter pc = interactor.PC;

            var currencyType = ECurrencyType.None;
            var value = 0;
            // i want to grab the first currency with a stack and add it to the stockpile 
            pc.Currency.GetCurrencyWithCount(ref currencyType, ref value);

            if (currencyType == ECurrencyType.None)
                return;

            pc.Currency.AddCurrency(currencyType, -value);

            context.ContainerManager.RPC_StockpileDropOff_Player(stockpileIndex, currencyType, value, pc);

            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                context.ContainerManager.Predict_StockpileDropOff_Player(stockpileIndex, currencyType, value);
        }
    }
}
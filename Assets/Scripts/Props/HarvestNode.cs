using Fusion;
using LichLord.World;
using Pathfinding;
using UnityEngine;

namespace LichLord.Props
{
    public class HarvestNode : Prop
    {
        [SerializeField]
        private InteractableComponent _interactableComponent;

        [SerializeField]
        private VisualEffectBase _interactEffect;

        [SerializeField]
        protected PropStateComponent _stateComponent;
        public PropStateComponent StateComponent => _stateComponent;

        public override void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            base.OnSpawned(propRuntimeState, propManager);

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

            _stateComponent.UpdateState(_propRuntimeState.GetState());

            UpdateNavmesh();
        }

        private void UpdateNavmesh()
        {
            var bounds = new Bounds(transform.position, new Vector3(5f, 5f, 5f)); // adjust size as needed
            var guo = new GraphUpdateObject(bounds)
            {
                updatePhysics = true,
                resetPenaltyOnPhysics = true,
                modifyWalkability = true
            };

            AstarPath.active.UpdateGraphs(guo);
        }

        public override void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            base.OnRender(propRuntimeState, renderDeltaTime);

            _stateComponent.UpdateState(_propRuntimeState.GetState());

            if (_interactEffect != null)
                _interactEffect.Toggle(propRuntimeState.GetIsInteracting());
        }

        private bool IsPotentialInteractor(InteractorComponent interactor)
        {
            if (_propRuntimeState.GetIsInteracting())
                return false;

            if (_propRuntimeState.GetIsActivated())
                return false;

            return interactor != null;
        }

        private bool IsInteractionValid(InteractorComponent interactor)
        {
            if (_propRuntimeState.GetIsActivated())
                return false;

            return true;
        }

        private string GetInteractionText(InteractorComponent interactor)
        {
            return "Harvest Node";
        }

        private float GetInteractionTime(InteractorComponent interactor)
        {
            return 3.0f;
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction started with Harvest Node.");
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with Harvest Node.");
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Harvest Node Interaction complete.");
            // Trigger effects, state changes, or events

            if (RuntimeState.Definition.PropDataDefinition is not HarvestNodeDataDefinition harvestData)
                return;

            Prop prop = interactable.Owner;
            NetworkRunner runner = interactor.Runner;
            SceneContext context = interactor.Context;

            context.PropManager.RPC_HarvestNode(prop.ChunkID, prop.GUID, harvestData.HarvestPointsCost);

            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                context.PropManager.Predict_SetActivated(prop.ChunkID, prop.GUID, true);

            interactor.PC.Currency.AddCurrency(harvestData.CurrencyTypeHarvested, harvestData.ResourcesPerHarvest);
        }


    }
}

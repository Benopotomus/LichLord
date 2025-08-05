using Fusion;
using LichLord.World;
using Pathfinding;
using UnityEngine;

namespace LichLord.Props
{
    public class Nexus : Prop
    {
        [SerializeField]
        private InteractableComponent _interactableComponent;

        [SerializeField]
        private VisualEffectBase _interactEffect;

        [SerializeField]
        private Transform _rocksTransform;

        public override bool IsAttackable
        {
            get
            {
                return false;
            }
        }

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

            _interactEffect.Toggle(propRuntimeState.GetIsInteracting());

            float rotationSpeed = 20f; // degrees per second
            _rocksTransform.Rotate(0f, rotationSpeed * renderDeltaTime, 0f, Space.Self);
            _rocksTransform.SetActive(!propRuntimeState.GetIsActivated());

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
            return "Activate Nexus";
        }

        private float GetInteractionTime(InteractorComponent interactor)
        {
            return 2.0f; // seconds to complete interaction
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction started with Nexus.");
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with Nexus.");
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Nexus interaction complete.");
            // Trigger effects, state changes, or events

            Prop prop = interactable.Owner;
            NetworkRunner runner = interactor.Runner;
            SceneContext context = interactor.Context;

            context.PropManager.RPC_SetActivated(prop.ChunkID, prop.GUID, true);

            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                context.PropManager.Predict_SetActivated(prop.ChunkID, prop.GUID, true);

            FStrongholdData strongholdData = new FStrongholdData();
            strongholdData.ChunkID = prop.ChunkID;
            strongholdData.GUID = (byte)prop.GUID;

            context.StrongholdManager.RPC_ActivateNexus(strongholdData);

            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                context.StrongholdManager.Predict_ActivateNexus(strongholdData);

        }
    }
}

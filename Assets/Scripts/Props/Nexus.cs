using Fusion;
using LichLord.World;
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
                GetTicksToComplete,
                GetInteractType,
                GetInteractDistance
            );

            _interactableComponent.onInteractStart += OnInteractStart;
            _interactableComponent.onInteractEnd += OnInteractEnd;
            _interactableComponent.onInteractionComplete += OnInteractionComplete;

        }

        public override void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            base.OnRender(propRuntimeState, renderDeltaTime);

            _interactEffect.Toggle(propRuntimeState.GetIsInteracting());
            _rocksTransform.SetActive(!propRuntimeState.GetIsActivated());
        }

        private bool IsPotentialInteractor(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;
            
            if (sqrDist > interactDistance)
                return false;

            if (_runtimeState.GetIsInteracting())
                return false;

            if (_runtimeState.GetIsActivated())
                return false;

            return interactor != null;
        }

        private bool IsInteractionValid(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            if (_runtimeState.GetIsActivated())
                return false;

            return true;
        }

        private string GetInteractionText(InteractorComponent interactor)
        {
            return "Activate Nexus";
        }

        private int GetTicksToComplete(InteractorComponent interactor)
        {
            return 64; // seconds to complete interaction
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
            NetworkRunner runner = interactor.Runner;
            SceneContext context = interactor.Context;

            Debug.Log("Interaction started with Nexus.");

            Context.PropManager.RPC_SetInteracting(ChunkID, Index, true);

            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                Context.PropManager.Predict_SetInteracting(ChunkID, Index, true);        
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            NetworkRunner runner = interactor.Runner;
            SceneContext context = interactor.Context;

            Debug.Log("Interaction ended with Nexus.");

            Context.PropManager.RPC_SetInteracting(ChunkID, Index, false);

            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                Context.PropManager.Predict_SetInteracting(ChunkID, Index, false);
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Nexus interaction complete.");
            // Trigger effects, state changes, or events

            NetworkRunner runner = interactor.Runner;
            SceneContext context = interactor.Context;

            context.PropManager.RPC_SetActivated(ChunkID, Index, true);

            
            if (!runner.IsSharedModeMasterClient && runner.GameMode != GameMode.Single)
                context.PropManager.Predict_SetActivated(ChunkID, Index, true);

            FStaticPropPosition staticPropPosition = new FStaticPropPosition();
            staticPropPosition.ChunkPosition = ChunkID;
            staticPropPosition.PropIndex = (ushort)Index;

            context.MissionManager.NexusInteractionComplete();
            context.StrongholdManager.RPC_ActivateNexus(staticPropPosition);
            
        }
    }
}

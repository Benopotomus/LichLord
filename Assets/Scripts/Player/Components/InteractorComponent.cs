/// <summary>
/// Works with the interaction component and listens for overlaps and inputs.
/// Players only
/// </summary>

namespace LichLord
{
    using UnityEngine;
    using System.Collections.Generic;
    using Fusion;
    using LichLord.Props;
    using LichLord.UI;

    public class InteractorComponent : ContextBehaviour
    {
        [SerializeField] private PlayerCharacter _pc;

        [SerializeField] private LayerMask _interactableLayerMask;

        private List<InteractableComponent> _allInteractables = new List<InteractableComponent>();

        [SerializeField] private InteractableComponent _bestInteractable;
        [SerializeField] private InteractableComponent _currentInteractable;

        private float _interactDistance = 1.0f;

        [Networked]
        private ref FWorldPosition _interactTargetPosition => ref MakeRef<FWorldPosition>();

        [SerializeField]
        private FUpperBodyAnimationState _interactState;

        public void ProcessInput(ref FGameplayInput input)
        {
            if (_bestInteractable == null)
                return;

            if (!input.Interact)
                return;

            if (_pc.FSM.StateMachine.ActiveState is IdleState idleState)
            {
                StartInteract(_bestInteractable);
  
            }
            else if (_pc.FSM.StateMachine.ActiveState is InteractingState interactingState)
            {
                StopInteract(_bestInteractable);
            }
        }

        private void StartInteract(InteractableComponent interactable)
        {
            int tick = Runner.Tick;

            CharacterStateBase state = _pc.FSM.StateMachine.ActiveState as CharacterStateBase; ;

            state.MoveToInteract();
            _currentInteractable = _bestInteractable;
            _currentInteractable.InteractStart(this, tick);
            _pc.Movement.LookTarget = _currentInteractable.transform;

            Prop prop = interactable.Owner;

            Context.PropManager.RPC_SetInteracting(prop.ChunkID, prop.GUID, true);

            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                Context.PropManager.Predict_SetInteracting(prop.ChunkID, prop.GUID, true);
        }

        private void StopInteract(InteractableComponent interactable)
        {
            int tick = Runner.Tick;

            CharacterStateBase state = _pc.FSM.StateMachine.ActiveState as CharacterStateBase; ;

            state.MoveToIdle();
            _currentInteractable.InteractEnd(this);
            _currentInteractable = null;
            _pc.Movement.LookTarget = null;
            
            Prop prop = interactable.Owner;

            Context.PropManager.RPC_SetInteracting(prop.ChunkID, prop.GUID, false);

            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                Context.PropManager.Predict_SetInteracting(prop.ChunkID, prop.GUID, false);
        }

        public void OnFixedUpdateNetwork(int tick, float deltaTime)
        {
            if (_currentInteractable != null)
            {
                Vector3 interactablePosition = _currentInteractable.transform.position;
                _pc.Movement.ProcessInteractMovement(interactablePosition, deltaTime);
                if (_currentInteractable.GetTimeRemaining(tick) <= 0f)
                {
                    _currentInteractable.CompleteInteract(this);
                    StopInteract(_currentInteractable);
                }
            }
        }

        public int UpperbodyTriggerNumber;
        public int UpperbodyTriggerDuration;

        public void UpdateInteractUI(float localRenderTime)
        {
            if (!HasStateAuthority)
                return;

            GameplayUI gameplayUI = Context.UI as GameplayUI;

            if (gameplayUI == null)
                return;

            UIFloatingInteract floatingInteract = gameplayUI.HUD.FloatingInteract;

            if (_bestInteractable == null)
            {
                floatingInteract.SetTarget(null);
                return;
            }

            if (_bestInteractable.IsInteractionValid(this))
            {
                floatingInteract.SetTarget(_bestInteractable.transform);
            }

            if (_currentInteractable == null)
            {
                floatingInteract.SetProgressBarVisible(false);
                return;
            }

            floatingInteract.SetProgressBarPercent(_currentInteractable.GetTimeRemaining(localRenderTime));
            floatingInteract.SetProgressBarVisible(true);
        }

        public void OnRender(float deltaTime, float localRenderTime, int tick)
        {
            UpdateInteractUI(localRenderTime);
        }

        public void RefreshInteractables()
        {
            if (_currentInteractable != null)
            {
                _bestInteractable = _currentInteractable;
                return;
            }

            _allInteractables.Clear();
            _bestInteractable = null;

            Collider[] checkedCollisions = new Collider[4];

            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                _interactDistance,
                checkedCollisions,
                _interactableLayerMask,
                QueryTriggerInteraction.Collide
            );

            float smallestDist = float.MaxValue;
            InteractableComponent newBestInteractable = null;

            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = checkedCollisions[i];
                if (collider == null)
                    continue;

                InteractableComponent curInteractable = collider.GetComponent<InteractableComponent>();
                if (curInteractable == null)
                    continue;

                if (!curInteractable.IsPotentialInteractor(this))
                    continue;

                var directionToInteractable = (curInteractable.transform.position - Camera.main.transform.position).normalized;
                var cameraForward = Camera.main.transform.forward;
                var dot = Vector3.Dot(directionToInteractable, cameraForward);

                if (dot < 0.95)
                    continue;

                float testDist = Vector3.SqrMagnitude(transform.position - curInteractable.transform.position);

                if (testDist < smallestDist)
                {
                    smallestDist = testDist;
                    newBestInteractable = curInteractable;
                }

                _allInteractables.Add(curInteractable);
            }

            _bestInteractable = newBestInteractable;
        }
    }
}
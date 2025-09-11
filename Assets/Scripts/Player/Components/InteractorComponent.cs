/// <summary>
/// Works with the interaction component and listens for overlaps and inputs.
/// Players only
/// </summary>

namespace LichLord
{
    using UnityEngine;
    using Fusion;
    using LichLord.UI;
    using DWD.Pooling;
    using AYellowpaper.SerializedCollections;
    using LichLord.Player;

    public class InteractorComponent : ContextBehaviour
    {
        [SerializeField] private PlayerCharacter _pc;
        public PlayerCharacter PC => _pc;

        [SerializeField] private InteractableComponent _bestInteractable;
        public InteractableComponent BestInteractable => _bestInteractable;

        [SerializeField] private InteractableComponent _currentInteractable;
        public InteractableComponent CurrentInteractable => _currentInteractable;

        [Networked]
        private ref FWorldPosition _interactTargetPosition => ref MakeRef<FWorldPosition>();

        [Networked]
        private EInteractType _interactType { get; set; }

        [SerializeField]
        [SerializedDictionary("InteractionType", "InteractorAction")]
        private SerializedDictionary<EInteractType, InteractorActionDefinition> _interactorActions;

        [SerializeField]
        private VisualEffectBeam _beamPrefab;

        private VisualEffectBeam _beamInstance;

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
            else if (_pc.FSM.StateMachine.ActiveState is BuildModeState buildModeState)
            {
                StartInteract(_bestInteractable);
            }
            else if (_pc.FSM.StateMachine.ActiveState is InteractingState interactingState)
            {
                StopInteract();
            }
        }

        // Used to force the interaction stopping via dialog closing
        public void SetInteractType(EInteractType newInteractType)
        { 
            _interactType = newInteractType;
        
        }
        private void StartInteract(InteractableComponent interactable)
        {
            int tick = Runner.Tick;

            CharacterStateBase state = _pc.FSM.StateMachine.ActiveState as CharacterStateBase; ;

            state.MoveToInteract();
            _currentInteractable = _bestInteractable;

            if (_currentInteractable == null)
                return;

            _interactTargetPosition.CopyPosition(_currentInteractable.transform.position);
            _pc.Movement.LookTarget = _currentInteractable.transform;
            _interactType = _currentInteractable.GetInteractType(this);
            _currentInteractable.InteractStart(this, tick);
        }

        private void StopInteract()
        {
            if (_currentInteractable == null)
                return;

            int tick = Runner.Tick;

            CharacterStateBase state = _pc.FSM.StateMachine.ActiveState as CharacterStateBase;

            _interactType = EInteractType.None;

            state.MoveToIdle();
            _currentInteractable.InteractEnd(this);
            _currentInteractable = null;
            _pc.Movement.LookTarget = null;
        }

        public void CancelInteract(InteractableComponent interactable, string warningMessage)
        { 
            StopInteract();

            GameplayUI gameplayUI = Context.UI as GameplayUI;

            if (gameplayUI == null)
                return;

            UIFloatingInteract interactUI = gameplayUI.HUD.FloatingInteract;

            if (interactUI != null)
                interactUI.ShowWarningMessage(warningMessage);
        }

        public void OnFixedUpdateNetwork(int tick, float deltaTime)
        {
            if (_interactType == EInteractType.None)
            {
                StopInteract();
            }

            if (_currentInteractable == null)
                return;

            RotateTowardInteract(deltaTime);

            if (!_currentInteractable.IsInteractionValid(this))
            {
                StopInteract();
                return;
            }

            // if ticks to complete is under zero, its infinite
            if (_currentInteractable.GetTicksToComplete(this) < 0)
                return;
                
            if (_currentInteractable.GetTimeRemaining(tick) <= 0f)
            {
                _currentInteractable.CompleteInteract(this);
                StopInteract();
            }        
        }

        public void RotateTowardInteract(float deltaTime)
        {
            Vector3 interactablePosition = _currentInteractable.transform.position;
            _pc.Movement.ProcessInteractMovement(interactablePosition, deltaTime);
        }

        public void OnRender(float deltaTime, float localRenderTime, int tick)
        {
        }

        public void OnEnterStateRender()
        {
            if (_interactorActions.TryGetValue(_interactType, out InteractorActionDefinition actionDefinition))
            {
                actionDefinition.OnEnterStateRender(this);
            }
        }

        public void OnExitStateRender()
        {
            _pc.AnimationController.SetAnimationForUpperBodyTrigger(0);
            _pc.Aim.TargetPitchOffset = 0f;
            _pc.Aim.TargetYawOffset = 0f;
            _pc.Aim.TargetRollOffset = 0f;

            if (_beamInstance != null)
                _beamInstance.StartRecycle(0.75f);
        }

        public void UpdateBeam()
        {
            if (_beamInstance == null) 
                return;

            _beamInstance.UpdateBeamPosition(
                _pc.Muzzle.GetMuzzlePosition(Projectiles.EMuzzle.RightHand), 
                _interactTargetPosition.Position);
        }

        public void RefreshInteractables()
        {
            if (_currentInteractable != null)
            {
                _bestInteractable = _currentInteractable;
                return;
            }

            _bestInteractable = null;

            // Use the cached raycast result from the scene camera
            var interactable = Context.Camera.CachedRaycastHit.interactable;

            if (interactable != null && interactable.IsPotentialInteractor(this))
            {
                _bestInteractable = interactable;
            }
        }

        public void SpawnBeamEffect(VisualEffectBeam beamPrefab)
        {
            Vector3 spawnPosition = _pc.Muzzle.GetMuzzlePosition(Projectiles.EMuzzle.RightHand);

            var instance = DWDObjectPool.Instance.SpawnAt(beamPrefab, spawnPosition, Quaternion.identity);
            if (instance is VisualEffectBeam beamEffect)
            {
                _beamInstance = beamEffect;
                _beamInstance.UpdateBeamPosition(
                    _pc.Muzzle.GetMuzzlePosition(Projectiles.EMuzzle.RightHand),
                    _interactTargetPosition.Position);

                _beamInstance.ToggleBeam(true);
            }
        }
    }
}
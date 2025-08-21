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
    using DWD.Pooling;
    using LichLord.Buildables;

    public class InteractorComponent : ContextBehaviour
    {
        [SerializeField] private PlayerCharacter _pc;
        public PlayerCharacter PC => _pc;

        [SerializeField] private LayerMask _interactableLayerMask;

        private List<InteractableComponent> _allInteractables = new List<InteractableComponent>();

        [SerializeField] private InteractableComponent _bestInteractable;
        [SerializeField] private InteractableComponent _currentInteractable;

        private float _interactDistance = 1.0f;

        [Networked]
        private ref FWorldPosition _interactTargetPosition => ref MakeRef<FWorldPosition>();

        [SerializeField]
        private float _pitchOffset = 0f;

        [SerializeField]
        private float _yawOffset = 0f;

        [SerializeField]
        private float _rollOffset = 0f;

        [SerializeField]
        private VisualEffectBeam _beamPrefab;

        private VisualEffectBeam _beamInstance;

        private UIFloatingInteract _floatingUI;
        public UIFloatingInteract FloatingUI
        {
            get
            {
                if (_floatingUI == null)
                {
                    GameplayUI gameplayUI = Context.UI as GameplayUI;

                    if (gameplayUI == null)
                        return null;

                    _floatingUI = gameplayUI.HUD.FloatingInteract;
                }

                return _floatingUI;
            }
        }

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
                StopInteract(_bestInteractable);
            }
        }

        private void StartInteract(InteractableComponent interactable)
        {
            int tick = Runner.Tick;

            CharacterStateBase state = _pc.FSM.StateMachine.ActiveState as CharacterStateBase; ;

            state.MoveToInteract();
            _currentInteractable = _bestInteractable;
            _interactTargetPosition.CopyPosition(_currentInteractable.transform.position);

            _pc.Movement.LookTarget = _currentInteractable.transform;

            _currentInteractable.InteractStart(this, tick);
        }

        private void StopInteract(InteractableComponent interactable)
        {
            int tick = Runner.Tick;

            CharacterStateBase state = _pc.FSM.StateMachine.ActiveState as CharacterStateBase; ;

            state.MoveToIdle();
            _currentInteractable.InteractEnd(this);
            _currentInteractable = null;
            _pc.Movement.LookTarget = null;
        }

        public void CancelInteract(InteractableComponent interactable, string warningMessage)
        { 
            StopInteract(interactable);

            if (FloatingUI != null)
                FloatingUI.ShowWarningMessage(warningMessage);
        }

        public void OnFixedUpdateNetwork(int tick, float deltaTime)
        {
            if (_currentInteractable != null)
            {
                RotateTowardInteract(deltaTime);

                if (_currentInteractable.GetTimeRemaining(tick) <= 0f)
                {
                    _currentInteractable.CompleteInteract(this);
                    StopInteract(_currentInteractable);
                }
            }
        }

        public void RotateTowardInteract(float deltaTime)
        {
            Vector3 interactablePosition = _currentInteractable.transform.position;
            _pc.Movement.ProcessInteractMovement(interactablePosition, deltaTime);
        }

        public void UpdateInteractUI(float localRenderTime)
        {
            if (!HasStateAuthority)
                return;


            if (FloatingUI == null)
                return;

            if (_bestInteractable == null)
            {
                FloatingUI.SetTarget(null);
                return;
            }

            if (_bestInteractable.IsInteractionValid(this))
            {
                FloatingUI.SetTarget(_bestInteractable.transform);
            }

            int stockpileIndex = -1;
            if (_bestInteractable.Owner is Stockpile stockpile)
            {
                stockpileIndex = stockpile.RuntimeState.GetStockpileIndex();
            }

            FloatingUI.ShowStockpileContents(stockpileIndex);

            if (_currentInteractable == null)
            {
                FloatingUI.SetProgressBarVisible(false);
                return;
            }

            FloatingUI.SetProgressBarPercent(_currentInteractable.GetPercentRemaining(localRenderTime));
            FloatingUI.SetProgressBarVisible(true);
        }

        public void OnRender(float deltaTime, float localRenderTime, int tick)
        {
            UpdateInteractUI(localRenderTime);
        }

        public void OnEnterStateRender()
        {
            _pc.AnimationController.SetAnimationForUpperBodyTrigger(5);
            _pc.Aim.TargetPitchOffset = _pitchOffset;
            _pc.Aim.TargetYawOffset = _yawOffset;
            _pc.Aim.TargetRollOffset = _rollOffset;
            SpawnBeamEffect();
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

            _allInteractables.Clear();
            _bestInteractable = null;

            Collider[] checkedCollisions = new Collider[8];

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

        private void SpawnBeamEffect()
        {
            Vector3 spawnPosition = _pc.Muzzle.GetMuzzlePosition(Projectiles.EMuzzle.RightHand);

            var instance = DWDObjectPool.Instance.SpawnAt(_beamPrefab, spawnPosition, Quaternion.identity);
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
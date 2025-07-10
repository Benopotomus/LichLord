/// <summary>
/// Works with the interaction component and listens for overlaps and inputs.
/// Players only
/// </summary>

namespace LichLord
{
    using UnityEngine;
    using System.Collections.Generic;
    using Fusion;
    using System;
    using LichLord.Props;
    using LichLord.UI;

    public class InteractorComponent : ContextBehaviour
    {
        [SerializeField]
        private LayerMask _interactableLayerMask;

        [SerializeField]
        private PlayerCharacter _pc;

        private List<InteractableComponent> _allInteractables = new List<InteractableComponent>();

        [SerializeField]
        private InteractableComponent _bestInteractable;

        private float _interactDistance = 1.0f;

        [Networked]
        private ref FInteractorState _state => ref MakeRef<FInteractorState>();

        public Action<InteractorComponent, InteractableComponent> onBestInteractableChanged;
        public void InvokeBestInteractableChanged(InteractableComponent interactable) { onBestInteractableChanged?.Invoke(this, interactable); }

        public Action<InteractorComponent, bool> onInteractingChanged;
        public void InvokeInteractingChanged(bool isInteracting) { onInteractingChanged?.Invoke(this, isInteracting); }

        public bool IsInteracting => _state.IsInteracting;
        public bool IsExecuting => _state.IsExecuting;
        public bool IsLooting => _state.IsLooting;

        public void ProcessInput(ref FGameplayInput input)
        {
            if (_bestInteractable == null)
                return;

            if (input.Interact)
            {
                _bestInteractable.InteractStart(this);
            }
        }

        public void OnFixedUpdate()
        {
            RefreshInteractables();
            UpdateInteractUI();
        }

        private void UpdateInteractUI()
        {
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
        }

        public void InteractableEntered(InteractableComponent interactable)
        {
            _allInteractables.Add(interactable);
        }

        public void InteractableExited(InteractableComponent interactable)
        {
            _allInteractables.Remove(interactable);
        }

        private void RefreshInteractables()
        {
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

                float testDist = Vector3.SqrMagnitude(transform.position - curInteractable.transform.position);

                if (testDist < smallestDist)
                {
                    smallestDist = testDist;
                    newBestInteractable = curInteractable;
                }

                _allInteractables.Add(curInteractable);
            }

            // If needed, do something with newBestInteractable

            _bestInteractable = newBestInteractable;
        }

        /*
        //[Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Unreliable)]
        private void RPC_CompleteInteract(FInteractorState state)
        {
            // Figure out what to do when the interact time completes
            InteractableComponent interactable = state.GetInteractable(Context);

            if (interactable == null)
                return;

            interactable.CompleteInteract(this, state);
            _state.IsInteracting = false;
            _state.StartTick = 0;
            _state.SetInteractable(null);

            // if the interact is a chest start looting
            if (interactable.Owner is Chest chest)
            {
                _state.IsLooting = true;
            }

            if (interactable.Owner is CreatureEntity creatureEntity)
            {
                Debug.Log("Complete Interact");

                _state.IsInteracting = false;
                _state.IsExecuting = false;
                _state.IsReviving = false;
            }
        }

        public bool PollInput(eInputAction inputAction)
        {
            if (!heroEntity.HeroInput.HasActive(inputAction))
                return false;

            if (heroEntity.ActionsComponent.PressLockedInput == inputAction)
                return false;

            if (!CanInteract())
                return false;

            if (!_state.IsInteracting)
                Input_StartInteract();
            else
                Input_EndInteract();

            heroEntity.ActionsComponent.PressLockedInput = inputAction;
            return true;
        }

        private bool CanInteract()
        {
            if (heroEntity.ActiveStrikeEvent.IsValid())
                return false;

            return true;
        }

        public void CancelLooting()
        {
            _state.Clear();
            _state.IsInteracting = false;
            _state.IsLooting = false;
        }

        public float GetInteractionPercent(float simOrRenderTime)
        {
            if (!_state.IsValid() || !_state.IsInteracting || BestInteractable == null)
                return 0;

            float timespan = BestInteractable.GetInteractionTime(this);
            float completeTime = (_state.StartTick * Runner.DeltaTime) + timespan;

            float remainingTime = completeTime - simOrRenderTime;

            return Mathf.Clamp01((timespan - remainingTime) / timespan);
        }


        public bool IsSkillUseLocked()
        {
            if (_state.IsInteracting)
                return true;

            return false;
        }

        public void Input_StartInteract()
        {
            InteractableComponent interactable = _state.GetInteractable(Context);
            if (interactable == null)
                return;

            // check interaction valid with the object (two people opening a door, or door state invalid)
            if (!interactable.IsInteractionValid(this))
                return;

            _state.StartTick = Runner.Tick;
            _state.IsInteracting = true;

            if (interactable.Owner is CreatureEntity targetCreature)
            {
                if (targetCreature.FSM.StateMachine.ActiveState is CreatureDownedState)
                {
                    switch (TeamUtility.GetAttitudeForContextBehaviors(targetCreature, heroEntity))
                    {
                        case eTeamAttitude.Friendly:
                            _state.IsReviving = true;

                            break;
                        case eTeamAttitude.Hostile:
                            _state.IsExecuting = true;
                            break;
                    }
                }
            }
        }

        public void Input_EndInteract()
        {
            _state.IsInteracting = false;
        }

        public Vector2 GetVectorToInteractable()
        {
            if (BestInteractable == null)
                return Vector2.right;

            return (BestInteractable.transform.position - heroEntity.CachedTransform.position).normalized;
        }
         */
    }


}
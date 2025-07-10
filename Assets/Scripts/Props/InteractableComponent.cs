using System;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Props
{
    public class InteractableComponent : MonoBehaviour
    {
        public Prop _owner { get; protected set; }
        private Func<InteractorComponent, bool> _IsPotentialInteract;
        private Func<InteractorComponent, bool> _IsInteractionValid;
        private Func<InteractorComponent, string> _GetInteractionText;
        private Func<InteractorComponent, float> _GetInteractionTime;

        public Action<InteractableComponent, InteractorComponent> onInteractStart;
        public Action<InteractableComponent, InteractorComponent> onInteractEnd;
        public Action<InteractableComponent, InteractorComponent> onInteractionComplete;

        public InteractorComponent CurrentInteractor { get; protected set; }
        public int InteractTick = 32;
        
        public void Activate(Prop owner,
            Func<InteractorComponent, bool> isPotentialInteract,
            Func<InteractorComponent, bool> isInteractionValid,
            Func<InteractorComponent, string> getInteractionText,
            Func<InteractorComponent, float> getInteractionTime)
        {
            _IsPotentialInteract = isPotentialInteract;
            _IsInteractionValid = isInteractionValid;
            _GetInteractionText = getInteractionText;
            _GetInteractionTime = getInteractionTime;
        }

        // Can this interactable be added to the list of potentials
        public virtual bool IsPotentialInteractor(InteractorComponent interactorComponent)
        {
            if (interactorComponent == null)
                return false;

            if (_IsPotentialInteract != null)
                return _IsPotentialInteract(interactorComponent);

            return true;
        }

        // Check the interactor and see if he has the correct item types and state to interact
        public virtual bool IsInteractionValid(InteractorComponent interactorComponent)
        {
            if (interactorComponent == null)
                return false;

            if (_IsInteractionValid != null)
                return _IsInteractionValid(interactorComponent);

            return true;
        }

        public virtual string GetInteractionText(InteractorComponent interactorComponent)
        {
            if (interactorComponent == null)
                return null;

            if (_GetInteractionText != null)
                return _GetInteractionText(interactorComponent);

            return null;
        }

        public virtual float GetInteractionTime(InteractorComponent interactorComponent)
        {
            if (interactorComponent == null)
                return 0f;

            if (_GetInteractionTime != null)
                return _GetInteractionTime(interactorComponent);

            return 0f;
        }

        
        // Interact start that runs on clients and server on state change
        public virtual void InteractStart(InteractorComponent interactor)
        {
            CurrentInteractor = interactor; 

        }

        // Client interact start
        public virtual void InteractEnd(InteractorComponent interactor)
        {

        }

        public virtual void CompleteInteract(InteractorComponent interactor, FInteractorState interactorState)
        {

        }

        private void ToggleInteractStartAudio(bool playAudio)
        {


        }
        
    }
}

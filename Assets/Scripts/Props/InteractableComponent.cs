
using LichLord.Player;
using LichLord.World;
using System;
using UnityEngine;

namespace LichLord
{
    public class InteractableComponent : MonoBehaviour
    {
        public IChunkTrackable Owner { get; protected set; }

        private Func<InteractorComponent, bool> _IsPotentialInteract;
        private Func<InteractorComponent, bool> _IsInteractionValid;
        private Func<InteractorComponent, string> _GetInteractionText;
        private Func<InteractorComponent, int> _GetTicksToComplete;
        private Func<InteractorComponent, EInteractType> _GetInteractType;
        private Func<InteractorComponent, float> _GetInteractDistance;

        public Action<InteractableComponent, InteractorComponent> onInteractStart;
        public Action<InteractableComponent, InteractorComponent> onInteractEnd;
        public Action<InteractableComponent, InteractorComponent> onInteractionComplete;

        public InteractorComponent CurrentInteractor { get; protected set; }

        public int InteractTick;

        public void Activate(IChunkTrackable owner,
            Func<InteractorComponent, bool> isPotentialInteract,
            Func<InteractorComponent, bool> isInteractionValid,
            Func<InteractorComponent, string> getInteractionText,
            Func<InteractorComponent, int> getTicksToComplete,
            Func<InteractorComponent, EInteractType> getInteractType,
            Func<InteractorComponent, float> getInteractDistance)
        {
            Owner = owner;
            _IsPotentialInteract = isPotentialInteract;
            _IsInteractionValid = isInteractionValid;
            _GetInteractionText = getInteractionText;
            _GetTicksToComplete = getTicksToComplete;
            _GetInteractType = getInteractType;
            _GetInteractDistance = getInteractDistance;
        }

        public float GetTimeRemaining(int currentTick)
        {
            if (CurrentInteractor == null)
                return 0;

            int ticksElapsed = currentTick - InteractTick;
            int ticksRemaining = GetTicksToComplete(CurrentInteractor) - ticksElapsed;

            if (ticksRemaining <= 0)
                return 0;

            float tickRate = 32f;
            return ticksRemaining / tickRate;
        }

        public float GetPercentRemaining(float localRenderTime)
        {
            if (CurrentInteractor == null || GetTicksToComplete(CurrentInteractor) <= 0)
                return 0f;

            float tickRate = 32f; // ticks per second
            float interactStartTime = InteractTick / tickRate;
            float totalDuration = GetTicksToComplete(CurrentInteractor) / tickRate;

            float timeElapsed = localRenderTime - interactStartTime;
            float timeRemaining = totalDuration - timeElapsed;

            float percentRemaining = timeRemaining / totalDuration;

            //Debug.Log($"Percent Remaining: {percentRemaining * 100f}%");
            return Mathf.Clamp01(percentRemaining);
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

        public virtual int GetTicksToComplete(InteractorComponent interactorComponent)
        {
            if (interactorComponent == null)
                return 0;

            if (_GetTicksToComplete != null)
                return _GetTicksToComplete(interactorComponent);

            return 0;
        }

        public virtual EInteractType GetInteractType(InteractorComponent interactorComponent)
        {
            if (interactorComponent == null)
                return EInteractType.None;

            if (_GetInteractType != null)
                return _GetInteractType(interactorComponent);

            return EInteractType.None;
        }

        public virtual float GetInteractDistance(InteractorComponent interactorComponent)
        {
            if (interactorComponent == null)
                return 0;

            if (_GetInteractDistance != null)
                return _GetInteractDistance(interactorComponent);

            return 0;
        }

        // Interact start that runs on clients and server on state change
        public virtual void InteractStart(InteractorComponent interactor, int tick)
        {
            CurrentInteractor = interactor;
            InteractTick = tick;
            onInteractStart?.Invoke(this, interactor);
        }

        // Client interact start
        public virtual void InteractEnd(InteractorComponent interactor)
        {
            CurrentInteractor = null;
            onInteractEnd?.Invoke(this, interactor);
        }

        public virtual void CompleteInteract(InteractorComponent interactor)
        {
            CurrentInteractor = null;
            onInteractionComplete?.Invoke(this, interactor);
        }
    }

    public enum EInteractType : byte
    {
        None,
        HarvestNode,
        Dialog,
    }
}

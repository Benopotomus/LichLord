using DG.Tweening;
using DWD.Pooling;
using LichLord.Items;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Buildables
{
    public class RefineryStateComponent : MonoBehaviour
    {
        [SerializeField] private Refinery _refinery;

        [SerializeField] private ERefineryState _currentState = ERefineryState.None;
        public ERefineryState CurrentState => _currentState;

        [SerializeField] private VisualEffectBase[] _activeVisualEffects;
        [SerializeField] private Light[] _activeLights;

        private readonly List<Tween> _lightTweens = new();

        [SerializeField] private int _localProgress = -1;
        public int LocalProgress => _localProgress;

        [SerializeField] private int _nextProgressTick = -1;
        public int NextProgressTick => _nextProgressTick;

        public void UpdateRefineryState(BuildableRuntimeState runtimeState, int tick, bool hasAuthority)
        {
            if (runtimeState.Definition is not RefineryDefinition refineryDefinition)
                return;

            UpdateStateByRecipe(runtimeState, tick, hasAuthority, refineryDefinition);
            UpdateProgress(runtimeState, tick, hasAuthority, refineryDefinition);
            UpdateStateVisuals(runtimeState.GetRefineryState());
        }

        private void UpdateStateVisuals(ERefineryState newState)
        {
            if (_currentState == newState)
                return;

            // stop any light tweens when changing state
            StopLightTweens();

            switch (newState)
            {
                case ERefineryState.None:
                    ToggleEffects(false);
                    ToggleLights(false);
                    break;

                case ERefineryState.Active:
                    ToggleEffects(true);
                    ToggleLights(true);
                    StartLightTweens();
                    break;

                case ERefineryState.Complete:
                case ERefineryState.Disabled:
                    ToggleEffects(false);
                    ToggleLights(false);
                    break;
            }

            _currentState = newState;
        }

        private void ToggleEffects(bool enable)
        {
            foreach (var effect in _activeVisualEffects)
            {
                if (effect)
                    effect.Toggle(enable);
            }
        }

        private void ToggleLights(bool enable)
        {
            foreach (var light in _activeLights)
            {
                if (light)
                    light.enabled = enable;
            }
        }

        private void StartLightTweens()
        {
            foreach (var light in _activeLights)
            {
                if (!light) continue;

                float startIntensity = 0.5f;
                float targetIntensity = 1.5f; // Pulse brighter

                Tween tween = DOTween.To(
                    () => light.intensity,
                    x => light.intensity = x,
                    targetIntensity,
                    0.5f
                )
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

                _lightTweens.Add(tween);
            }
        }

        private void StopLightTweens()
        {
            foreach (var tween in _lightTweens)
                tween.Kill();

            _lightTweens.Clear();
        }

        private void UpdateStateByRecipe(BuildableRuntimeState runtimeState,
            int tick,
            bool hasAuthority,
            RefineryDefinition refineryDefinition)
        {
            ERefineryState currentRefineryState = runtimeState.GetRefineryState();
            List<(int, FItemSlotData)> inSlots = runtimeState.GetRefineryInItemSlotDatas();
            List<(int, FItemSlotData)> outSlots = runtimeState.GetRefineryOutItemSlotDatas();
            RefinementRecipe validRecipe = refineryDefinition.GetValidRecipe(inSlots);

            if (validRecipe == null)
            {
                runtimeState.SetRefineryState(ERefineryState.None);
                runtimeState.SetRefineryProgress(0);
                return;
            }

            if (!validRecipe.CanProgress(outSlots))
            {
                runtimeState.SetRefineryState(ERefineryState.Complete);
                runtimeState.SetRefineryProgress(0);
                return;
            }

            if (currentRefineryState == ERefineryState.Active)
                return;

            runtimeState.SetRefineryState(ERefineryState.Active);
            runtimeState.SetRefineryProgress(0);
        }

        private void UpdateProgress(BuildableRuntimeState runtimeState,
            int tick,
            bool hasAuthority,
            RefineryDefinition refineryDefinition)
        {
            if (runtimeState.GetRefineryState() != ERefineryState.Active)
                return;

            int currentProgress = runtimeState.GetRefineryProgress();

            if (_localProgress != currentProgress)
            {
                _localProgress = currentProgress;
                _nextProgressTick = tick + refineryDefinition.TicksPerProgress;
            }

            if (!hasAuthority)
                return;

            if (tick >= _nextProgressTick)
            {
                currentProgress++;
                runtimeState.SetRefineryProgress(currentProgress);

                if (currentProgress >= refineryDefinition.MaxProgress)
                {
                    ContainerManager containerManager = runtimeState.Context.ContainerManager;
                    List<(int, FItemSlotData)> inSlots = runtimeState.GetRefineryInItemSlotDatas();
                    List<(int, FItemSlotData)> outSlots = runtimeState.GetRefineryOutItemSlotDatas();

                    RefinementRecipe validRecipe = refineryDefinition.GetValidRecipe(inSlots);
                    List<FItemData> completedItems = validRecipe.GetCompletedItems();

                    for (int i = 0; i < inSlots.Count; i++)
                    {
                        FItemData itemData = inSlots[i].Item2.ItemData;
                        ItemDefinition itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID);

                        int stacks = validRecipe.GetStacksToRemove(itemDefinition);
                        containerManager.RemoveItemStacksFromSlot(inSlots[i].Item1, stacks);
                    }

                    for (int i = 0; i < completedItems.Count; i++)
                    {
                        containerManager.AddItemToSlot(outSlots[i].Item1, completedItems[i]);
                    }

                    runtimeState.SetRefineryProgress(0);
                }
            }
            
        }

        public float GetLocalRefineryProgress()
        {
            if (CurrentState != ERefineryState.Active)
                return 0f;

            if (_refinery.RuntimeState.Definition is not RefineryDefinition refineryDefinition)
                return 0f;

            float progressPercent = (float)LocalProgress / (float)refineryDefinition.MaxProgress;
            int ticksToNextProgress = (NextProgressTick - _refinery.Context.Runner.Tick);

            float progressStep = (1f / (float)refineryDefinition.MaxProgress);
            float additionalProgress = (1f - ((float)ticksToNextProgress / refineryDefinition.TicksPerProgress)) * progressStep;

            return progressPercent + additionalProgress;
        }
    }
}

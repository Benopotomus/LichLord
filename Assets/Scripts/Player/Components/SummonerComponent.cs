using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace LichLord
{
    public class SummonerComponent : NetworkBehaviour
    {
        [SerializeField] private PlayerCharacter _pc;

        [Networked] 
        private sbyte _selectedIndex { get; set; }
        public ELoadoutSlot SelectedSlot
        {
            get
            {
                switch (_selectedIndex)
                {
                    case 0:
                        return ELoadoutSlot.Summon_00;
                    case 1:
                        return ELoadoutSlot.Summon_01;
                    case 2:
                        return ELoadoutSlot.Summon_02;
                    case 3:
                        return ELoadoutSlot.Summon_03;
                    case 4:
                        return ELoadoutSlot.Summon_04;
                    default:
                        return ELoadoutSlot.None;
                }
            }
        }

        [Networked] 
        private sbyte _activeSummoningIndex { get; set; }

        [Networked]
        private TickTimer _activeManeuverTimer { get; set; }
        private int _activeManeuverTick;

        // Current upper body blend amount
        private float _moveSpeedMultiplier = 1f;

        public float GetMoveSpeedMultiplier()
        { 
            if(_activeSummoningIndex < 0)
                return 1f;

            return _moveSpeedMultiplier;
        }

        public override void Spawned()
        {
            base.Spawned();
            ReplicateToAll(false);
        }

        public void ProcessInput(ref FGameplayInput input)
        {
            ProcessSummonSelection(ref input);
            ProcessManeuverActivation(ref input);
            ProcessActiveManeuver(ref input);
        }

        public void OnFixedUpdate()
        {
            ProcessManeuverExpiration();
        }

        private void ProcessActiveManeuver(ref FGameplayInput input)
        {
            /*
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            if (activeManeuver.InputType == EInputType.Held)
            {
                if (input.FireHeld)
                {
                    _activeManeuverTimer = TickTimer.CreateFromSeconds(Runner, activeManeuver.Duration);
                }
            }

            if (!_activeManeuverTimer.ExpiredOrNotRunning(Runner))
            {
                int ticksSinceStart = Runner.Tick - _activeManeuverTick;
                activeManeuver.SustainExecute(_pc, Runner, ticksSinceStart);
            }
            */
        }

        private void ProcessManeuverExpiration()
        {
            /*
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            if (_activeManeuverTimer.ExpiredOrNotRunning(Runner))
            {
                activeManeuver.EndExecute(_pc, Runner);
                return;
            }
            */
        }

        private void ProcessManeuverActivation(ref FGameplayInput input)
        {

        }

        private List<int> GetValidSummonIndexes()
        {
            List<int> actions = new List<int>();

            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_00).IsValid())
                actions.Add(0);
            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_01).IsValid())
                actions.Add(1);
            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_02).IsValid())
                actions.Add(2);
            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_03).IsValid())
                actions.Add(3);
            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_04).IsValid())
                actions.Add(4);

            return actions;
        }

        private void ProcessSummonSelection(ref FGameplayInput input)
        {
            List<int> validActions = GetValidSummonIndexes();

            // If no valid actions, reset _selectedIndex and exit
            if (validActions.Count == 0)
            {
                _selectedIndex = -1;
                UpdateActionSelection(_selectedIndex);
                return;
            }

            // Check if current _selectedIndex is invalid
            if (!validActions.Contains(_selectedIndex))
            {
                // Find the next valid index
                int currentPos = 0;
                if (_selectedIndex >= 0)
                {
                    // Try to find the next valid index after the current _selectedIndex
                    for (int i = 0; i < validActions.Count; i++)
                    {
                        if (validActions[i] > _selectedIndex)
                        {
                            currentPos = i;
                            break;
                        }
                    }
                }
                // Update to the next valid index
                _selectedIndex = (sbyte)validActions[currentPos];
                UpdateActionSelection(_selectedIndex);
                //Debug.Log($"[ActionManager] Current index {_selectedIndex} was invalid, cycled to {validActions[currentPos]}");
            }

            int newIndex = -1;
            if (input.ScrollDelta != 0)
            {
                // Find the current position in validActions
                int currentPos = validActions.IndexOf(_selectedIndex);
                // Move to next/previous valid index based on scroll direction
                int delta = input.ScrollDelta > 0 ? 1 : -1;
                currentPos = (currentPos + delta + validActions.Count) % validActions.Count;
                newIndex = validActions[currentPos];
                //Debug.Log($"[ActionManager] ScrollDelta={input.ScrollDelta}, CurrentPos={currentPos}, NewIndex={newIndex}");
            }
            else if (input.ActionSelection > 0)
            {
                // Check if the selected index is valid
                int selectedIndex = input.ActionSelection - 1;
                if (validActions.Contains(selectedIndex))
                {
                    newIndex = selectedIndex;
                    //Debug.Log($"[ActionManager] ActionSelection={input.ActionSelection}, NewIndex={newIndex}");
                }
                else
                {
                    //Debug.Log($"[ActionManager] Ignored invalid ActionSelection={input.ActionSelection} (not in validActions)");
                    return;
                }
            }

            // If no valid new index, exit
            if (newIndex < 0)
                return;

            // If the new index is the same as the current, exit
            if (newIndex == _selectedIndex)
                return;

            // Update to the new valid index
            UpdateActionSelection(newIndex);
        }

        private void UpdateActionSelection(int newIndex)
        {
            _selectedIndex = (sbyte)newIndex;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyStartExecute(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(maneuverDefinitionID);

            int weaponId = _pc.Weapons.GetWeaponID();
            var animationState = maneuver.UpperBodyAnimationStates[weaponId];

            if (!maneuver.Fullbody)
            {
                _pc.AnimationController.SetAnimationForUpperBodyTrigger(animationState);
            }

            _pc.Aim.TargetPitchOffset = animationState.PitchOffset;
            _pc.Aim.TargetYawOffset = animationState.YawOffset;
            _pc.Aim.TargetRollOffset = animationState.RollOffset;

            foreach (var action in maneuver.ManeuverActions)
            {
                action.Execute(_pc, Runner);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyEndExecute(ushort maneuverDefinitionID)
        {

            _pc.Aim.TargetPitchOffset = 0;
            _pc.Aim.TargetYawOffset = 0;

            _activeSummoningIndex = -1;
        }

    }
}
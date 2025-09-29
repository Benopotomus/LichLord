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

            if (HasStateAuthority)
            {


            }
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
     

        private void ProcessSummonSelection(ref FGameplayInput input)
        {
            /*
            int newIndex = -1;
            if (input.ScrollDelta != 0 && _availableManeuvers.Count > 1)
            {
                int delta = input.ScrollDelta > 0 ? 1 : -1;
                newIndex = (_selectedIndex + delta + _availableManeuvers.Count) % _availableManeuvers.Count;
                //Debug.Log($"[ActionManager] ScrollDelta={input.ScrollDelta}, Delta={delta}, NewIndex={newIndex}");
            }

            if (input.ActionSelection > 0)
            {
                //Debug.Log($"[ActionManager] ActionSelection={input.ActionSelection}");
                newIndex = input.ActionSelection - 1;
            }

            if (newIndex >= _availableManeuvers.Count)
            {
                //Debug.Log($"[ActionManager] Ignored invalid ActionSelection={input.ActionSelection} (exceeds availableActions.Count={availableActions.Count})");
                return;
            }

            if (newIndex < 0)
                return;

            if (newIndex == _selectedIndex)
                return;

            UpdateActionSelection(newIndex);
            */
        }

        private void UpdateActionSelection(int newIndex)
        {
            /*
            if (HasStateAuthority)
            {
                if (_selectedIndex >= 0 && _selectedIndex < _availableManeuvers.Count)
                {
                    _availableManeuvers[_selectedIndex].DeselectAction(_pc, Runner);
                }

                _selectedIndex = (sbyte)newIndex;
                if (newIndex >= 0 && newIndex < _availableManeuvers.Count)
                {
                    _availableManeuvers[newIndex].SelectAction(_pc, Runner);
                }

                if (newIndex >= 0 && newIndex < _availableManeuvers.Count)
                {
                    Debug.Log($"[ActionManager] Selected action: {_availableManeuvers[newIndex].ManeuverName} (Index: {newIndex})");
                }
                else
                {
                    Debug.Log("[ActionManager] Action selection cleared");
                }
            }
            */
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
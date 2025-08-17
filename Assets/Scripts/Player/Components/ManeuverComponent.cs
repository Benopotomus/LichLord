using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace LichLord
{
    public class ManeuverComponent : NetworkBehaviour
    {
        [SerializeField] private PlayerCharacter _pc;

        [Header("Maneuvers Setup")]
        [SerializeField] private List<ManeuverDefinition> _availableManeuvers = new List<ManeuverDefinition>();
        public IReadOnlyList<ManeuverDefinition> AvailableManeuvers => _availableManeuvers;

        [SerializeField] public Transform ActionSpawnPoint; // Where actions originate

        [Networked] private sbyte _selectedIndex { get; set; }
        [Networked] private sbyte _activeManeuverIndex { get; set; }

        [Networked]
        private TickTimer _activeManeuverTimer { get; set; }
        private int _activeManeuverTick;

        [Networked, Capacity(8)]
        private NetworkDictionary<sbyte, TickTimer> _maneuverCooldownTimers { get; }

        // Current upper body blend amount
        private float _moveSpeedMultiplier = 1f;

        public float GetMoveSpeedMultiplier()
        { 
            if(_activeManeuverIndex < 0)
                return 1f;

            return _moveSpeedMultiplier;
        }

        public override void Spawned()
        {
            base.Spawned();
            ReplicateToAll(false);

            if (HasStateAuthority)
            {
                if (_availableManeuvers.Count > 0)
                {
                    _selectedIndex = 0;
                    _availableManeuvers[0].SelectAction(_pc, Runner);
                    Debug.Log($"[ActionManager] Automatically selected action: {_availableManeuvers[0].ManeuverName} (Index: 0)");
                }
                else
                {
                    _selectedIndex = -1;
                    Debug.LogWarning("[ActionManager] No actions available, no initial selection set.");
                }

                for (int i = 0; i < _availableManeuvers.Count; i++)
                {
                    _activeManeuverTimer = TickTimer.None;
                    _maneuverCooldownTimers.Set((sbyte)i, TickTimer.None);
                }

            }
        }

        public void ProcessInput(ref FGameplayInput input)
        {
            ProcessManeuverSelection(ref input);
            ProcessManeuverActivation(ref input);
            ProcessActiveManeuver(ref input);
        }

        public void OnFixedUpdate()
        {
            ProcessManeuverExpiration();
        }

        private void ProcessActiveManeuver(ref FGameplayInput input)
        {
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
        }

        private void ProcessManeuverExpiration()
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            if (_activeManeuverTimer.ExpiredOrNotRunning(Runner))
            {
                activeManeuver.EndExecute(_pc, Runner);
                return;
            }
        }

        private void ProcessManeuverActivation(ref FGameplayInput input)
        {
            // if the selected index is out of range, early out
            if (_selectedIndex < 0 || _selectedIndex >= _availableManeuvers.Count)
                return;
 
            // Cache current selected maneuver
            ManeuverDefinition selectedManeuver = _availableManeuvers[_selectedIndex];

            // if the cooldown timer doesn't exist for this selected index, early out
            if (!_maneuverCooldownTimers.TryGet(_selectedIndex, out var cooldownTimer))
            {
                Debug.Log("Maneuver cooldown timer doesn't exist for index " + _selectedIndex);
                return;
            }

            // If the event is on cooldown, early out
            if (!cooldownTimer.ExpiredOrNotRunning(Runner))
            {
                //Debug.Log("Maneuver cooldown timer is running for " + _selectedIndex);
                return;
            }

            // Cache current selected maneuver
            ManeuverDefinition activeManeuver = GetActiveManeuver();

            if (activeManeuver != null)
                return;

            if (input.Fire)
            {
                //Debug.Log($"[ActionManager] Executing action: {GetSelectedManeuver().ManeuverName} (Index: {_selectedIndex})");

                _activeManeuverTick = Runner.Tick;
                _activeManeuverIndex = _selectedIndex;
                _activeManeuverTimer = TickTimer.CreateFromSeconds(Runner, selectedManeuver.Duration);

                activeManeuver = GetActiveManeuver();
                activeManeuver.StartExecute(_pc, Runner);
            }
        }

        public ManeuverDefinition GetActiveManeuver()
        {
            if(_activeManeuverIndex < 0)
                return null;

            return _availableManeuvers[_activeManeuverIndex];
        }

        public ManeuverDefinition GetSelectedManeuver()
        {
            if (_selectedIndex < 0)
                return null;

            return _availableManeuvers[_selectedIndex];
        }

        public float GetCooldownPercent(int slot)
        {
            // if the cooldown timer doesn't exist for this selected index, early out
            if (!_maneuverCooldownTimers.TryGet((sbyte)slot, out TickTimer cooldownTimer))
            {
                return 0f;
            }

            ManeuverDefinition definition = _availableManeuvers[slot];
            if (definition.Cooldown == 0)
                return 0;

            float? remainingTime = cooldownTimer.RemainingTime(Runner); // Use float? to accept nullable float

            // Handle the nullable case
            if (!remainingTime.HasValue)
            {
                return 0f; // Or another default value, depending on your requirements
            }

            return (remainingTime.Value / definition.Cooldown);
        }

        public void OnRender()
        {
            float deltaTime = Time.deltaTime;
        }

        public void UpdateMoveSpeed(float deltaTime)
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();

            if (activeManeuver != null)
            {
                _moveSpeedMultiplier = Mathf.Lerp(_moveSpeedMultiplier, activeManeuver.MovementSpeedMultiplier, deltaTime * 4f);
            }
            else
            {
                _moveSpeedMultiplier = Mathf.Lerp(_moveSpeedMultiplier, 1, deltaTime * 4f);
            }
        }

        private void ProcessManeuverSelection(ref FGameplayInput input)
        {
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
        }

        private void UpdateActionSelection(int newIndex)
        {
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
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyStartExecute(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(maneuverDefinitionID);

            if (!maneuver.Fullbody)
            {
                _pc.AnimationController.SetAnimationForUpperBodyTrigger(maneuver.UpperbodyTriggerNumber);
            }

            _pc.Aim.TargetPitchOffset = maneuver.PitchOffset;
            _pc.Aim.TargetYawOffset = maneuver.YawOffset;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyEndExecute(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(maneuverDefinitionID);

            if (!maneuver.Fullbody)
            {
                _pc.AnimationController.SetAnimationForUpperBodyTrigger(0);
            }

            if (_maneuverCooldownTimers.TryGet(_activeManeuverIndex, out TickTimer cooldownTimer))
            {
                cooldownTimer = TickTimer.CreateFromSeconds(Runner, maneuver.Cooldown);
                _maneuverCooldownTimers.Set(_selectedIndex, cooldownTimer);
            }

            _pc.Aim.TargetPitchOffset = 0;
            _pc.Aim.TargetYawOffset = 0;

            _activeManeuverIndex = -1;
        }

        public int GetAvailableActionsCount()
        {
            return _availableManeuvers.Count;
        }
       
    }
}
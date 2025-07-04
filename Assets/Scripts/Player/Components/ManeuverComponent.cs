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

        [Networked]
        private sbyte _activeUpperBodyTriggerNumber { get; set; }

        [Networked, Capacity(8)]
        private NetworkDictionary<sbyte, TickTimer> _maneuverCooldownTimers { get; }

        [SerializeField] private GameObject gunModel; // Gun model to toggle visibility
        [SerializeField] private ParticleSystem gunMuzzleParticle; // ParticleSystem on gunModel for muzzle flash

        private int _animIDUpperBodyTriggerNumber = Animator.StringToHash("UpperBodyTriggerNumber");
        private int _animIDUpperBodyTrigger = Animator.StringToHash("UpperBodyTrigger");
        private int _animIDUpperBodyBlend = Animator.StringToHash("UpperBodyBlend");

        // Current upper body blend amount
        private float _upperBodyBlend = 0f;
        private float _moveSpeedMultiplier = 1f;

        public override void Spawned()
        {
            base.Spawned();

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
                else
                {
                    _activeUpperBodyTriggerNumber = 0;
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
                //Debug.Log($"[ActionManager] Executing action: {selectedAction.ActionName} (Index: {SelectedActionIndex})");

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

            UpdateMoveSpeed(deltaTime);
            UpdateWeaponModel();
            UpdateAnimation(deltaTime);
        }

        private void UpdateAnimation(float deltaTime)
        {
            if (!_activeManeuverTimer.ExpiredOrNotRunning(Runner))
            {
                _upperBodyBlend = Mathf.Clamp01(_upperBodyBlend + (deltaTime * 8f));
            }
            else
            {
                _upperBodyBlend = Mathf.Clamp01(_upperBodyBlend - (deltaTime * 4f));
            }

            _pc.Animator.SetFloat(_animIDUpperBodyBlend, _upperBodyBlend);
        }

        private void UpdateMoveSpeed(float deltaTime)
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

            _pc.Movement.SetManeuverSpeedMultiplier(_moveSpeedMultiplier);
        }

        public void UpdateWeaponModel()
        {
            ManeuverDefinition selectedAction = GetSelectedManeuver();
            if (selectedAction is GunManeuverDefinition gunActionData)
            {
                if (gunModel != null)
                {
                    gunModel.SetActive(true);
                }
            }

            if (gunModel != null)
            {
                gunModel.SetActive(false);
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

        public void SetUpperBodyTriggerNumber(sbyte newTriggerNumber)
        {
            _activeUpperBodyTriggerNumber = newTriggerNumber;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyStartExecute(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(maneuverDefinitionID);

            if (!maneuver.Fullbody)
            {
                _pc.Animator.SetFloat(_animIDUpperBodyBlend, 0.01f);
                _activeUpperBodyTriggerNumber = (sbyte)maneuver.UpperbodyTriggerNumber;
                _pc.Animator.SetInteger(_animIDUpperBodyTriggerNumber, _activeUpperBodyTriggerNumber);
                _pc.Animator.SetTrigger(_animIDUpperBodyTrigger);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyEndExecute(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(maneuverDefinitionID);

            if (!maneuver.Fullbody)
            {
                _pc.Animator.SetFloat(_animIDUpperBodyBlend, 0.00f);
                _activeUpperBodyTriggerNumber = 0;
                _pc.Animator.SetInteger(_animIDUpperBodyTriggerNumber, _activeUpperBodyTriggerNumber);
                _pc.Animator.SetTrigger(_animIDUpperBodyTrigger);
            }

            if (_maneuverCooldownTimers.TryGet(_activeManeuverIndex, out TickTimer cooldownTimer))
            {
                cooldownTimer = TickTimer.CreateFromSeconds(Runner, maneuver.Cooldown);
                _maneuverCooldownTimers.Set(_selectedIndex, cooldownTimer);
            }
            
            _activeManeuverIndex = -1;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ExecuteGunAction(NetworkId playerId, int maneuverID, Vector3 spawnPosition, Vector3 targetPosition, Vector3 hitPosition, Vector3 hitNormal)
        {
            ManeuverDefinition maneuver = _availableManeuvers[_selectedIndex];

            Vector3 direction = (targetPosition - spawnPosition).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            if (maneuver.TableID != maneuverID)
                return;

            /*
            // Play muzzle effect
            if (maneuver.ActionEffect != null)
            {
                var effectInstance = DWDObjectPool.Instance.SpawnAt(maneuver.ActionEffect, spawnPosition, rotation) as VisualEffectBase;
                effectInstance.Initialize();
            }
            */

            if (maneuver.ActionSound != null)
            {
                AudioSource.PlayClipAtPoint(maneuver.ActionSound, spawnPosition);
            }

            if (_pc.Animator != null)
            {
                _pc.Animator.SetTrigger("UpperBodyTrigger");
                
            }
        }

        public int GetAvailableActionsCount()
        {
            return _availableManeuvers.Count;
        }

       
    }
}
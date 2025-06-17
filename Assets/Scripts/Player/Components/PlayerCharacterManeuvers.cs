using UnityEngine;
using Fusion;
using System.Collections.Generic;
using DWD.Pooling;
using Pathfinding.RVO;
using System;

namespace LichLord
{
    public class PlayerCharacterManeuvers : NetworkBehaviour
    {
        [SerializeField] private PlayerCharacter _pc;

        [Header("Action Setup")]
        [SerializeField] private List<ManeuverDefinition> _availableManeuvers = new List<ManeuverDefinition>();
        public List<ManeuverDefinition> AvailableManeuvers => _availableManeuvers;
        
        [SerializeField] public Transform ActionSpawnPoint; // Where actions originate

        [Networked] private sbyte _selectedIndex { get; set; }
        [Networked] private sbyte _activeManeuverIndex { get; set; }

        [Networked]
        private TickTimer _activeManeuverTimer { get; set; }
        private int _activeManeuverTick;

        [Networked, Capacity(8)]
        private NetworkDictionary<sbyte, TickTimer> _maneuverCooldownTimers { get; }

        [SerializeField] private GameObject gunModel; // Gun model to toggle visibility
        [SerializeField] private ParticleSystem gunMuzzleParticle; // ParticleSystem on gunModel for muzzle flash

        private int _animIDTriggerNumber = Animator.StringToHash("UpperBodyTriggerNumber");
        private int _animIDUpperBodyTrigger = Animator.StringToHash("UpperBodyTrigger");
        private int _animIDUpperBodyBlend = Animator.StringToHash("UpperBodyBlend");

        // Current upper body blend amount
        private float _upperBodyBlend = 0f;

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

        public void OnFixedUpdate(ref FGameplayInput input)
        {
            if (!HasStateAuthority) 
                return;

            ProcessActionSelection(ref input);
            ProcessManeuverActivation(ref input);
            ProcessActiveManeuver(ref input);
        }

        private void ProcessActiveManeuver(ref FGameplayInput input)
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            if (_activeManeuverTimer.ExpiredOrNotRunning(Runner))
                return;

            int ticksSinceStart = Runner.Tick - _activeManeuverTick;
            activeManeuver.SustainExecute(_pc, Runner, ticksSinceStart);

            //Debug.Log(ticksSinceStart);
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
                Debug.Log("Fire Pressed");

                //Debug.Log($"[ActionManager] Executing action: {selectedAction.ActionName} (Index: {SelectedActionIndex})");

                _activeManeuverTick = Runner.Tick;
                _activeManeuverIndex = _selectedIndex;
                _activeManeuverTimer = TickTimer.CreateFromSeconds(Runner, selectedManeuver.Duration);

                cooldownTimer = TickTimer.CreateFromSeconds(Runner, selectedManeuver.Cooldown);
                _maneuverCooldownTimers.Set(_selectedIndex, cooldownTimer);

                activeManeuver = GetActiveManeuver();
                activeManeuver.StartExecute(_pc, Runner);
            }

            if (cooldownTimer.ExpiredOrNotRunning(Runner))
            {
                _pc.Movement.SetCastSpeedMultiplier(1f);
            }
            else
            {
                _pc.Movement.SetCastSpeedMultiplier(_availableManeuvers[_selectedIndex].MovementSpeedMultiplier);
            }
        }

        private ManeuverDefinition GetActiveManeuver()
        {
            if(_activeManeuverIndex < 0)
                return null;

            if (_activeManeuverTimer.ExpiredOrNotRunning(Runner))
                return null;

            return _availableManeuvers[_activeManeuverIndex];
        }

        private ManeuverDefinition GetSelectedManeuver()
        {
            if (_selectedIndex < 0)
                return null;

            return _availableManeuvers[_selectedIndex];
        }

        public void OnRender()
        {
            float deltaTime = Time.deltaTime;
            UpdateWeaponModel();
            UpdateAnimation(deltaTime);
        }


        private void UpdateAnimation(float deltaTime)
        {

            if (!_activeManeuverTimer.ExpiredOrNotRunning(Runner))
            {
                _upperBodyBlend = Mathf.Clamp01(_upperBodyBlend + (deltaTime * 4f));
            }
            else
            {
                _upperBodyBlend = Mathf.Clamp01(_upperBodyBlend - (deltaTime * 4f));
            }

            _pc.Animator.SetFloat(_animIDUpperBodyBlend, _upperBodyBlend);

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

        private void ProcessActionSelection(ref FGameplayInput input)
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
        public void RPC_NotifyActionExecution(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(maneuverDefinitionID);

            _pc.Animator.SetInteger(_animIDTriggerNumber, maneuver.AnimationTriggerNumber);

            if (!maneuver.Fullbody)
            {
                _pc.Animator.SetFloat(_animIDUpperBodyBlend, 0.01f);
                _pc.Animator.SetTrigger(_animIDUpperBodyTrigger);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ExecuteGunAction(NetworkId playerId, int maneuverID, Vector3 spawnPosition, Vector3 targetPosition, Vector3 hitPosition, Vector3 hitNormal)
        {
            ManeuverDefinition maneuver = _availableManeuvers[_selectedIndex];

            Vector3 direction = (targetPosition - spawnPosition).normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            if (maneuver.TableID != maneuverID)
                return;

            // Play muzzle effect
            if (maneuver.ActionEffect != null)
            {
                var effectInstance = DWDObjectPool.Instance.SpawnAt(maneuver.ActionEffect, spawnPosition, rotation) as VisualEffectBase;
                effectInstance.Initialize();
            }

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
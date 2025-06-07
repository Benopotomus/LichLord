using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Starter.Shooter;
using LichLord.Projectiles;
using System.Linq;
using DWD.Pooling;
using LichLord.NonPlayerCharacters;

namespace LichLord
{
    public class CreatureManeuvers : NetworkBehaviour
    {
        [Header("Action Setup")]
        [SerializeField] private List<ManeuverDefinition> availableActions = new List<ManeuverDefinition>();
        [SerializeField] public Transform ActionSpawnPoint; // Where actions originate
        [SerializeField] private PlayerCreature _playerCreature;
        [Networked] private int SelectedActionIndex { get; set; }
        [Networked] private TickTimer CooldownTimer { get; set; }
        [Networked] private TickTimer AnimationTimer { get; set; }

        [SerializeField] private GameObject gunModel; // Gun model to toggle visibility
        [SerializeField] private ParticleSystem gunMuzzleParticle; // ParticleSystem on gunModel for muzzle flash

        private bool _hasInitializedSelection;

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority)
            {
                if (availableActions.Count > 0)
                {
                    SelectedActionIndex = 0;
                    _hasInitializedSelection = true;
                    availableActions[0].SelectAction(_playerCreature, Runner);
                    RPC_LogActionSelection(0);
                    Debug.Log($"[ActionManager] Automatically selected action: {availableActions[0].ActionName} (Index: 0)");
                }
                else
                {
                    SelectedActionIndex = -1;
                    Debug.LogWarning("[ActionManager] No actions available, no initial selection set.");
                }

                CooldownTimer = TickTimer.None;
                AnimationTimer = TickTimer.None;
            }
        }

        public void ProcessInput(FGameplayInput input)
        {
            if (!HasStateAuthority) return;

            ProcessActionSelection(input);

            if (input.Fire)
            {
                //Debug.Log("Fire Pressed");

                if (SelectedActionIndex < 0 || SelectedActionIndex >= availableActions.Count)
                {
                    //Debug.Log("[ActionManager] Fire input (LMB) ignored: No action selected or invalid index.");
                    return;
                }

                if (!CooldownTimer.ExpiredOrNotRunning(Runner))
                {
                    //Debug.Log($"[ActionManager] Fire input (LMB) ignored: Action {availableActions[SelectedActionIndex].ActionName} on cooldown.");
                    return;
                }

                ManeuverDefinition selectedAction = availableActions[SelectedActionIndex];
                //Debug.Log($"[ActionManager] Executing action: {selectedAction.ActionName} (Index: {SelectedActionIndex})");
                selectedAction.Execute(_playerCreature, Runner);
                CooldownTimer = TickTimer.CreateFromSeconds(Runner, selectedAction.Cooldown);
                AnimationTimer = TickTimer.CreateFromSeconds(Runner, selectedAction.AnimationDuration);
                RPC_NotifyActionExecution(selectedAction.ActionName, selectedAction.AnimationTrigger);
            }

            if (CooldownTimer.ExpiredOrNotRunning(Runner))
            {
                _playerCreature.Movement.SetCastSpeedMultiplier(1f);
            }
            else
            {
                _playerCreature.Movement.SetCastSpeedMultiplier(availableActions[SelectedActionIndex].MovementSpeedMultiplier);
            }
        }

        public void OnRender()
        {
            UpdateWeaponModel();
        }

        public void UpdateWeaponModel()
        {
            ManeuverDefinition selectedAction = availableActions[SelectedActionIndex];
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

        private void ProcessActionSelection(FGameplayInput input)
        {
            int newIndex = -1;
            if (input.ScrollDelta != 0 && availableActions.Count > 1)
            {
                int delta = input.ScrollDelta > 0 ? 1 : -1;
                newIndex = (SelectedActionIndex + delta + availableActions.Count) % availableActions.Count;
                Debug.Log($"[ActionManager] ScrollDelta={input.ScrollDelta}, Delta={delta}, NewIndex={newIndex}");
            }

            if (input.ActionSelection > 0)
            {
                Debug.Log($"[ActionManager] ActionSelection={input.ActionSelection}");
                newIndex = input.ActionSelection - 1;
            }

            if (newIndex >= availableActions.Count)
            {
                Debug.Log($"[ActionManager] Ignored invalid ActionSelection={input.ActionSelection} (exceeds availableActions.Count={availableActions.Count})");
                return;
            }

            if (newIndex < 0)
                return;

            if (newIndex == SelectedActionIndex)
                return;

            UpdateActionSelection(newIndex);
        }

        private void UpdateActionSelection(int newIndex)
        {
            if (HasStateAuthority)
            {
                if (SelectedActionIndex >= 0 && SelectedActionIndex < availableActions.Count)
                {
                    availableActions[SelectedActionIndex].DeselectAction(_playerCreature, Runner);
                }

                SelectedActionIndex = newIndex;
                _hasInitializedSelection = newIndex >= 0;
                if (newIndex >= 0 && newIndex < availableActions.Count)
                {
                    availableActions[newIndex].SelectAction(_playerCreature, Runner);
                }

                RPC_LogActionSelection(newIndex);

                if (newIndex >= 0 && newIndex < availableActions.Count)
                {
                    Debug.Log($"[ActionManager] Selected action: {availableActions[newIndex].ActionName} (Index: {newIndex})");
                }
                else
                {
                    Debug.Log("[ActionManager] Action selection cleared");
                }
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_LogActionSelection(int actionIndex)
        {
            if (actionIndex == -1)
            {
                Debug.Log("[ActionManager] Player cleared action selection");
            }
            else if (availableActions.Count > actionIndex)
            {
                Debug.Log($"[ActionManager] Player selected action: {availableActions[actionIndex].ActionName} (Index: {actionIndex})");
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_NotifyActionExecution(string actionName, string animationTrigger)
        {
            //Debug.Log($"[ActionManager] Player executed action: {actionName}");
            if (_playerCreature.Animator != null && !string.IsNullOrEmpty(animationTrigger))
            {
                _playerCreature.Animator.SetTrigger(animationTrigger);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ExecuteGunAction(NetworkId playerId, int maneuverID, Vector3 spawnPosition, Vector3 targetPosition, Vector3 hitPosition, Vector3 hitNormal)
        {
            ManeuverDefinition maneuver = availableActions[SelectedActionIndex];

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

            if (_playerCreature.Animator != null && !string.IsNullOrEmpty(maneuver.AnimationTrigger))
            {
                _playerCreature.Animator.SetTrigger(maneuver.AnimationTrigger);
            }
        }

        public int GetAvailableActionsCount()
        {
            return availableActions.Count;
        }
    }
}
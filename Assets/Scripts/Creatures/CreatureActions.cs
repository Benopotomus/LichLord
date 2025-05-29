using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Starter.Shooter;

namespace LichLord
{
    public class CreatureActions : NetworkBehaviour
    {
        [Header("Action Setup")]
        [SerializeField] private List<ActionData> availableActions = new List<ActionData>();
        [SerializeField] private Transform actionSpawnPoint; // Where actions originate (e.g., camera for gun)
        [SerializeField] private GameObject gunModel; // Gun model to toggle visibility
        [SerializeField] private ParticleSystem gunMuzzleParticle; // ParticleSystem on gunModel for muzzle flash

        [Networked] private int SelectedActionIndex { get; set; } // Current selected action index (-1 for none)
        [Networked] private TickTimer CooldownTimer { get; set; }
        [Networked] private TickTimer AnimationTimer { get; set; }

        [SerializeField] private PlayerCreature _playerCreature;

        private bool _hasInitializedSelection; // Track if initial selection is set

        public override void Spawned()
        {
            base.Spawned();

            if (HasStateAuthority)
            {
                // Automatically select element 0 (slot 1) if available
                if (availableActions.Count > 0)
                {
                    SelectedActionIndex = 0;
                    _hasInitializedSelection = true;
                    RPC_LogActionSelection(0); // Notify clients of initial selection
                    Debug.Log($"[ActionManager] Automatically selected action: {availableActions[0].ActionName} (Type: {availableActions[0].Type}, Index: 0)");
                }
                else
                {
                    SelectedActionIndex = -1;
                    Debug.LogWarning("[ActionManager] No actions available, no initial selection set.");
                }
                CooldownTimer = TickTimer.None;
                AnimationTimer = TickTimer.None;
            }

            UpdateGunModelVisibility();
        }

        // Called From FSM
        public void ProcessInput(FGameplayInput input)
        {
            if (!HasStateAuthority) return;

            ProcessActionSelection(input);

            if (input.Fire)
            {
                Debug.Log("Fire Pressed");

                if (SelectedActionIndex < 0 || SelectedActionIndex >= availableActions.Count)
                {
                    Debug.Log("[ActionManager] Fire input (LMB) ignored: No action selected or invalid index.");
                    return;
                }

                if (!CooldownTimer.ExpiredOrNotRunning(Runner))
                {
                    Debug.Log($"[ActionManager] Fire input (LMB) ignored: Action {availableActions[SelectedActionIndex].ActionName} on cooldown.");
                    return;
                }

                if (!AnimationTimer.ExpiredOrNotRunning(Runner))
                {
                    Debug.Log($"[ActionManager] Fire input (LMB) ignored: Action {availableActions[SelectedActionIndex].ActionName} animation in progress.");
                    return;
                }

                Debug.Log($"[ActionManager] Executing action: {availableActions[SelectedActionIndex].ActionName} (Index: {SelectedActionIndex})");
                ExecuteAction(availableActions[SelectedActionIndex]);
                CooldownTimer = TickTimer.CreateFromSeconds(Runner, availableActions[SelectedActionIndex].Cooldown);
                AnimationTimer = TickTimer.CreateFromSeconds(Runner, availableActions[SelectedActionIndex].AnimationDuration);
                RPC_NotifyActionExecution(availableActions[SelectedActionIndex].ActionName, availableActions[SelectedActionIndex].AnimationTrigger);
            }

            if (AnimationTimer.ExpiredOrNotRunning(Runner))
            {
                _playerCreature.Movement.SetCastSpeedMultiplier(1f);
            }
            else
            {
                _playerCreature.Movement.SetCastSpeedMultiplier(availableActions[SelectedActionIndex].MovementSpeedMultiplier);
            }
        }

        private void ProcessActionSelection(FGameplayInput input)
        {
            int newIndex = -1; // Default to no selection
            // Handle scroll delta
            if (input.ScrollDelta != 0 && availableActions.Count > 1)
            {
                int delta = input.ScrollDelta > 0 ? 1 : -1; // 120 -> 1, -120 -> -1
                newIndex = (SelectedActionIndex + delta + availableActions.Count) % availableActions.Count;
                Debug.Log($"[ActionManager] ScrollDelta={input.ScrollDelta}, Delta={delta}, NewIndex={newIndex}");
            }

            if (input.ActionSelection > 0)
            {
                Debug.Log($"[ActionManager] ActionSelection={input.ActionSelection}");
                newIndex = input.ActionSelection - 1; // 1-based to 0-based
            }

            // Early exit for invalid ActionSelection
            if (newIndex >= availableActions.Count)
            {
                Debug.Log($"[ActionManager] Ignored invalid ActionSelection={input.ActionSelection} (exceeds availableActions.Count={availableActions.Count})");
                return;
            }

            if (newIndex < 0)
                return;

            // Skip if no change in selection
            if (newIndex == SelectedActionIndex)
                return;

            // Update selection
            UpdateActionSelection(newIndex);
        }

        private void UpdateActionSelection(int newIndex)
        {
            SelectedActionIndex = newIndex;
            _hasInitializedSelection = newIndex >= 0; // True if a valid action is selected
            RPC_LogActionSelection(newIndex);

            if (newIndex >= 0 && newIndex < availableActions.Count)
            {
                Debug.Log($"[ActionManager] Selected action: {availableActions[newIndex].ActionName} (Index: {newIndex})");
            }
            else
            {
                Debug.Log("[ActionManager] Action selection cleared");
            }

            UpdateGunModelVisibility();
        }

        private void ExecuteAction(ActionData data)
        {
            if (data.Type == ActionType.Melee)
            {
                Ray ray = new Ray(actionSpawnPoint.position, actionSpawnPoint.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, data.Range))
                {
                    var health = hit.collider.GetComponentInParent<Health>();
                    if (health != null)
                    {
                        health.Killed = enemyHealth => _playerCreature.OnEnemyKilled(enemyHealth);
                        health.TakeHit(data.Damage, true);
                    }
                    Debug.Log($"[ActionManager] Melee hit {hit.collider.gameObject.name} with {data.ActionName}, damage: {data.Damage}");
                }
            }
            else if (data.Type == ActionType.Spell)
            {
                if (data.ProjectilePrefab != null)
                {
                    Vector3 spawnPos = actionSpawnPoint.position;
                    Quaternion spawnRot = actionSpawnPoint.rotation;
                    var projectile = Runner.Spawn(data.ProjectilePrefab, spawnPos, spawnRot);
                    if (projectile != null)
                    {
                        var rb = projectile.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.velocity = actionSpawnPoint.forward * data.ProjectileSpeed;
                        }
                    }
                }
                else
                {
                    Ray ray = new Ray(actionSpawnPoint.position, actionSpawnPoint.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, data.Range, data.HitMask))
                    {
                        var health = hit.collider.GetComponentInParent<Health>();
                        if (health != null)
                        {
                            health.Killed = enemyHealth => _playerCreature.OnEnemyKilled(enemyHealth);
                            health.TakeHit(data.Damage, true);
                        }
                        Debug.Log($"[ActionManager] Spell hit {hit.collider.gameObject.name} with {data.ActionName}, damage: {data.Damage}");
                    }
                }
                if (data.ActionEffect != null)
                {
                    data.ActionEffect.Play();
                }
            }
            else if (data.Type == ActionType.Gun)
            {
                Vector3 hitPosition = Vector3.zero;
                Vector3 hitNormal = Vector3.zero;

                Ray ray = new Ray(actionSpawnPoint.position, actionSpawnPoint.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, data.Range, data.HitMask))
                {
                    var health = hit.collider.GetComponentInParent<Health>();
                    if (health != null)
                    {
                        health.Killed = enemyHealth => _playerCreature.OnEnemyKilled(enemyHealth);
                        health.TakeHit(data.Damage, true);
                    }
                    hitPosition = hit.point;
                    hitNormal = hit.normal;
                    Debug.Log($"[ActionManager] Gun hit {hit.collider.gameObject.name} with {data.ActionName}, damage: {data.Damage}");
                }

                if (gunMuzzleParticle != null)
                {
                    gunMuzzleParticle.Play();
                }
                else
                {
                    Debug.LogWarning("[ActionManager] Gun muzzle particle is not assigned for Gun action.");
                }
                if (hitPosition != Vector3.zero && data.ImpactPrefab != null)
                {
                    Instantiate(data.ImpactPrefab, hitPosition, Quaternion.LookRotation(hitNormal));
                }
            }

            if (data.ActionSound != null)
            {
                AudioSource.PlayClipAtPoint(data.ActionSound, actionSpawnPoint.position);
            }
        }

        private void UpdateGunModelVisibility()
        {
            bool isGunSelected = SelectedActionIndex >= 0 && SelectedActionIndex < availableActions.Count && availableActions[SelectedActionIndex].Type == ActionType.Gun;
            if (gunModel != null)
            {
                gunModel.SetActive(isGunSelected);
                Debug.Log($"[ActionManager] Gun model {(isGunSelected ? "enabled" : "disabled")} for action index {SelectedActionIndex}");
            }
            else if (isGunSelected)
            {
                Debug.LogError("[ActionManager] Gun model is not assigned but Gun action is selected.");
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
                Debug.Log($"[ActionManager] Player selected action: {availableActions[actionIndex].ActionName} (Type: {availableActions[actionIndex].Type}, Index: {actionIndex})");
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_NotifyActionExecution(string actionName, string animationTrigger)
        {
            Debug.Log($"[ActionManager] Player executed action: {actionName}");
            if (_playerCreature.Animator != null && !string.IsNullOrEmpty(animationTrigger))
            {
                _playerCreature.Animator.SetTrigger(animationTrigger);
            }
        }

        public int GetSelectedActionIndex => SelectedActionIndex;

        // Provide access to availableActions.Count
        public int GetAvailableActionsCount()
        {
            return availableActions.Count;
        }
    }
}
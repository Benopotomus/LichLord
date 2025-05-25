using UnityEngine;
using Fusion;
using System.Collections.Generic;
using Starter.Shooter;

namespace LichLord
{
    public class ActionManager : NetworkBehaviour
    {
        [Header("Action Setup")]
        [SerializeField] private List<ActionData> availableActions = new List<ActionData>();
        [SerializeField] private Transform actionSpawnPoint; // Where actions originate (e.g., camera for gun)
        [SerializeField] private GameObject gunModel; // Gun model to toggle visibility
        [SerializeField] private ParticleSystem gunMuzzleParticle; // ParticleSystem on gunModel for muzzle flash

        [Networked] private int SelectedActionIndex { get; set; } // Current selected action index (-1 for none)
        [Networked] private TickTimer CooldownTimer { get; set; }
        [Networked] private TickTimer AnimationTimer { get; set; }

        private PlayerCharacterInput _playerInput;
        private CharacterMovement _characterMovement;
        private PlayerCharacter _playerCharacter; // For OnEnemyKilled callback
        private NetworkObject _networkObject;

        public override void Spawned()
        {
            base.Spawned();
            _playerInput = GetComponent<PlayerCharacterInput>();
            _characterMovement = GetComponent<CharacterMovement>();
            _playerCharacter = GetComponent<PlayerCharacter>();
            _networkObject = GetComponent<NetworkObject>();

            if (!_playerInput)
                Debug.LogError("[ActionManager] Missing PlayerCharacterInput component.");
            if (!_characterMovement)
                Debug.LogError("[ActionManager] Missing CharacterMovement component.");
            if (!_playerCharacter)
                Debug.LogError("[ActionManager] Missing PlayerCharacter component.");
            if (!actionSpawnPoint)
                Debug.LogError("[ActionManager] Action spawn point not assigned.");
            if (availableActions.Count == 0)
                Debug.LogWarning("[ActionManager] No actions assigned to availableActions list.");
            if (!gunModel)
                Debug.LogError("[ActionManager] Gun model not assigned in Inspector.");
            if (!gunMuzzleParticle)
                Debug.LogError("[ActionManager] Gun muzzle particle not assigned in Inspector.");

            if (HasStateAuthority)
            {
                SelectedActionIndex = -1;
                CooldownTimer = TickTimer.None;
                AnimationTimer = TickTimer.None;
            }

            UpdateGunModelVisibility(); // Initialize gun model visibility
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _playerInput == null) return;

            ProcessActionSelection(_playerInput.CurrentInput);

            if (_playerInput.CurrentInput.Fire)
            {
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

            if (_characterMovement != null)
            {
                if (AnimationTimer.ExpiredOrNotRunning(Runner))
                {
                    _characterMovement.SetCastSpeedMultiplier(1f);
                }
                else
                {
                    _characterMovement.SetCastSpeedMultiplier(availableActions[SelectedActionIndex].MovementSpeedMultiplier);
                }
            }
        }

        private void ProcessActionSelection(GameplayInput input)
        {
            if (SelectedActionIndex - 1 == input.ActionSelection)
                return;

            if (input.ActionSelection == 0)
            {
                if (SelectedActionIndex != -1)
                {
                    SelectedActionIndex = -1;
                    RPC_LogActionSelection(-1);
                    Debug.Log("[ActionManager] Action selection cleared");
                    UpdateGunModelVisibility();
                }
            }
            else if (input.ActionSelection > 0 && input.ActionSelection <= availableActions.Count)
            {
                SelectedActionIndex = input.ActionSelection - 1;
                RPC_LogActionSelection(SelectedActionIndex);
                Debug.Log($"[ActionManager] Selected action: {availableActions[SelectedActionIndex].ActionName} (Index: {SelectedActionIndex})");
                UpdateGunModelVisibility();
            }
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
                        health.Killed = enemyHealth => _playerCharacter.OnEnemyKilled(enemyHealth);
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
                    var projectile = Runner.Spawn(data.ProjectilePrefab, spawnPos, spawnRot, Object.InputAuthority);
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
                            health.Killed = enemyHealth => _playerCharacter.OnEnemyKilled(enemyHealth);
                            health.TakeHit(data.Damage, true);
                        }
                        Debug.Log($"[ActionManager] Spell hit {hit.collider.gameObject.name} with {data.ActionName}, damage: {data.Damage}");
                    }
                }
                if (data.ActionEffect != null)
                {
                    data.ActionEffect.Play(); // Play spell-specific particle effect
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
                        health.Killed = enemyHealth => _playerCharacter.OnEnemyKilled(enemyHealth);
                        health.TakeHit(data.Damage, true);
                    }
                    hitPosition = hit.point;
                    hitNormal = hit.normal;
                    Debug.Log($"[ActionManager] Gun hit {hit.collider.gameObject.name} with {data.ActionName}, damage: {data.Damage}");
                }

                if (gunMuzzleParticle != null)
                {
                    gunMuzzleParticle.Play(); // Play muzzle flash on gun model
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
                AudioSource.PlayClipAtPoint(data.ActionSound, actionSpawnPoint.position); // Play gun sound
            }
        }

        private void UpdateGunModelVisibility()
        {
            bool isGunSelected = SelectedActionIndex >= 0 && SelectedActionIndex < availableActions.Count && availableActions[SelectedActionIndex].Type == ActionType.Gun;
            if (gunModel != null)
            {
                gunModel.SetActive(isGunSelected);
                //Debug.Log($"[ActionManager] Gun model {(isGunSelected ? "enabled" : "disabled")} for action index {SelectedActionIndex}");
            }
            else if (isGunSelected)
            {
                //Debug.LogError("[ActionManager] Gun model is not assigned but Gun action is selected.");
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_LogActionSelection(int actionIndex)
        {
            if (actionIndex == -1)
            {
                //Debug.Log("[ActionManager] Player cleared action selection");
            }
            else if (availableActions.Count > actionIndex)
            {
                //Debug.Log($"[ActionManager] Player selected action: {availableActions[actionIndex].ActionName} (Index: {actionIndex})");
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_NotifyActionExecution(string actionName, string animationTrigger)
        {
            Debug.Log($"[ActionManager] Player executed action: {actionName}");
            if (_characterMovement != null && _characterMovement.Animator != null && !string.IsNullOrEmpty(animationTrigger))
            {
                _characterMovement.Animator.SetTrigger(animationTrigger); // Triggers "Shoot" for gun
            }
        }

        public int GetSelectedActionIndex => SelectedActionIndex;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Addons.KCC;
using Fusion.Addons.SimpleKCC;
using UnityEngine.Rendering;
using Starter.Shooter;
using Starter;

namespace LichLord
{
    /// <summary>
    /// Main player script - controls player movement, actions, and animations.
    /// </summary>
    public sealed class PlayerCreature : NetworkBehaviour
    {
        [Header("References")]
        public Health Health;
        public CreatureMovement Movement;
        public PlayerCameraController CameraController;
        public PlayerCreatureInput Input;
        public ActionManager Actions;
        public Animator Animator;
        public Transform CameraPivot;
        public UINameplate Nameplate;
        public Collider Hurtbox;
        public Renderer[] HeadRenderers;
        public GameObject[] FirstPersonOverlayObjects;

        [Header("Animation Setup")]
        public Transform ChestTargetPosition;
        public Transform ChestBone;

        [Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
        public string Nickname { get; set; }
        [Networked, HideInInspector]
        public int ChickenKills { get; set; }

        private GameManager _gameManager;

        /// <summary>
        /// A static dictionary of the local player, using the NetworkRunner as the key to account for multi-peer mode.
        /// </summary>
        public static Dictionary<NetworkRunner, Player> LocalPlayerDictionary { get; set; }

        /// <summary>
        /// A static dictionary that contains lists of all the players in the game, using NetworkRunners as the keys to account for multi-peer mode
        /// </summary>
        public static Dictionary<NetworkRunner, List<Player>> PlayerListDictionary { get; set; }
        public static bool TryGetLocalPlayer(NetworkRunner runner, out Player player)
        {
            if (runner == null)
            {
                player = null;
                return false;
            }

            if (LocalPlayerDictionary.TryGetValue(runner, out player))
            {
                return player != null;
            }
            return false;
        }

        public override void Spawned()
        {
            Runner.SetPlayerAlwaysInterested(Runner.LocalPlayer, Object, true);

            if (HasStateAuthority)
            {
                _gameManager = FindObjectOfType<GameManager>();

                // Set player nickname that is saved in UIGameMenu
                Nickname = PlayerPrefs.GetString("PlayerName");
            }

            // In case the nickname is already changed, trigger the change manually
            OnNicknameChanged();

            if (HasStateAuthority)
            {
                // For input authority, deactivate head renderers so they don’t obstruct the view
                for (int i = 0; i < HeadRenderers.Length; i++)
                {
                    HeadRenderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }

                // Some objects (e.g., weapon) are rendered with a secondary Overlay camera
                int overlayLayer = LayerMask.NameToLayer("FirstPersonOverlay");
                for (int i = 0; i < FirstPersonOverlayObjects.Length; i++)
                {
                    FirstPersonOverlayObjects[i].layer = overlayLayer;
                }

                // Look rotation interpolation is skipped for local player
            }

            // Ensure ActionManager is assigned
            if (Actions == null)
            {
                Actions = GetComponent<ActionManager>();
                if (Actions == null)
                    Debug.LogError("[PlayerCharacter] Missing ActionManager component.");
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (Health.IsFinished)
            {
                // Player is dead and death timer is finished, respawn the player
                Respawn(_gameManager.GetSpawnPosition());
            }

            Movement.KCC.SetActive(Health.IsAlive);
        }

        public override void Render()
        {
            // Disable hits when player is dead
            Hurtbox.enabled = Health.IsAlive;
        }

        private void LateUpdate()
        {
            if (Health.IsAlive == false)
                return;

            // IK after animations
            var pitchRotation = Movement.KCC.GetLookRotation(true, false);
            CameraPivot.localRotation = Quaternion.Euler(pitchRotation);

            // Dummy IK solution: snap chest bone to ChestTargetPosition
            float blendAmount = HasStateAuthority ? 0.05f : 0.2f;
            ChestBone.position = Vector3.Lerp(ChestTargetPosition.position, ChestBone.position, blendAmount);
            ChestBone.rotation = Quaternion.Lerp(ChestTargetPosition.rotation, ChestBone.rotation, blendAmount);
        }

        private void Respawn(Vector3 position)
        {
            ChickenKills = 0;
            Health.Revive();

            Movement.KCC.SetPosition(position);
            Movement.KCC.SetLookRotation(0f, 0f);
        }

        public void OnEnemyKilled(Health enemyHealth)
        {
            // Killing chicken grants 1 point, killing other player has -10 points penalty
            ChickenKills += enemyHealth.GetComponent<Chicken>() != null ? 1 : -10;
        }

        private void OnNicknameChanged()
        {
            if (HasStateAuthority)
                return; // Do not show nickname for local player

            Nameplate.SetNickname(Nickname);
        }
    }
}
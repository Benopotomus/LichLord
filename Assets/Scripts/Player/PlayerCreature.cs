using UnityEngine;
using Fusion;
using UnityEngine.Rendering;
using Starter.Shooter;
using Starter;
using LichLord.Projectiles;
using SoulGames.EasyGridBuilderPro;

namespace LichLord
{
    /// <summary>
    /// Main player script - controls player movement, actions, and animations.
    /// </summary>
    public class PlayerCreature : RelayPlayer , INetActor, IHitInstigator, IHitTarget
    {
        [Header("References")]
        public Health Health;
        public CreatureMovement Movement;
        public PlayerCameraController CameraController;
        public PlayerCreatureInput Input;
        public CreatureManeuvers Actions;
        public PlayerProjectilePool ProjectilePool;
        public HurtboxComponent Hurtbox;
        public Animator Animator;
        public Transform CameraPivot;
        public UINameplate Nameplate;
        public Renderer[] HeadRenderers;
        public GameObject[] FirstPersonOverlayObjects;

        [Header("Animation Setup")]
        public Transform ChestTargetPosition;
        public Transform ChestBone;

        [Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
        public string Nickname { get; set; }

        private GameManager _gameManager;

        public FNetObjectID NetObjectID
        {
            get => Object != null ? new FNetObjectID { networkId = Object.Id } : default;
        }

        INetActor IHitInstigator.NetActor => this;

        [SerializeField]
        private BuildableGridObjectTypeSO item;

        public static bool TryGetLocalPlayer(NetworkRunner runner, out PlayerCreature playerCreature)
        {
            playerCreature = null;

            if (runner == null)
                return false;

            runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject playerObject);

            if(playerObject == null)
                return false;

            playerCreature = playerObject.GetComponent<PlayerCreature>();
            if (playerCreature == null)
                return false;

            return true;
        }

        public override void Spawned()
        {
            base.Spawned();

            Context.LocalPlayerCreature = this;
            Context.LocalPlayerRef = Object.StateAuthority;

            Runner.SetPlayerObject(Runner.LocalPlayer, Object);
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
            }

            // Ensure ActionManager is assigned
            if (Actions == null)
            {
                Actions = GetComponent<CreatureManeuvers>();
                if (Actions == null)
                    Debug.LogError("[PlayerCharacter] Missing ActionManager component.");
            }

            //EasyGridBuilderPro.Instance.SetSelectedBuildable(item);
        }

        public void ApplyDamage(int guid, Vector3 impulse, int damage)
        {
            //Context.PropManager.RaiseEvent(new DamageEvent { impulse = Vector3.zero, damage = 9001 });

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

            Health.Revive();

            Movement.KCC.SetPosition(position);
            Movement.KCC.SetLookRotation(0f, 0f);
        }

        public void OnEnemyKilled(Health enemyHealth)
        {
        }

        private void OnNicknameChanged()
        {
            if (HasStateAuthority)
                return; // Do not show nickname for local player

            Nameplate.SetNickname(Nickname);
        }

        void IHitInstigator.HitPerformed(ref FHitUtilityData hit)
        {
            //throw new System.NotImplementedException();
        }

        void IHitTarget.ProcessHit(ref FHitUtilityData hit)
        {
            //throw new System.NotImplementedException();
        }

        void IHitTarget.OnHitTaken(ref FHitUtilityData hit)
        {
            //throw new System.NotImplementedException();
        }
    }
}
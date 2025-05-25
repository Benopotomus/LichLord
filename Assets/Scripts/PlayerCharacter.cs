using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Addons.KCC;
using Starter.Shooter;
using Starter;
using UnityEngine.Rendering;
using Fusion.Addons.SimpleKCC;

namespace LichLord
{
        /// <summary>
        /// Main player scrip - controls player movement and animations.
        /// </summary>
     
    public sealed class PlayerCharacter : NetworkBehaviour
    {
        [Header("References")]
        public Health Health;
        public CharacterMovement CharacterMovement;
        public PlayerCameraController PlayerCameraController;
        public PlayerCharacterInput PlayerInput;
        public Animator Animator;
        public Transform CameraPivot;
        public Transform CameraHandle;
        public Transform ScalingRoot;
        public UINameplate Nameplate;
        public Collider Hitbox;
        public Renderer[] HeadRenderers;
        public GameObject[] FirstPersonOverlayObjects;

        [Header("Fire Setup")]
        public LayerMask HitMask;
        public GameObject ImpactPrefab;
        public ParticleSystem MuzzleParticle;

        [Header("Animation Setup")]
        public Transform ChestTargetPosition;
        public Transform ChestBone;

        private int _animIDShoot = Animator.StringToHash("Shoot");

        [Header("Sounds")]
        public AudioSource FireSound;

        [Networked, HideInInspector, Capacity(24), OnChangedRender(nameof(OnNicknameChanged))]
        public string Nickname { get; set; }
        [Networked, HideInInspector]
        public int ChickenKills { get; set; }


        [Networked]
        private Vector3 _hitPosition { get; set; }
        [Networked]
        private Vector3 _hitNormal { get; set; }
        [Networked]
        private int _fireCount { get; set; }

        private int _visibleFireCount;

        private GameManager _gameManager;

        public override void Spawned()
        {
            if (HasStateAuthority)
            {
                _gameManager = FindObjectOfType<GameManager>();

                // Set player nickname that is saved in UIGameMenu
                Nickname = PlayerPrefs.GetString("PlayerName");
            }

            // In case the nickname is already changed,
            // we need to trigger the change manually
            OnNicknameChanged();

            // Reset visible fire count
            _visibleFireCount = _fireCount;

            if (HasStateAuthority)
            {
                // For input authority deactivate head renderers so they are not obstructing the view
                for (int i = 0; i < HeadRenderers.Length; i++)
                {
                    HeadRenderers[i].shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }

                // Some objects (e.g. weapon) are renderer with secondary Overlay camera.
                // This prevents weapon clipping into the wall when close to the wall.
                int overlayLayer = LayerMask.NameToLayer("FirstPersonOverlay");
                for (int i = 0; i < FirstPersonOverlayObjects.Length; i++)
                {
                    FirstPersonOverlayObjects[i].layer = overlayLayer;
                }

                // Look rotation interpolation is skipped for local player.
                // Look rotation is set manually in Render.
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (Health.IsFinished)
            {
                // Player is dead and death timer is finished, let's respawn the player
                Respawn(_gameManager.GetSpawnPosition());
            }

            var input = Health.IsAlive ? PlayerInput.CurrentInput : default;
            ProcessInput(input);


            CharacterMovement.ProcessMovementInput(input);

            CharacterMovement.KCC.SetActive(Health.IsAlive);

            PlayerInput.ResetInput();
        }

        public override void Render()
        {
            ShowFireEffects();

            // Disable hits when player is dead
            Hitbox.enabled = Health.IsAlive;
        }

        private void LateUpdate()
        {
            if (Health.IsAlive == false)
                return;

            // IK after animations

            // Update camera pivot (influences ChestIK)
            // (KCC look rotation is set earlier in Render)
            var pitchRotation = CharacterMovement.KCC.GetLookRotation(true, false);
            CameraPivot.localRotation = Quaternion.Euler(pitchRotation);

            // Dummy IK solution, we are snapping chest bone to prepared ChestTargetPosition position
            // Lerping blends the fixed position with little bit of animation position.
            float blendAmount = HasStateAuthority ? 0.05f : 0.2f;
            ChestBone.position = Vector3.Lerp(ChestTargetPosition.position, ChestBone.position, blendAmount);
            ChestBone.rotation = Quaternion.Lerp(ChestTargetPosition.rotation, ChestBone.rotation, blendAmount);
            
        }

        private void ProcessInput(GameplayInput input)
        {
            if (input.Fire)
            {
                Fire();
            }
        }

        private void Fire()
        {
            // Clear hit position in case nothing will be hit
            _hitPosition = Vector3.zero;

            // Whole projectile path and effects are immediately processed (= hitscan projectile)
            if (Physics.Raycast(CameraHandle.position, CameraHandle.forward, out var hitInfo, 200f, HitMask))
            {
                // Deal damage
                var health = hitInfo.collider != null ? hitInfo.collider.GetComponentInParent<Health>() : null;
                if (health != null)
                {
                    health.Killed = OnEnemyKilled;
                    health.TakeHit(1, true);
                }

                // Save hit point to correctly show bullet path on all clients.
                // This however works only for single projectile per FUN and with higher fire cadence
                // some projectiles might not be fired on proxies because we save only the position
                // of the LAST hit.
                _hitPosition = hitInfo.point;
                _hitNormal = hitInfo.normal;
            }

            // In this example projectile count property (fire count) is used not only for weapon fire effects
            // but to spawn the projectile visuals themselves.
            _fireCount++;
        }

        private void Respawn(Vector3 position)
        {
            ChickenKills = 0;
            Health.Revive();

            CharacterMovement.KCC.SetPosition(position);
            CharacterMovement.KCC.SetLookRotation(0f, 0f);
        }

        private void OnEnemyKilled(Health enemyHealth)
        {
            // Killing chicken grants 1 point, killing other player has -10 points penalty.
            ChickenKills += enemyHealth.GetComponent<Chicken>() != null ? 1 : -10;
        }

        private void ShowFireEffects()
        {
            // Notice we are not using OnChangedRender for fireCount property but instead
            // we are checking against a local variable and show fire effects only when visible
            // fire count is SMALLER. This prevents triggering false fire effects when
            // local player mispredicted fire (e.g. input got lost) and fireCount property got decreased.
            if (_visibleFireCount < _fireCount)
            {
                FireSound.PlayOneShot(FireSound.clip);
                MuzzleParticle.Play();
                Animator.SetTrigger(_animIDShoot);

                if (_hitPosition != Vector3.zero)
                {
                    // Impact gets destroyed automatically with DestroyAfter script
                    Instantiate(ImpactPrefab, _hitPosition, Quaternion.LookRotation(_hitNormal));
                }
            }

            _visibleFireCount = _fireCount;
        }




        private void OnNicknameChanged()
        {
            if (HasStateAuthority)
                return; // Do not show nickname for local player

            Nameplate.SetNickname(Nickname);
        }
    }
}

using UnityEngine;
using Fusion;
using UnityEngine.Rendering;
using Starter.Shooter;
using Starter;
using LichLord.Projectiles;
using SoulGames.EasyGridBuilderPro;
using LichLord.NonPlayerCharacters;
using LichLord.World;

namespace LichLord
{
    /// <summary>
    /// Main player script - controls player movement, actions, and animations.
    /// </summary>
    public class PlayerCharacter : RelayPlayer, INetActor, IHitInstigator, IHitTarget, IChunkTrackable
    {
        [Header("References")]
        public Health Health;
        public PlayerCharacterMovementComponent Movement;
        public PlayerCameraController CameraController;
        public PlayerCharacterInput Input;
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

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        public FNetObjectID NetObjectID
        {
            get => Object != null ? new FNetObjectID { networkId = Object.Id } : default;
        }

        INetActor IHitInstigator.NetActor => this;

        private Chunk _chunk;
        public Chunk CurrentChunk { get { return _chunk; } set { _chunk = value; } }

        [SerializeField]
        private BuildableGridObjectTypeSO item;

        public static bool TryGetLocalPlayer(NetworkRunner runner, out PlayerCharacter playerCreature)
        {
            playerCreature = null;

            if (runner == null)
                return false;

            runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject playerObject);

            if(playerObject == null)
                return false;

            playerCreature = playerObject.GetComponent<PlayerCharacter>();
            if (playerCreature == null)
                return false;

            return true;
        }

        public override void Spawned()
        {
            base.Spawned();

            Context.LocalPlayerCreature = this;
            Context.LocalPlayerRef = Object.StateAuthority;
            Context.NetworkGame.OnPlayerSpawned(this);

            Runner.SetPlayerObject(Runner.LocalPlayer, Object);
            Runner.SetPlayerAlwaysInterested(Runner.LocalPlayer, Object, true);

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

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            Context.NetworkGame.OnPlayerDespawned(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (Health.IsFinished)
            {
            }
        }

        public override void Render()
        {
            base.Render();
            // Disable hits when player is dead
            Hurtbox.enabled = Health.IsAlive;
            UpdateChunk(Context.ChunkManager);
        }

        private void LateUpdate()
        {
            if (Health.IsAlive == false)
                return;

            // IK after animations
            var pitchRotation = Movement.WorldTransform.Pitch;
            CameraPivot.localRotation = Quaternion.Euler(pitchRotation, 0 , 0);

            // Dummy IK solution: snap chest bone to ChestTargetPosition
            float blendAmount = HasStateAuthority ? 0.05f : 0.2f;
            ChestBone.position = Vector3.Lerp(ChestTargetPosition.position, ChestBone.position, blendAmount);
            ChestBone.rotation = Quaternion.Lerp(ChestTargetPosition.rotation, ChestBone.rotation, blendAmount);
        }

        private void Respawn(Vector3 position)
        {
            Health.Revive();
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
            if (hit.target is NonPlayerCharacter npc)
            {
                npc.Replicator.RPC_DealDamageToNPC(npc.NetObjectID.index, hit.damageData.damageValue);

                if (!Runner.IsSharedModeMasterClient)
                    npc.Replicator.Predict_DealDamageToNPC(npc.NetObjectID.index, hit.damageData.damageValue);
               
            }
        }

        void IHitTarget.ProcessHit(ref FHitUtilityData hit)
        {
            //throw new System.NotImplementedException();
        }

        void IHitTarget.OnHitTaken(ref FHitUtilityData hit)
        {
            //throw new System.NotImplementedException();
        }

        public void UpdateChunk(ChunkManager chunkManager)
        {
            var lastChunk = CurrentChunk;
            var newChunk = chunkManager.GetChunkAtPosition(CachedTransform.position);

            CurrentChunk = newChunk;

            if (lastChunk != newChunk)
            {
                if(lastChunk != null)
                    lastChunk.RemoveObject(gameObject);

                newChunk.AddObject(gameObject);
            }

            if (HasStateAuthority)
            {
                if (lastChunk != newChunk)
                { 
                    //Chunk changed, toggle on/off items
                }
            }   
        }
    }
}
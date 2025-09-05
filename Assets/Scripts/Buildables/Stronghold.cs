using Fusion;
using LichLord.Buildables;
using LichLord.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LichLord
{
    public class Stronghold : ContextBehaviour, IChunkTrackable, IHitTarget
    {
        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        [SerializeField] private DecalProjector _decalProjector;

        [SerializeField] private BuildableZone _buildableZone;
        public BuildableZone BuildableZone => _buildableZone;

        [SerializeField] private TerrainFlattener _terrainFlattener;

        [SerializeField] private InteractableComponent _interactableComponent;

        [Networked]
        private ref FStrongholdData _data => ref MakeRef<FStrongholdData>();
        public FStrongholdData Data => _data;

        [Networked]
        private int _currentHealth { get; set; }
        public int CurrentHealth => _currentHealth;

        [Networked]
        private int _rank { get; set; }
        public int Rank => _rank;

        public int _maxHealth;
        public int MaxHealth => _maxHealth;

        private float _buildDistance { get; set; }

        private float _influenceDistance = 20.0f;
        private float _localInfluenceDistance = -1;

        // IChunkTrackable
        public Chunk CurrentChunk { get { return _chunk; } set { } }
        private Chunk _chunk;
        public Vector3 Position => _cachedTransform.position;
        public float BonusRadius { get { return 4f; } }
        public virtual Collider HurtBoxCollider { get { return Hurtbox.HurtBoxes[0]; } }
        public bool IsAttackable {
            get
            { 
                if (_currentHealth > 0)
                    return true;

                return false;
            }
        }

        public HurtboxComponent Hurtbox;

        public override void Spawned()
        {
            base.Spawned();

            if (Context.ChunkManager.ChunksReady)
            {
                OnChunksReady();
            }
            else
            {
                Context.ChunkManager.onChunksReady += OnChunksReady;
            }

            _interactableComponent.Activate(
                this,
                IsPotentialInteractor,
                IsInteractionValid,
                GetInteractionText,
                GetInteractionTime
            );

            _interactableComponent.onInteractStart += OnInteractStart;
            _interactableComponent.onInteractEnd += OnInteractEnd;
            _interactableComponent.onInteractionComplete += OnInteractionComplete;

            Context.StrongholdManager.OnStrongholdSpawned(this);
        }

        private void OnChunksReady()
        {
            Context.ChunkManager.onChunksReady -= OnChunksReady;
            _cachedTransform.position = Context.StrongholdManager.GetStrongholdPosition(_data);
            _chunk = Context.ChunkManager.GetChunk(_data.ChunkID);
            _chunk.AddObject(this);
            var newChunks = Context.ChunkManager.GetNearbyChunks(CurrentChunk.ChunkID, radius: 1);
            Context.ChunkManager.TryAddReplicatedChunks(newChunks);

        }

        public void SetSpawnData(FStrongholdData data, int currentHealth, int rank, int buildableZoneId)
        { 
            _data = data;
            _currentHealth = currentHealth;
            _rank = rank;
            _maxHealth = 1000 + ((rank - 1) * 100);
            _influenceDistance = 20 + ((rank - 1) * 5);
            _buildableZone.ZoneID = (byte)buildableZoneId;
        }

        public override void Render()
        {
            base.Render();

            _maxHealth = 1000 + ((_rank - 1) * 100);
            _influenceDistance = 20 + ((_rank - 1) * 5);

            if (_localInfluenceDistance != _influenceDistance)
            {
                _decalProjector.size = new Vector3(_influenceDistance * 2.95f, _influenceDistance * 2.95f, _influenceDistance * 2.95f);
                _buildableZone.SetTriggerSize(_influenceDistance);
                _terrainFlattener.TryFlatten(_influenceDistance, 10f);
                _localInfluenceDistance = _influenceDistance;
            }
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {

        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {

        }

        [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamage(int damage)
        {
            _currentHealth = Mathf.Max(0, _currentHealth - damage);

            if (_currentHealth == 0)
            {
            }

        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            var newChunks = Context.ChunkManager.GetNearbyChunks(CurrentChunk.ChunkID, radius: 1);
            Context.ChunkManager.TryAddReplicatedChunks(newChunks);

            _interactableComponent.onInteractStart -= OnInteractStart;
            _interactableComponent.onInteractEnd -= OnInteractEnd;
            _interactableComponent.onInteractionComplete -= OnInteractionComplete;
            _chunk.RemoveObject(this);
            Context.StrongholdManager.OnStrongholdDespawned(this);

            base.Despawned(runner, hasState);
        }

        // Interactable

        private bool IsPotentialInteractor(InteractorComponent interactor)
        {
            return interactor != null;
        }

        private bool IsInteractionValid(InteractorComponent interactor)
        {
            return true;
        }

        private string GetInteractionText(InteractorComponent interactor)
        {
            return "Stockpile";
        }

        private float GetInteractionTime(InteractorComponent interactor)
        {
            return 3.0f;
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction started with Stronghold.");
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with Stronghold.");
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Stronghold Interaction complete.");
            // Trigger effects, state changes, or events
            Context.InvasionManager.BeginInvasion(2, Data);

            //_rank++;
        }
    }
}

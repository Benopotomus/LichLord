using Fusion;
using LichLord.Buildables;
using LichLord.Items;
using LichLord.NonPlayerCharacters;
using LichLord.World;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LichLord
{
    public class Stronghold : ContextBehaviour, IChunkTrackable, IHitTarget
    {
        [Networked]
        public byte StrongholdID { get; set; }

        [Networked]
        public ushort ContainerIndex { get; set; }

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        [SerializeField] private DecalProjector _decalProjector;

        [SerializeField] private BuildableZone _buildableZone;
        public BuildableZone BuildableZone => _buildableZone;

        [SerializeField] private TerrainFlattener _terrainFlattener;

        [SerializeField] private InteractableComponent _interactableComponent;

        [SerializeField] 
        private WorkerComponent _workerComponent;
        public WorkerComponent WorkerComponent => _workerComponent;

        [Networked]
        private ref FStaticPropPosition _data => ref MakeRef<FStaticPropPosition>();
        public FStaticPropPosition Data => _data;

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
        public float InfluenceDistance => _influenceDistance;

        private float _localInfluenceDistance = -1;

        [SerializeField]
        private FContainerSlotData _containerSlotData = new FContainerSlotData();
        public FContainerSlotData ContainerSlotData => _containerSlotData;

        // IChunkTrackable
        public Chunk CurrentChunk { get { return _chunk; } set { } }
        private Chunk _chunk;
        public Vector3 Position => _cachedTransform.position;

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
                GetTicksToComplete,
                GetInteractType,
                GetInteractDistance
            );

            _interactableComponent.onInteractStart += OnInteractStart;
            _interactableComponent.onInteractEnd += OnInteractEnd;
            _interactableComponent.onInteractionComplete += OnInteractionComplete;

            _cachedTransform.position = _data.GetPosition(Context, HasStateAuthority);
            _containerSlotData = Context.ContainerManager.GetContainerDataAtIndex(ContainerIndex);
            Context.StrongholdManager.OnStrongholdSpawned(this);
        }

        private void OnChunksReady()
        {
            Context.ChunkManager.onChunksReady -= OnChunksReady;
            _cachedTransform.position = _data.GetPosition(Context, HasStateAuthority);
            _chunk = Context.ChunkManager.GetChunk(_data.ChunkID);
            _chunk.AddObject(this);
            var newChunks = Context.ChunkManager.GetNearbyChunks(CurrentChunk.ChunkID, radius: 1);
            Context.ChunkManager.TryAddReplicatedChunks(newChunks);
        }

        public void SetSpawnData(int strongholdId, FStaticPropPosition positionData, int currentHealth, int rank,int containerIndex)
        { 
            _data = positionData;
            _currentHealth = currentHealth;
            _rank = rank;
            _maxHealth = 1000 + ((rank - 1) * 100);
            _influenceDistance = 20 + ((rank - 1) * 5);
            StrongholdID = (byte)strongholdId;
            ContainerIndex = (ushort)containerIndex;
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
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            return interactor != null;
        }

        private bool IsInteractionValid(InteractorComponent interactor)
        {
            float interactDistance = GetInteractDistance(interactor) * GetInteractDistance(interactor);
            float sqrDist = (transform.position - interactor.transform.position).sqrMagnitude;

            if (sqrDist > interactDistance)
                return false;

            return true;
        }

        private string GetInteractionText(InteractorComponent interactor)
        {
            return "" + this;
        }

        private int GetTicksToComplete(InteractorComponent interactor)
        {
            return -1;
        }

        private EInteractType GetInteractType(InteractorComponent interactor)
        {
            return EInteractType.Stronghold;
        }

        private float GetInteractDistance(InteractorComponent interactor)
        {
            return 5;
        }

        private void OnInteractStart(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction started with " + this);
            //Context.InvasionManager.BeginInvasion(2, StrongholdID);
        }

        private void OnInteractEnd(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction ended with " + this);
        }

        private void OnInteractionComplete(InteractableComponent interactable, InteractorComponent interactor)
        {
            Debug.Log("Interaction complete with " + this);
            // Trigger effects, state changes, or events


            //_rank++;
        }
    }
}

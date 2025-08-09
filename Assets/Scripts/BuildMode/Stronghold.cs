using Fusion;
using LichLord.World;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LichLord
{
    public class Stronghold : ContextBehaviour, IChunkTrackable, IHitTarget
    {
        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        [SerializeField] private DecalProjector _decalProjector;

        [Networked]
        private ref FStrongholdData _data => ref MakeRef<FStrongholdData>();
        public FStrongholdData Data => _data;

        // Stronghold heart health
        [Networked]
        private int _currentHealth { get; set; }
        public int CurrentHealth => _currentHealth;

        public int _maxHealth;
        public int MaxHealth => _maxHealth;

        private float _buildDistance { get; set; }

        [Networked]
        private float _influenceDistance { get; set; } = 20.0f;

        public Chunk CurrentChunk { get { return _chunk; } set { } }
        private Chunk _chunk;

        public Vector3 Position => _cachedTransform.position;

        public float BonusRadius { get { return 4f; } }

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

            CachedTransform.position = Context.StrongholdManager.GetStrongholdPosition(_data);
            _chunk = Context.ChunkManager.GetChunk(_data.ChunkID);
            _chunk.AddObject(this);

            Context.InvasionManager.BeginInvasion(1, _data);
            Context.StrongholdManager.OnStrongholdSpawned(this);
        }

        public void SetData(FStrongholdData data, int currentHealth, int maxHealth, float influenceDistance)
        { 
            _data = data;
            _currentHealth = currentHealth;
            _maxHealth = maxHealth;
        }

        public override void Render()
        {
            base.Render();
            _decalProjector.size = new Vector3(_influenceDistance, _influenceDistance);
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {

        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {

        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable, InvokeLocal = true)]
        public void RPC_DealDamage(int damage)
        {
            _currentHealth = Mathf.Max(0, _currentHealth - damage);

            if (_currentHealth == 0)
            {
            }

        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _chunk.RemoveObject(this);
            Context.StrongholdManager.OnStrongholdDespawned(this);
        }
    }
}

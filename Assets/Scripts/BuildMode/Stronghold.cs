using Fusion;
using LichLord.World;
using UnityEngine;


namespace LichLord.Buildables
{
    public class Stronghold : ContextBehaviour, IChunkTrackable
    {
        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        [Networked]
        private ref FStrongholdData _data => ref MakeRef<FStrongholdData>();

        // Stronghold heart health
        [Networked]
        private int _health { get; set; }
        [Networked]
        private float _buildDistance { get; set; }
        [Networked]
        private float _influenceDistance { get; set; }

        public Chunk CurrentChunk { get { return Context.ChunkManager.GetChunk(_data.ChunkID); } set { } }

        public Vector3 Position => _cachedTransform.position;

        public bool IsAttackable => true;

        public HurtboxComponent Hurtbox;

        public override void Spawned()
        {
            base.Spawned();
        }

        public void SetData(FStrongholdData data)
        { 
            _data = data;
        }
    }
}


using LichLord.World;
using UnityEngine;

namespace LichLord.Props
{
    public class NetworkProp : ContextBehaviour, IHitTarget, IChunkTrackable
    {
        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        [SerializeField]
        protected PropRuntimeState _propRuntimeState;
        public PropRuntimeState RuntimeState => _propRuntimeState;

        [SerializeField]
        protected PropDefinition _propDefinition;

        // IChunkTrackable
        private Chunk _currentChunk;
        public Chunk CurrentChunk { get => _currentChunk; set => _currentChunk = value; }

        public Vector3 Position => CachedTransform.position;
        public bool IsAttackable
        {
            get { return true; }
        }

        public virtual void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
            _propRuntimeState = propRuntimeState;
            _propDefinition = propRuntimeState.Definition;

            CachedTransform.position = _propRuntimeState.position;
            CachedTransform.rotation = _propRuntimeState.rotation;

            _currentChunk = propRuntimeState.chunk;
        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {

        }

        public void ProcessHit(ref FHitUtilityData hit)
        {

        }
    }
}

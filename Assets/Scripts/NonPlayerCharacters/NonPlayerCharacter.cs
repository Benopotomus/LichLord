using DWD.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace LichLord.NonPlayerCharacters
{
    // This is just the visual representation of an NPC
    // It updates while active. 
    // Because its always networked and updating, it should be processed
    // on the master client no matter what.
    
    public class NonPlayerCharacter : DWDObjectPoolObject, IHitTarget, IHitInstigator, INetActor
    {
        protected NonPlayerCharacterManager _nonPlayerCharacterManager;
        protected NonPlayerCharacterRuntimeState _nonPlayerCharacterRuntimeState;

        [SerializeField] private Transform _transform;
        public Transform CachedTransform => _transform;

        [SerializeField] private NavMeshAgent _agent;
        public NavMeshAgent Agent => _agent;

        public HurtboxComponent Hurtbox;

        public INetActor NetActor => this;
        public FNetObjectID NetObjectID => new FNetObjectID();

        public void OnRender()
        { 
        
        }

        public void OnFixedUpdate()
        { 
        
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {

        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {

        }

        public void HitPerformed(ref FHitUtilityData hit)
        {

        }
    }
}

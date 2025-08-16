using LichLord.Props;
using UnityEngine;

namespace LichLord.Buildables
{ 
    public class BuildableWall : Buildable
    {
        // IChunkTrackable
        public override float BonusRadius { get { return 0; } }
        public override bool IsAttackable
        {
            get 
            {
                if(_healthComponent.CurrentHealth == 0)
                    return false;

                return true; 
            } 
        }
        public override Collider HurtBoxCollider { get { return Hurtbox.HurtBoxes[0]; } }

        [SerializeField] protected BuildableHealthComponent _healthComponent;
        [SerializeField] protected BuildableStateComponent _stateComponent;

        public override void OnSpawned(BuildableZone zone, BuildableRuntimeState runtimeState)
        {
            base.OnSpawned(zone, runtimeState);

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());
        }

        public override void OnRender(BuildableRuntimeState runtimeState, float renderDeltaTime, bool hasAuthority)
        {
            base.OnRender(runtimeState, renderDeltaTime, hasAuthority);

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
            _stateComponent.UpdateState(RuntimeState.GetState());
        }

        public override void StartRecycle()
        {
            base.StartRecycle();
            _stateComponent.UpdateState(EBuildableState.Inactive);
        }

        public override void OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public override void ProcessHit(ref FHitUtilityData hit)
        {
        }
    }
}
using LichLord.World;
using UnityEngine;

namespace LichLord.Props
{
    public class DestructibleProp : Prop, IHitTarget
    {
        [SerializeField]
        protected PropStateComponent _stateComponent;
        public PropStateComponent StateComponent => _stateComponent;

        [SerializeField]
        protected PropHealthComponent _healthComponent;
        public PropHealthComponent HealthComponent => _healthComponent;

        // IHitTarget
        public IChunkTrackable ChunkTrackable => this;

        public HurtboxComponent Hurtbox;

        public override void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
           base.OnSpawned(propRuntimeState, propManager);

            _stateComponent.UpdateState(_runtimeState.GetState());
            _healthComponent.UpdateHealth(_runtimeState.GetHealth());
        }

        // This is the visuals for authority and client.
        // Read only - no logic should update here.
        public override void OnRender(PropRuntimeState propRuntimeState, float renderDeltaTime)
        {
            base.OnRender(propRuntimeState, renderDeltaTime);

            _stateComponent.UpdateState(propRuntimeState.GetState());
            _healthComponent.UpdateHealth(propRuntimeState.GetHealth());
        }

        public void OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public void ProcessHit(ref FHitUtilityData hit)
        {
        }
    }
}
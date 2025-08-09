using UnityEngine;
using DWD.Pooling;
using LichLord.World;

namespace LichLord.Props
{
    public class Destructible : Prop, IHitTarget
    {
        [SerializeField]
        protected PropStateComponent _stateComponent;
        public PropStateComponent StateComponent => _stateComponent;

        [SerializeField]
        protected PropHealthComponent _healthComponent;
        public PropHealthComponent HealthComponent => _healthComponent;

        public HurtboxComponent Hurtbox;

        public float BonusRadius { get { return 0; } }

        public override void OnSpawned(PropRuntimeState propRuntimeState, PropManager propManager)
        {
           base.OnSpawned(propRuntimeState, propManager);

            _stateComponent.UpdateState(_propRuntimeState.GetState());
            _healthComponent.UpdateHealth(_propRuntimeState.GetHealth());
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
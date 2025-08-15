using LichLord.Props;
using UnityEngine;

namespace LichLord.Buildables
{
    public class BuildableWall : Buildable
    {
        public override float BonusRadius { get { return 1; } }
        public override bool IsAttackable => false;

        [SerializeField]
        protected BuildableHealthComponent _healthComponent;
        public BuildableHealthComponent HealthComponent => _healthComponent;

        public override void OnSpawned(BuildableZone zone, BuildableRuntimeState runtimeState)
        {
            base.OnSpawned(zone, runtimeState);

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
        }

        public override void OnRender(BuildableRuntimeState runtimeState, float renderDeltaTime, bool hasAuthority)
        {
            base.OnRender(runtimeState, renderDeltaTime, hasAuthority);

            _healthComponent.UpdateHealth(RuntimeState.GetHealth());
        }

        public override void StartRecycle()
        {
            base.StartRecycle();
        }

        public override void OnHitTaken(ref FHitUtilityData hit)
        {
        }

        public override void ProcessHit(ref FHitUtilityData hit)
        {
        }
    }
}
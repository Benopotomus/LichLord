using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterLifetimeComponent : MonoBehaviour
    {
        [SerializeField]
        private int _nextLifetimeProgressTick;

        [SerializeField]
        private int _lifetimeProgress;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            if (!runtimeState.IsWarrior())
                return;

            _lifetimeProgress = runtimeState.GetLifetimeProgress();
            _nextLifetimeProgressTick = tick + runtimeState.GetTicksPerLifetime();
        }

        public void UpdateLifetime(NonPlayerCharacterRuntimeState runtimeState,
            bool hasAuthority, 
            int tick)
        {
            if (!runtimeState.IsWarrior())
                return;

            if (tick > _nextLifetimeProgressTick)
            {
                _lifetimeProgress = runtimeState.GetLifetimeProgress();
                int newlifetime = _lifetimeProgress + 1;

                runtimeState.SetLifetimeProgress(newlifetime);
                _nextLifetimeProgressTick = tick + runtimeState.GetTicksPerLifetime();

                if (newlifetime >= runtimeState.GetLifetimeProgressMax())
                {
                    runtimeState.SetState(ENPCState.Dead);
                }
            }
        }

    }
}

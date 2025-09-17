using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterSpawningComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField]
        private Transform _spawnAttachment;

        private VisualEffectSpawner _visualSpawner = new VisualEffectSpawner();

        private int _spawnEndTick;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {

        }

        public void UpdateSpawningState(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            if (tick > _spawnEndTick)
            {
                runtimeState.SetState(ENPCState.Idle);
            }
        }

        public void StartSpawnState(int tick)
        {
            NonPlayerCharacterSpawnState spawnState = _npc.RuntimeState.Definition.SpawnState;
            var animTrigger = spawnState.AnimationTrigger;

            _spawnEndTick = tick + (int)(spawnState.StateTime * 32);
            _npc.AnimationController.SetAnimationForTrigger(animTrigger, true);

            SpawnImpactVisualEffect(spawnState);
        }

        public void SpawnImpactVisualEffect(NonPlayerCharacterSpawnState spawnState)
        {
            if (spawnState.SpawnEffect.Name != "")
                _visualSpawner.SpawnVisualEffectAttached(_spawnAttachment, _spawnAttachment.rotation, spawnState.SpawnEffect);
        }
    }
}

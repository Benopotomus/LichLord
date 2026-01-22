using DWD.Pooling;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterHitReactComponent : MonoBehaviour
    {
        [SerializeField]
        private NonPlayerCharacter _npc;

        [SerializeField]
        private List<HitReactionDefinition> _hitReacts = new List<HitReactionDefinition>();

        int _hitReactEndTick;

        [SerializeField]
        private Transform _impactAttachment;

        // Additive Hit Reaction
        private int _currentAdditiveReactIndex = 0;
        public int CurrentAdditiveReactIndex => _currentAdditiveReactIndex;

        int _additiveHitReactEndTick;

        [SerializeField]
        private List<AdditiveHitReactionDefinition> _additiveHitReacts = new List<AdditiveHitReactionDefinition>();

        private VisualEffectSpawner _visualSpawner = new VisualEffectSpawner();

        private void Start()
        {
            _visualSpawner.OnLoadedAttached += OnVisualsPrefabLoadedAttached;
        }

        public void UpdateHitReactState(NonPlayerCharacterRuntimeState runtimeState, int tick)
        { 
            if (tick > _hitReactEndTick)
            {
                runtimeState.SetState(ENPCState.Idle);
            }
        }

        public void UpdateAdditiveHitReactState(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            int hitReactIndex = runtimeState.GetAdditiveHitReact();

            if (hitReactIndex > 0 && 
                tick >_additiveHitReactEndTick)
            {
                runtimeState.SetAdditiveHitReact(0);
            }

            if(hitReactIndex > 0  && 
                _currentAdditiveReactIndex != hitReactIndex)
            {
                StartAdditiveHitReact(hitReactIndex, tick);
                _currentAdditiveReactIndex = hitReactIndex;
            }
        }

        public void StartHitReact(ENPCState state, int animIndex, int tick)
        {
            if (animIndex > _hitReacts.Count)
                return;
            /*
            Debug.Log("Guid: " + _npc.GUID + ", Starting Hit React " + animIndex +
                ", tick: " + _npc.Context.Runner.Tick);
            */
            HitReactionDefinition hitReact = _hitReacts[animIndex];
            var animTrigger = hitReact.AnimationTrigger;

            _hitReactEndTick = tick + hitReact.TickDuration;
            _npc.AnimationController.SetAnimationForTrigger(animTrigger);

            SpawnImpactVisualEffect(animIndex);
        }

        public void StartAdditiveHitReact(int reactIndex, int tick)
        {
            if (_additiveHitReacts.Count == 0)
                return;

            AdditiveHitReactionDefinition additiveHitReact = _additiveHitReacts[reactIndex];
            var animTrigger = additiveHitReact.AdditiveAnimationTrigger;

            _npc.AnimationController.SetAdditiveAnimationForTrigger(animTrigger);

            SpawnImpactVisualEffect(reactIndex);

            _additiveHitReactEndTick = tick + 16;
        }

        public void SpawnImpactVisualEffect(int animIndex)
        {
            HitReactionDefinition hitReact = _hitReacts[animIndex];
            var animTrigger = hitReact.AnimationTrigger;

            if (hitReact.HitEffect.Name != "")
                _visualSpawner.SpawnVisualEffectAttached(_impactAttachment, _impactAttachment.rotation, hitReact.HitEffect);
        }

        private void OnVisualsPrefabLoadedAttached(GameObject loadedGameObject, Transform attachment, Quaternion rotation)
        {
            var poolObject = loadedGameObject.GetComponent<DWDObjectPoolObject>();

            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn Visuals Prefab for Impact");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAttached(poolObject, attachment.position, attachment.rotation, attachment);
            if(instance is StandaloneVisualEffect standaloneEffect)
                standaloneEffect.Initialize();
        }

        private void OnDestroy()
        {
            _visualSpawner.OnLoadedAttached -= OnVisualsPrefabLoadedAttached;
        }
    }
}

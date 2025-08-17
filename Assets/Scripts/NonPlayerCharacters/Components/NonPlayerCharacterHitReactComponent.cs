using DWD.Pooling;
using DWD.Utility.Loading;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterHitReactComponent : MonoBehaviour
    {
        [SerializeField]
        private NonPlayerCharacter _npc;

        [SerializeField]
        private List<NonPlayerCharacterHitReactState> _hitReacts = new List<NonPlayerCharacterHitReactState>();

        float _hitReactTimer = 0.5f;

        [SerializeField]
        private Transform _impactAttachment;

        private VisualEffectSpawner _visualSpawner = new VisualEffectSpawner();

        private void Start()
        {
            _visualSpawner.OnLoadedAttached += OnVisualsPrefabLoadedAttached;
        }

        public void UpdateHitReactState(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            //Debug.Log(_hitReactTimer);
            _hitReactTimer -= renderDeltaTime;
            if (_hitReactTimer < 0f)
            {
                data.State = ENonPlayerState.Idle;
                _npc.Replicator.UpdateNPCData(ref data, _npc.Index);
            }
        }

        public void StartHitReact(ENonPlayerState state, int animIndex)
        {
            if (animIndex > _hitReacts.Count)
                return;
            /*
            Debug.Log("Guid: " + _npc.GUID + ", Starting Hit React " + animIndex +
                ", tick: " + _npc.Context.Runner.Tick);
            */
            NonPlayerCharacterHitReactState hitReact = _hitReacts[animIndex];
            var animTrigger = hitReact.AnimationTrigger;

            _hitReactTimer = hitReact.StateTime;
            _npc.AnimationController.SetAnimationForTrigger(animTrigger);

            SpawnImpactVisualEffect(animIndex);
        }

        public void SpawnImpactVisualEffect(int animIndex)
        {
            NonPlayerCharacterHitReactState hitReact = _hitReacts[animIndex];
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

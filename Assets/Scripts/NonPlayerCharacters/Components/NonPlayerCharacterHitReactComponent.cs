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

        public void UpdateHitReactState(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            //Debug.Log(_hitReactTimer);
            _hitReactTimer -= renderDeltaTime;
            if (_hitReactTimer < 0f)
            {
                data.State = ENonPlayerState.Idle;
                _npc.Replicator.UpdateNPCData(ref data);
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
            var hitReact = _hitReacts[animIndex];
            var animTrigger = hitReact.AnimationTrigger;

            _hitReactTimer = hitReact.StateTime;
            _npc.AnimationController.SetAnimationForTrigger(animTrigger);
        }
    }
}

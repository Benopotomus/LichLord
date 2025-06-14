using System.Data;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterStateComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField] private ENonPlayerState _currentState = ENonPlayerState.Inactive;
        public ENonPlayerState CurrentState => _currentState;

        float _hitReactTimeMax = 0.5f;
        float _hitReactTimer = 0.5f;

        float _deadTimeMax = 15.0f;
        float _deadTimer = 15.0f;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
        }

        public void UpdateState(ref FNonPlayerCharacterData data)
        {
            ENonPlayerState newState = data.State;
            int animIndex = data.AnimationIndex;

            if (_currentState == newState)
                return;

            switch (newState)
            {
                case ENonPlayerState.Idle:
                    NPC.Hurtbox.SetHitBoxesActive(true);
                    break;

                case ENonPlayerState.Inactive:
                    NPC.Hurtbox.SetHitBoxesActive(false);
                    break;
                case ENonPlayerState.Dead:
                    _deadTimer = _deadTimeMax;
                    NPC.Animator.SetInteger("Weapon", 0);
                    NPC.Animator.SetInteger("TriggerNumber", 20);
                    NPC.Animator.SetTrigger("Trigger");
                    NPC.Hurtbox.SetHitBoxesActive(false);
                    break;
                case ENonPlayerState.HitReact:
                    //12 flinch
                    //26 knockback
                    //20 dead
                    //Debug.Log("Trigger Hit React");
                    _hitReactTimer = _hitReactTimeMax;
                    NPC.Animator.SetBool("Moving", false);
                    NPC.Animator.SetInteger("Action", 1);
                    NPC.Animator.SetInteger("Weapon", 0);
                    NPC.Animator.SetBool("Blocking", false);
                    NPC.Animator.SetInteger("TriggerNumber", 12);
                    NPC.Animator.SetTrigger("Trigger");
                    break;

                case ENonPlayerState.Maneuver_1:
                    NPC.Brain.SetAnimationForManeuver(newState, animIndex);
                    break;
            }

            _currentState = newState;
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            switch (_currentState)
            {
                case ENonPlayerState.HitReact:

                    //Debug.Log(_hitReactTimer);
                    _hitReactTimer -= renderDeltaTime;
                    if (_hitReactTimer < 0f)
                    {
                        data.State = ENonPlayerState.Idle;
                        NPC.Replicator.UpdateNPCData(data);
                    }
                    break;
                case ENonPlayerState.Dead:

                    _deadTimer -= renderDeltaTime;
                    if (_deadTimer < 0f)
                    {
                        data.State = ENonPlayerState.Inactive;
                        NPC.Replicator.UpdateNPCData(data);
                    }
                    break;
            }
        }
    }
}

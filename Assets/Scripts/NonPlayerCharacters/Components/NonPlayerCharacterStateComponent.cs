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

        [SerializeField] private int _currentAnimIndex;
        public int CurrentAnimIndex => _currentAnimIndex;

        float _deadTimeMax = 15.0f;
        float _deadTimer = 15.0f;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
        }

        public void UpdateState(ref FNonPlayerCharacterData data)
        {
            ENonPlayerState newState = data.State;
            int animIndex = data.AnimationIndex;

            if (_currentState == newState &&
                _currentAnimIndex == animIndex)
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
                    NPC.HitReact.StartHitReact(newState, animIndex);
                    break;

                case ENonPlayerState.Maneuver_1:
                case ENonPlayerState.Maneuver_2:
                case ENonPlayerState.Maneuver_3:
                case ENonPlayerState.Maneuver_4:
                    NPC.Brain.SetAnimationForManeuver(newState, animIndex);
                    break;
            }

            _currentAnimIndex = animIndex;
            _currentState = newState;
        }

        public void AuthorityUpdate(ref FNonPlayerCharacterData data, float renderDeltaTime)
        {
            switch (_currentState)
            {
                case ENonPlayerState.HitReact:
                    NPC.HitReact.UpdateHitReactState(ref data, renderDeltaTime);
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

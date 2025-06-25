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

        float _deadTimeMax = 5.0f;
        float _deadTimer = 5.0f;

        public void OnSpawned(ref FNonPlayerCharacterSpawnParams spawnParams)
        {
        }

        public void UpdateState(ref FNonPlayerCharacterData data, bool hasAuthority)
        {
            ENonPlayerState newState = data.State;
            int animIndex = data.AnimationIndex;

            if (_currentState == newState &&
                _currentAnimIndex == animIndex)
                return;

            NPC.AnimationController.SetAnimationForState(newState);

            switch (newState)
            {
                case ENonPlayerState.Idle:
                    NPC.Hurtbox.SetHitBoxesActive(true);
                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.locked = false;
                        NPC.Movement.AIFollower.rvoSettings.priority = 0.5f;
                        NPC.Movement.SetFollowerUpdatePosition(true);
                        NPC.Movement.SetFollowerLocalAvoidance(true);
                        NPC.Movement.SetFollowerCanMove(true);
                        NPC.Movement.AIFollower.destination = NPC.CachedTransform.position;
                    }
                    break;

                case ENonPlayerState.Inactive:
                    NPC.Hurtbox.SetHitBoxesActive(false);
                    if (hasAuthority)
                    {
                        NPC.Movement.SetFollowerUpdatePosition(false);
                        NPC.Movement.SetFollowerLocalAvoidance(false);
                    }
                    break;
                case ENonPlayerState.Dead:
                    _deadTimer = _deadTimeMax;
                    NPC.Hurtbox.SetHitBoxesActive(false);
                    NPC.Collider.enabled = false;
                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.priority = 0.5f;
                        NPC.Movement.SetFollowerUpdatePosition(false);
                        NPC.Movement.SetFollowerLocalAvoidance(false);
                        NPC.Movement.SetFollowerCanMove(false);
                    }
                    break;
                case ENonPlayerState.HitReact:
                    NPC.HitReact.StartHitReact(newState, animIndex);

                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.locked = false;
                        NPC.Movement.AIFollower.rvoSettings.priority = 0.5f;
                        NPC.Movement.AIFollower.destination = NPC.CachedTransform.position;
                        NPC.Movement.SetFollowerUpdateRotation(false);
                        NPC.Movement.SetFollowerUpdatePosition(false);
                    }
                    break;

                case ENonPlayerState.Maneuver_1:
                case ENonPlayerState.Maneuver_2:
                case ENonPlayerState.Maneuver_3:
                case ENonPlayerState.Maneuver_4:
                    NPC.Brain.SetAnimationForManeuver(newState, animIndex);
                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.locked = true;
                        NPC.Movement.AIFollower.destination = NPC.CachedTransform.position;
                        NPC.Movement.AIFollower.rvoSettings.priority = 1;
                    }
                    break;
            }

            // last state
            switch (_currentState)
            {
                case ENonPlayerState.Dead:
                case ENonPlayerState.Inactive:
                    NPC.CachedTransform.position = data.Position;
                    NPC.CachedTransform.rotation = data.Rotation;
                    NPC.Collider.enabled = true;
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

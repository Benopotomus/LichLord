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
            ENonPlayerState oldState = _currentState;
            ENonPlayerState newState = data.State;
            int animIndex = data.AnimationIndex;

            if (_currentState == newState &&
                _currentAnimIndex == animIndex)
                return;

            NPC.AnimationController.SetAnimationForState(oldState, newState);
            //Debug.Log("oldState " + oldState + " to " + newState);

            switch (oldState)
            { 
                case ENonPlayerState.Inactive:
                    NPC.Movement.AIFollower.Teleport(data.Position);
                    //Debug.Log("teleporting " + data.Position);
                    break;

            }

            switch (newState)
            {
                case ENonPlayerState.Idle:
                    NPC.Collider.enabled = true;
                    NPC.Hurtbox.SetHitBoxesActive(true);

                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.locked = false;
                        NPC.Movement.AIFollower.rvoSettings.priority = 0.5f;
                        NPC.Movement.SetFollowerUpdatePosition(true);
                        NPC.Movement.SetFollowerUpdateRotation(true);
                        NPC.Movement.SetFollowerLocalAvoidance(true);
                        NPC.Movement.SetFollowerCanMove(true);
                        //NPC.Movement.AIFollower.destination = NPC.CachedTransform.position;
                    }
                    break;

                case ENonPlayerState.Inactive:
                    NPC.Hurtbox.SetHitBoxesActive(false);
                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.priority = 0.5f;
                        NPC.Movement.SetFollowerUpdatePosition(false);
                        NPC.Movement.SetFollowerUpdateRotation(false);
                        NPC.Movement.SetFollowerLocalAvoidance(false);
                        NPC.Movement.SetFollowerCanMove(false);
                    }
                    break;
                case ENonPlayerState.Dead:
                    _deadTimer = _deadTimeMax;
                    NPC.Hurtbox.SetHitBoxesActive(false);
                    NPC.Collider.enabled = false;
                    NPC.HitReact.SpawnImpactVisualEffect(0);

                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.priority = 0.5f;
                        NPC.Movement.SetFollowerUpdatePosition(false);
                        NPC.Movement.SetFollowerUpdateRotation(false);
                        NPC.Movement.SetFollowerLocalAvoidance(false);
                        NPC.Movement.SetFollowerCanMove(false);
                    }
                    break;
                case ENonPlayerState.HitReact:
                    NPC.Collider.enabled = true;
                    NPC.HitReact.StartHitReact(newState, animIndex);

                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.locked = true;
                        NPC.Movement.AIFollower.rvoSettings.priority = 0.5f;
                        //NPC.Movement.AIFollower.destination = NPC.CachedTransform.position;
                        NPC.Movement.SetFollowerUpdateRotation(false);
                        NPC.Movement.SetFollowerUpdatePosition(false);
                    }
                    break;

                case ENonPlayerState.Maneuver_1:
                case ENonPlayerState.Maneuver_2:
                case ENonPlayerState.Maneuver_3:
                case ENonPlayerState.Maneuver_4:
                    NPC.Collider.enabled = true;
                    NPC.Brain.SetAnimationForManeuver(newState, animIndex);

                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.locked = true;
                        //NPC.Movement.AIFollower.destination = NPC.CachedTransform.position;
                        NPC.Movement.AIFollower.rvoSettings.priority = 1;
                    }
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
                        _currentState = ENonPlayerState.Inactive;
                        data.State = ENonPlayerState.Inactive;
                        NPC.Replicator.UpdateNPCData(ref data, _npc.Index);
                    }
                    break;
            }
        }
    }
}

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

        int _deathTicks = 64;
        int _deathEndTick;

        public void UpdateStateChange(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority, int tick)
        {
            ENonPlayerState oldState = _currentState;
            ENonPlayerState newState = runtimeState.GetState();
            int animIndex = runtimeState.GetAnimationIndex();

            if (_currentState == newState &&
                _currentAnimIndex == animIndex)
                return;

            NPC.AnimationController.SetAnimationForState(oldState, newState);
            //Debug.Log("oldState " + oldState + " to " + newState);

            switch (oldState)
            {
                case ENonPlayerState.Dead:
                case ENonPlayerState.Inactive:
                    NPC.Movement.AIFollower.Teleport(runtimeState.GetPosition());
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
                    _deathEndTick = tick + _deathTicks;
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
                    NPC.HitReact.StartHitReact(newState, animIndex, tick);

                    if (hasAuthority)
                    {
                        NPC.Movement.AIFollower.rvoSettings.locked = true;
                        NPC.Movement.AIFollower.rvoSettings.priority = 0.5f;

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

                        NPC.Movement.AIFollower.rvoSettings.priority = 1;
                    }
                    break;
            }

            _currentAnimIndex = animIndex;
            _currentState = newState;
        }

        // State Authority Only
        public void UpdateCurrentState(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            switch (_currentState)
            {
                case ENonPlayerState.HitReact:
                    NPC.HitReact.UpdateHitReactState(runtimeState, tick);
                    break;
                case ENonPlayerState.Dead:

                    if (tick > _deathEndTick)
                    {
                        runtimeState.SetState(ENonPlayerState.Inactive);
                    }

                    break;
            }
        }
    }
}

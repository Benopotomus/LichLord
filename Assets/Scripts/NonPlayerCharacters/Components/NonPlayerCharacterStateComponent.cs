using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterStateComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField] private ENPCState _currentState = ENPCState.Inactive;
        public ENPCState CurrentState => _currentState;

        [SerializeField] private int _currentAnimIndex;
        public int CurrentAnimIndex => _currentAnimIndex;

        private int _deathTicks = 64;
        private int _deathEndTick;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority, int tick)
        {
            UpdateState(runtimeState, hasAuthority, tick, true);
        }

        public void StartRecycle()
        {
            _currentState = ENPCState.Inactive;
        }

        public void UpdateState(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority, int tick, bool forceUpdate = false)
        {
            UpdateStateChange(runtimeState, hasAuthority, tick, forceUpdate);

            if (hasAuthority)
                UpdateCurrentState(runtimeState, tick);
        }

        private void UpdateStateChange(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority, int tick, bool forceUpdate = false)
        {
            ENPCState oldState = _currentState;
            ENPCState newState = runtimeState.GetState();
            int animIndex = runtimeState.GetAnimationIndex();

            if (!forceUpdate && _currentState == newState && _currentAnimIndex == animIndex)
                return;

            NPC.AnimationController.SetAnimationForState(oldState, newState);

            // Teleport NPC if transitioning from Dead or Inactive
            if (oldState == ENPCState.Dead || oldState == ENPCState.Inactive)
            {
                NPC.Movement.AIFollower.Teleport(runtimeState.GetPosition());
            }

            // Handle new state
            switch (newState)
            {
                case ENPCState.Idle:
                    NPC.Collider.enabled = true;
                    NPC.Hurtbox.SetHitBoxesActive(true);
                    if (hasAuthority)
                    {
                        SetRVOSettings(false, 0.5f); // unlocked, can move
                        SetFollowerMovement(true, true,  true);
                    }
                    break;

                case ENPCState.Maneuver_1:
                case ENPCState.Maneuver_2:
                case ENPCState.Maneuver_3:
                case ENPCState.Maneuver_4:
                    NPC.Collider.enabled = true;
                    NPC.Brain.SetAnimationForManeuver(newState, animIndex);
                    if (hasAuthority)
                    {
                        SetRVOSettings(true, 1f); // unlocked, higher priority
                        SetFollowerMovement(true, true,  true);
                    }
                    break;

                case ENPCState.HitReact:
                    NPC.Collider.enabled = true;
                    NPC.HitReact.StartHitReact(newState, animIndex, tick);
                    if (hasAuthority)
                    {
                        SetRVOSettings(true, 0.5f); // lock agent during hit
                        SetFollowerMovement(false, false,  false);
                    }
                    break;

                case ENPCState.Dead:
                    _deathEndTick = tick + _deathTicks;
                    NPC.Collider.enabled = false;
                    NPC.Hurtbox.SetHitBoxesActive(false);
                    NPC.HitReact.SpawnImpactVisualEffect(0);
                    if (hasAuthority)
                    {
                        SetRVOSettings(true, 0.5f); // locked
                        SetFollowerMovement(false, false,  false);
                    }
                    break;

                case ENPCState.Inactive:
                    NPC.Hurtbox.SetHitBoxesActive(false);
                    if (hasAuthority)
                    {
                        SetRVOSettings(true, 0.5f); // locked
                        SetFollowerMovement(false, false, false);
                    }
                    break;

                case ENPCState.Spawning:
                    NPC.Collider.enabled = false;
                    NPC.Hurtbox.SetHitBoxesActive(false);
                    NPC.SpawningComponent.StartSpawnState(tick);
                    if (hasAuthority)
                    {
                        SetRVOSettings(true, 0.5f); // locked
                        SetFollowerMovement(false, false, false);
                    }
                    break;
            }

            if (runtimeState.IsWorker())
            {
                runtimeState.SendWorkerStateChanged(newState);
            }

            _currentAnimIndex = animIndex;
            _currentState = newState;
        }

        private void UpdateCurrentState(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            switch (runtimeState.GetState())
            {
                case ENPCState.Spawning:
                    NPC.SpawningComponent.UpdateSpawningState(runtimeState, tick);
                    break;
                case ENPCState.HitReact:
                    NPC.HitReact.UpdateHitReactState(runtimeState, tick);
                    break;
                case ENPCState.Dead:
                    if (tick > _deathEndTick)
                        runtimeState.SetState(ENPCState.Inactive);
                    break;
            }
        }

        // --- Helpers ---
        private void SetRVOSettings(bool locked, float priority)
        {
            var follower = NPC.Movement.AIFollower;

            if (!follower.enableLocalAvoidance)
                return;

            var settings = follower.rvoSettings;

            settings.locked = locked;
            settings.priority = priority;
            follower.rvoSettings = settings;
        }

        private void SetFollowerMovement(bool updatePosition, bool updateRotation, bool canMove)
        {
            NPC.Movement.SetFollowerUpdatePosition(updatePosition);
            NPC.Movement.SetFollowerUpdateRotation(updateRotation);
            NPC.Movement.SetFollowerCanMove(canMove);
        }
    }
}

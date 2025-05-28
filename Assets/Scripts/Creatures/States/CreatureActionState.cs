using Fusion;
using Fusion.Addons.FSM;
using UnityEngine;

namespace LichLord
{
    [Tooltip("Player state entered when performing attacks.")]
    public class CreatureActionState : CreatureStateBase
    {
        [Tooltip("The curve at which the player moves on their forward vector.")]
        public AnimationCurve movementCurve;

        [Tooltip("Reference to the next state once the attack ends.")]
        public StateBehaviour nextState;

        [Tooltip("How much time, in seconds, must the player wait before entering their next attack.")]
        public float nextAtkWaitTime;

        [Tooltip("Reference to the next attack if attack is pressed again; if null, the player will always return to idle.")]
        public StateBehaviour nextAtkState;

        [Tooltip("What attack level is required to perform the next attack.  If not at the required level, the next attack will not be performed.")]
        public int nextAtkLvlReq;

        [Tooltip("The length of the state in seconds; once exceeded, the character will return to their idle state.")]
        public float stateLength = 21 / 60f;

        // Radius of the attack
        [Tooltip("How much damage does this attack do?")]
        public int damage;

        [Tooltip("The time, in seconds, in which the attack will become active.")]
        public float attackRangeTimeMin;

        [Tooltip("The time, in seconds, in which the attack will become inactive.")]
        public float attackRangeTimeMax;

        [Tooltip("The layers that can be hit by this attack.")]
        public LayerMask attackableLayerMask;

        Collider[] hitsColliders = new Collider[8];

        // Attack info
        [Tooltip("The radius of the attack.")]
        public float attackRadius;

        [Tooltip("The offset of the attack position.")]
        public Vector3 positionOffset;

        [Tooltip("The amount of knockback caused by this attack.")]
        public float knockbackStrength = 5f;

        [Tooltip("The amount of time some enemies will be stunned when hit by this attack.")]
        public float hitStunLength = 1f;

        [Networked, Capacity(8), Tooltip("How many targets where hit by this attack.  Used to prevent attacking the same enemy multiple times.")]
        public NetworkLinkedList<NetworkObject> hitTargets => default;

        protected override void OnEnterState()
        {

        }

        protected override void OnEnterStateRender()
        {
            anim.CrossFadeInFixedTime(animState, animTransitionLength);
        }

        protected override void OnExitState()
        {
            hitTargets.Clear();
            base.OnExitState();
        }

        protected override void OnExitStateRender()
        {

        }

        protected override void OnFixedUpdate()
        {
            /*
            simpleKCC.Move(transform.forward * movementCurve.Evaluate(Machine.StateTime), 0f);

            if (Machine.StateTime >= stateLength)
            {
                Machine.ForceActivateState(nextState);
                return;
            }
            else if (fsmRef.PlayerNetworkObject.AttackLevel >= nextAtkLvlReq && nextAtkState != null && Machine.StateTime >= nextAtkWaitTime)
            {
                if (fsmRef.CurrentInput.buttons.WasPressed(fsmRef.PreviousInput.buttons, GameInput.ATK_BUTTON_INDEX))
                {
                    Machine.ForceActivateState(nextAtkState);
                    return;
                }
            }

            if (Machine.StateTime >= attackRangeTimeMin && Machine.StateTime <= attackRangeTimeMax)
            {
                // Perform the attack check here...
                int result = Runner.GetPhysicsScene().OverlapSphere(transform.position + transform.rotation * positionOffset, attackRadius, hitsColliders,
                    attackableLayerMask, QueryTriggerInteraction.Collide);

                for (int i = 0; i < result; i++)
                {
                    Attackable target = hitsColliders[i].GetComponent<Attackable>();

                    // If no enemy has been hit or this target has already been hit, we continue.
                    if (target == null || hitTargets.Contains(target.Object))
                        continue;

                    AttackInfo attackState = new AttackInfo()
                    {
                        damage = damage,
                        knockbackVector = transform.forward * knockbackStrength,
                        hitRecoveryTime = hitStunLength,
                    };
                    target.OnHitLocal(attackState, fsmRef.PlayerNetworkObject);

                    if (i >= hitTargets.Count)
                        hitTargets.Add(target.Object);
                    else
                        hitTargets.Set(i, target.Object);
                }
            }
            */
        }
    }
}
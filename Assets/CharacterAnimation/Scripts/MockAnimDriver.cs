using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static MockAnimDriver;

public class MockAnimDriver : MonoBehaviour
{

    [Header("References")]
    public Animator Animator;

    [Header("Character stuff")]
    public Vector3 PreviousMoveDir;
    public Weapon CurrentWeapon;
    public Sheath SheathLocation;

    private void Start()
    {
        Relax();
    }

    private void Update()
    {
        Vector3 moveDir = Vector3.zero;

        // Movement Direction
        if(Input.GetKey(KeyCode.A))
        {
            moveDir.x -= 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveDir.x += 1;
        }
        if (Input.GetKey(KeyCode.W))
        {
            moveDir.z += 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveDir.z -= 1;
        }
        SetMovementDirection(moveDir.normalized);

        // Movement State
        if(moveDir.magnitude > 0)
        {
            SetMovementState(MovementState.Moving);
        }
        else
        {
            SetMovementState(MovementState.Idle);
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            SetMovementState(MovementState.Sprinting);
        }
        else if(Input.GetKey(KeyCode.LeftControl))
        {
            SetMovementState(MovementState.Crouching);
        }
    }

    /// <summary>
    /// Movement states of the Animation Controller
    /// </summary>
    public enum MovementState
    {
        Idle,
        Moving,
        Sprinting,
        Crouching
    }

    /// <summary>
    /// Weapon types of the Animation Controller.
    /// All of them 2 handed animations, apart from the Staff
    /// </summary>
    public enum Weapon
    {
        None = 0,
        TwoHandSword,
        TwoHandSpear,
        TwoHandAxe,
        TwoHandBow,
        TwoHandCrossbow,
        Staff
    }

    /// <summary>
    /// Sheath positions of the Animation Controller
    /// </summary>
    public enum Sheath
    {
        Back = 0,
        Hips
    }

    /// <summary>
    /// Damage types.
    /// 2 Front hits
    /// 1 Back hit
    /// 1 hit for left and right
    /// 2 knock back animations
    /// </summary>
    public enum Damage
    {
        FrontHit1 = 1,
        FrontHit2,
        BackHit1,
        LeftHit1,
        RightHit1,
        Knockback1,
        Knockback2,
    }

    /// <summary>
    /// 3-3 animations for left and right sided attacks
    /// </summary>
    public enum AttackType
    {
        Left1 = 1,
        Left2,
        Left3,
        Right1,
        Right2,
        Right3
    }

    /// <summary>
    /// 3 Cast animations, that are aimed forward
    /// Buff1 aimed up, buff2 aimed down
    /// AOE1 aimed up and AOE2 aimed down
    /// 2 Summon animations
    /// </summary>
    public enum SpellType
    {
        Cast1 = 1,
        Cast2,
        Cast3,
        Buff1,
        Buff2,
        AOE1,
        AOE2,
        Summon1,
        Summon2
    }

    /// <summary>
    /// Pickup picks up something from the ground and puts it in their pocket
    /// Activate looks like a button press forward
    /// Boost ?
    /// </summary>
    public enum Interaction
    {
        Pickup = 2,
        Activate = 3,
        Boost = 9
    }

    /// <summary>
    /// Call this to start the Animation Controller in a default doing nothing state
    /// </summary>
    public void Relax()
    {
        Animator.SetInteger("Weapon", -1);
        Animator.SetInteger("TriggerNumber", 25);
        Animator.SetTrigger("Trigger");
    }

    /// <summary>
    /// Start of the jump animation, the liftoff
    /// </summary>
    public void Jump()
    {
        Animator.SetInteger("Jumping", 1);
        Animator.SetInteger("TriggerNumber", 18);
        Animator.SetTrigger("Trigger");
    }

    /// <summary>
    /// Mid part of the jump animation, this loops
    /// </summary>
    public void Flight()
    {
        Animator.SetInteger("Jumping", 2);
        Animator.SetInteger("TriggerNumber", 18);
        Animator.SetTrigger("Trigger");
    }

    /// <summary>
    /// The landing of the jump / flight.
    /// Dont call this function "Land" that is reserved for the animation event that is invoked when the landing anim ends
    /// </summary>
    public void LandFromJump()
    {
        Animator.SetInteger("Jumping", 0);
        Animator.SetInteger("TriggerNumber", 18);
        Animator.SetTrigger("Trigger");
    }

    /// <summary>
    /// Set the movement state of the animation controller
    /// Crouch - Walk/Run - Sprint
    /// </summary>
    /// <param name="movement"></param>
    public void SetMovementState(MovementState movement)
    {
        switch(movement)
        {
            case MovementState.Idle:
                Animator.SetBool("Moving", false);
                Animator.SetBool("Sprint", false);
                Animator.SetBool("Crouch", false);
                break;
            case MovementState.Moving:
                Animator.SetBool("Moving", true);
                Animator.SetBool("Sprint", false);
                Animator.SetBool("Crouch", false);
                break;
            case MovementState.Sprinting:
                Animator.SetBool("Moving", true);
                Animator.SetBool("Sprint", true);
                Animator.SetBool("Crouch", false);
                break;
            case MovementState.Crouching:
                Animator.SetBool("Moving", true);
                Animator.SetBool("Crouch", true);
                Animator.SetBool("Sprint", false);
                break;
        }
    }

    /// <summary>
    /// Sets the parameters of the movement blendspaces
    /// A vector magnitude of 0.5 is a walk
    /// 1.0 is a run
    /// </summary>
    /// <param name="moveDir"></param>
    public void SetMovementDirection(Vector3 moveDir)
    {
        Animator.SetFloat("Velocity X", moveDir.x);
        Animator.SetFloat("Velocity Z", moveDir.z);
    }

    /// <summary>
    /// Sets the sheath location used for the unequip animations
    /// </summary>
    /// <param name="sheath"></param>
    public void SetSheath(Sheath sheath)
    {
        SheathLocation = sheath;
    }

    /// <summary>
    /// Plays an equip animation for a weapon, and enters the state machine of it.
    /// </summary>
    /// <param name="weapon"></param>
    /// <param name="sheath"></param>
    public void EquipWeapon(Weapon weapon, Sheath sheath)
    {
        CurrentWeapon = weapon;

        Animator.SetBool("Moving", false);
        Animator.SetInteger("TriggerNumber", 16);
        Animator.SetInteger("SheathLocation", (int)sheath);
        Animator.SetInteger("WeaponSwitch", 0);
        Animator.SetInteger("Weapon", (int)CurrentWeapon);

        Animator.SetTrigger("Trigger");
    }

    /// <summary>
    /// Unequips the current weapon and returns to a relaxed state
    /// </summary>
    public void UnEquipWeapon()
    {
        Animator.SetBool("Moving", false);
        Animator.SetInteger("TriggerNumber", 15);

        Animator.SetInteger("SheathLocation", (int)SheathLocation);
        Animator.SetInteger("WeaponSwitch", -1);

        Animator.SetInteger("Weapon", (int)CurrentWeapon);

        Animator.SetTrigger("Trigger");
    }

    /// <summary>
    /// Getting hit / Taking damage clips
    /// </summary>
    /// <param name="damage"></param>
    public void TakeDamage(Damage damage)
    {
        if(damage != Damage.Knockback1 && damage != Damage.Knockback2)
        {
            Animator.SetBool("Blocking", false);
            Animator.SetInteger("Action", (int)damage);
            Animator.SetInteger("TriggerNumber", 12);
        }
        else
        {
            Animator.SetBool("Blocking", true);
            Animator.SetInteger("Action", (int)damage - 5);
            Animator.SetInteger("TriggerNumber", 26);
        }
        
        Animator.SetInteger("Weapon", (int)CurrentWeapon);
        Animator.SetTrigger("Trigger");

        // Reset block
        Animator.SetBool("Blocking", false);
    }

    /// <summary>
    /// Plays attack animations.
    /// All meelee other than for the Bow and Crossbow
    /// </summary>
    /// <param name="attack"></param>
    public void Attack(AttackType attack)
    {
        Animator.SetBool("Moving", false);
        Animator.SetInteger("TriggerNumber", 4);
        Animator.SetTrigger("Trigger");

        Animator.SetInteger("Action", (int)attack);
    }

    /// <summary>
    /// Special spell animations for the Staff.
    /// Equip the staff first, to be in the correct state machine.
    /// These animations have a looping core. This function starts that, and for demonstration ends it after 0.5 seconds.
    /// </summary>
    /// <param name="spell"></param>
    public async void SpellCast(SpellType spell)
    {
        if((int)spell < 4)
        {
            Animator.SetInteger("TriggerNumber", 7);
            Animator.SetInteger("Action", (int)spell);
        }
        else
        {
            Animator.SetInteger("TriggerNumber", 10);
            Animator.SetInteger("Action", (int)spell - 3);
        }

        Animator.SetTrigger("Trigger");

        // Shut the cast off after 1 sec
        await Task.Delay(500);
        StopSpellCast();
    }

    /// <summary>
    /// Stops the currently playing looping spell cast
    /// </summary>
    public void StopSpellCast()
    {
        Animator.SetInteger("TriggerNumber", 11);
        Animator.SetTrigger("Trigger");
    }

    /// <summary>
    /// Plays one of the 3 interaction animations
    /// Pickup - Activate - Boost
    /// </summary>
    /// <param name="interaction"></param>
    public void Interact(Interaction interaction)
    {
        Animator.SetInteger("Action", (int)interaction);
        Animator.SetInteger("TriggerNumber", 2);
        Animator.SetTrigger("Trigger");
    }
}

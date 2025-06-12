using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockAnimDriver : MonoBehaviour
{

    [Header("References")]
    public Animator Animator;

    [Header("Parameters")]
    public bool Aiming = false;
    public bool Blocking = false;
    public bool Crouch = false;
    public bool Injured = false;
    public bool Moving = false;
    public bool Sprint = false;
    public bool Stunned = false;
    public bool Swimming = false;
    public bool Trigger = false;
    public int TriggerNumber = 0;
    public int Action = 0;
    public int Jumping = 0;
    public int Side = 0;
    public int LeftWeapon = 0;
    public int RightWeapon = 0;
    public int SheathLocation = 0;
    public int Talking = 0;
    public int Weapon = 0;
    public int WeaponSwitch = 0;
    public float Idle = 0;
    public float AimHorizontal = 0f;
    public float AimVertical = 0f;
    public float AnimationSpeed = 1f;
    public float BowPull = 0f;
    public float Charge = 0f;
    public float VelocityX = 0f;
    public float VelocityY = 0f;

    private void Update()
    {
        if(Trigger)
        {
            Animator.SetTrigger("Trigger");
            Trigger = false;
        }

        Animator.SetBool("Aiming", Aiming);
        Animator.SetBool("Blocking", Blocking);
        Animator.SetBool("Crouch", Crouch);
        Animator.SetBool("Injured", Injured);
        Animator.SetBool("Moving", Moving);
        Animator.SetBool("Sprint", Sprint);
        Animator.SetBool("Stunned", Stunned);
        Animator.SetBool("Swimming", Swimming);
        Animator.SetInteger("TriggerNumber", TriggerNumber);
        Animator.SetInteger("Action", Action);
        Animator.SetInteger("Jumping", Jumping);
        Animator.SetInteger("Side", Side);
        Animator.SetInteger("LeftWeapon", LeftWeapon);
        Animator.SetInteger("RightWeapon", RightWeapon);
        Animator.SetInteger("SheathLocation", SheathLocation);
        Animator.SetInteger("Talking", Talking);
        Animator.SetInteger("Weapon", Weapon);
        Animator.SetInteger("WeaponSwitch", WeaponSwitch);
        Animator.SetFloat("Idle", Idle);
        Animator.SetFloat("AimHorizontal", AimHorizontal);
        Animator.SetFloat("AimVertical", AimVertical);
        Animator.SetFloat("AnimationSpeed", AnimationSpeed);
        Animator.SetFloat("BowPull", BowPull);
        Animator.SetFloat("Charge", Charge);
        Animator.SetFloat("Velocity X", VelocityX);
        Animator.SetFloat("Velocity Z", VelocityY);
    }
}

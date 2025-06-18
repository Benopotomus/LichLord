using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MockAnimDriver))]
public class MockAnimDriverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        #region JUMP
        GUILayout.Label("Jump and Flight");
        if (GUILayout.Button("Jump"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Jump();
        }
        if (GUILayout.Button("Fly"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Flight();
        }
        if (GUILayout.Button("Land"))
        {
            ((MockAnimDriver)serializedObject.targetObject).LandFromJump();
        }
        EditorGUILayout.Separator();
        #endregion

        #region SHEATH
        GUILayout.Label("Sheath Location");
        if(GUILayout.Button("Back Sheath"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SetSheath(MockAnimDriver.Sheath.Back);
        }
        if(GUILayout.Button("Hip Sheath"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SetSheath(MockAnimDriver.Sheath.Hips);
        }
        EditorGUILayout.Separator();
        #endregion

        #region WEAPON_EQUIP
        GUILayout.Label("Weapon Equips");
        if (GUILayout.Button("Unequip"))
        {
            ((MockAnimDriver)serializedObject.targetObject).UnEquipWeapon();
        }
        if (GUILayout.Button("Two Hand Sword"))
        {
            ((MockAnimDriver)serializedObject.targetObject).EquipWeapon(MockAnimDriver.Weapon.TwoHandSword, 
                ((MockAnimDriver)serializedObject.targetObject).SheathLocation);
        }
        if (GUILayout.Button("Two Hand Spear"))
        {
            ((MockAnimDriver)serializedObject.targetObject).EquipWeapon(MockAnimDriver.Weapon.TwoHandSpear,
                ((MockAnimDriver)serializedObject.targetObject).SheathLocation);
        }
        if (GUILayout.Button("Two Hand Axe"))
        {
            ((MockAnimDriver)serializedObject.targetObject).EquipWeapon(MockAnimDriver.Weapon.TwoHandAxe,
                ((MockAnimDriver)serializedObject.targetObject).SheathLocation);
        }
        if (GUILayout.Button("Two Hand Bow"))
        {
            ((MockAnimDriver)serializedObject.targetObject).EquipWeapon(MockAnimDriver.Weapon.TwoHandBow,
                ((MockAnimDriver)serializedObject.targetObject).SheathLocation);
        }
        if (GUILayout.Button("Two Hand Crossbow"))
        {
            ((MockAnimDriver)serializedObject.targetObject).EquipWeapon(MockAnimDriver.Weapon.TwoHandCrossbow,
                ((MockAnimDriver)serializedObject.targetObject).SheathLocation);
        }
        if (GUILayout.Button("Staff"))
        {
            ((MockAnimDriver)serializedObject.targetObject).EquipWeapon(MockAnimDriver.Weapon.Staff,
                ((MockAnimDriver)serializedObject.targetObject).SheathLocation);
        }
        EditorGUILayout.Separator();
        #endregion

        #region DAMAGE_TAKE
        GUILayout.Label("Damage Take");
        if (GUILayout.Button("Front Hit 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).TakeDamage(MockAnimDriver.Damage.FrontHit1);
        }
        if (GUILayout.Button("Front Hit 2"))
        {
            ((MockAnimDriver)serializedObject.targetObject).TakeDamage(MockAnimDriver.Damage.FrontHit2);
        }
        if (GUILayout.Button("Back Hit 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).TakeDamage(MockAnimDriver.Damage.BackHit1);
        }
        if (GUILayout.Button("Left Hit 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).TakeDamage(MockAnimDriver.Damage.LeftHit1);
        }
        if (GUILayout.Button("Right Hit 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).TakeDamage(MockAnimDriver.Damage.RightHit1);
        }
        if (GUILayout.Button("Knockback 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).TakeDamage(MockAnimDriver.Damage.Knockback1);
        }
        if (GUILayout.Button("Knockback 2"))
        {
            ((MockAnimDriver)serializedObject.targetObject).TakeDamage(MockAnimDriver.Damage.Knockback2);
        }
        EditorGUILayout.Separator();
        #endregion

        #region ATTACK
        GUILayout.Label("Attack");
        if (GUILayout.Button("Left 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Attack(MockAnimDriver.AttackType.Left1);
        }
        if (GUILayout.Button("Left 2"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Attack(MockAnimDriver.AttackType.Left2);
        }
        if (GUILayout.Button("Left 3"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Attack(MockAnimDriver.AttackType.Left3);
        }
        if (GUILayout.Button("Right 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Attack(MockAnimDriver.AttackType.Right1);
        }
        if (GUILayout.Button("Right 2"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Attack(MockAnimDriver.AttackType.Right2);
        }
        if (GUILayout.Button("Right 3"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Attack(MockAnimDriver.AttackType.Right3);
        }
        EditorGUILayout.Separator();
        #endregion

        #region STAFF_SPELLS
        GUILayout.Label("Spell Casting, Equip Staff First");
        if (GUILayout.Button("Cast 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SpellCast(MockAnimDriver.SpellType.Cast1);
        }
        if (GUILayout.Button("Cast 2"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SpellCast(MockAnimDriver.SpellType.Cast2);
        }
        if (GUILayout.Button("Cast 3"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SpellCast(MockAnimDriver.SpellType.Cast3);
        }
        if (GUILayout.Button("Buff 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SpellCast(MockAnimDriver.SpellType.Buff1);
        }
        if (GUILayout.Button("Buff 2"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SpellCast(MockAnimDriver.SpellType.Buff2);
        }
        if (GUILayout.Button("AOE 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SpellCast(MockAnimDriver.SpellType.AOE1);
        }
        if (GUILayout.Button("AOE 2"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SpellCast(MockAnimDriver.SpellType.AOE2);
        }
        if (GUILayout.Button("Summon 1"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SpellCast(MockAnimDriver.SpellType.Summon1);
        }
        if (GUILayout.Button("Summon 2"))
        {
            ((MockAnimDriver)serializedObject.targetObject).SpellCast(MockAnimDriver.SpellType.Summon2);
        }
        EditorGUILayout.Separator();
        #endregion

        #region INTERACTIONS
        GUILayout.Label("Interactions");
        if (GUILayout.Button("Pickup"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Interact(MockAnimDriver.Interaction.Pickup);
        }
        if (GUILayout.Button("Activate"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Interact(MockAnimDriver.Interaction.Activate);
        }
        if (GUILayout.Button("Boost"))
        {
            ((MockAnimDriver)serializedObject.targetObject).Interact(MockAnimDriver.Interaction.Boost);
        }
        EditorGUILayout.Separator();
        #endregion
    }
}

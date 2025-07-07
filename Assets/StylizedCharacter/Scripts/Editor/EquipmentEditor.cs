using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NHance.Assets.Scripts;

[CustomEditor(typeof(Equipment))]
public class EquipmentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if(GUILayout.Button("Remap Bones"))
        {
            ((Equipment)serializedObject.targetObject).RemapBones();
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }
    }
}

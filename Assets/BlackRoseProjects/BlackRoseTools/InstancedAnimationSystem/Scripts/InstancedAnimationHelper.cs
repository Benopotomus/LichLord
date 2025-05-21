using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using BlackRoseProjects.Utility;
using BlackRoseProjects.InstancedAnimationSystem.Exceptions;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using Unity.Mathematics;
using Unity.Collections;
#endif

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("InstancedAnimationSystem.Editor", AllInternalsVisible = false)]
namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal static class InstancedAnimationHelper
    {
        internal const string INSTANCING_NORMAL_TRANSITION_BLENDING = "INSTANCING_NORMAL_TRANSITION_BLENDING";
        internal const string BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES = "BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES";
        internal const string BLACKROSE_INSTANCING_PROFILING = "BLACKROSE_INSTANCING_PROFILING";
        internal const string BLACKROSE_INSTANCING_SAFETY_CHECKS = "BLACKROSE_INSTANCING_SAFETY_CHECKS";
        internal const string BLACKROSE_INSTANCING_MATH = "BLACKROSE_INSTANCING_MATH";
        internal const string BLACKROSE_INSTANCING_COLLECTIONS = "BLACKROSE_INSTANCING_COLLECTIONS";
        internal const string BLACKROSE_INSTANCING_BURST = "BLACKROSE_INSTANCING_BURST";

        internal const string BakedAnimationFolder = "InstancedAnimationData";

        internal const string BuiltInEditorOutline = "Hidden/BlackRoseProjects/InstancedAnimationSystem/Built-in/CustomEditorOutline";
#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
        private static AnimationComparer animationComparer = new AnimationComparer();
        private static AnimationInfo animationSearchInfo = new AnimationInfo();
#endif

        [System.Diagnostics.Conditional(BLACKROSE_INSTANCING_PROFILING)]
        internal static void BeginSample(string name)
        {
            UnityEngine.Profiling.Profiler.BeginSample(name);
        }

        [System.Diagnostics.Conditional(BLACKROSE_INSTANCING_PROFILING)]
        internal static void EndSample()
        {
            UnityEngine.Profiling.Profiler.EndSample();
        }

        [System.Diagnostics.Conditional(BLACKROSE_INSTANCING_SAFETY_CHECKS)]
        internal static void SafetyCheck(bool value, string description)
        {
            if (!value)
                throw new SafetyFailException(description);
        }

        [System.Diagnostics.Conditional(BLACKROSE_INSTANCING_SAFETY_CHECKS)]
        internal static void SafetyCheck(Array array, int index, string description, object parameters)
        {
            if (index < 0 || index >= array.Length)
                throw new SafetyFailException(description + parameters);
        }

        internal static float FlatScale(this Vector3 vector)
        {
            return (vector.x + vector.y + vector.z) / 3f;
        }

        internal static Shader GetInstancedShaderForShader(Shader shader)
        {
            string shaderName = "";
            if (shader != null)
                shaderName = shader.name;
            Pipelines pipeline = BRPPipelineHelper.GetCurrentPipeline();

            if (pipeline == Pipelines.URP)
            {
                return Shader.Find("BlackRoseProjects/InstancedAnimationSystem/Universal Render Pipeline/BRP-Lit");
            }
            else if (pipeline == Pipelines.HDRP)
            {
                return Shader.Find("BlackRoseProjects/InstancedAnimationSystem/HDRP/BRP-Lit");
            }
            else if (pipeline == Pipelines.Built_In)
            {
                if (shaderName.StartsWith("BlackRoseProjects/InstancedAnimationSystem/Built-in/"))
                    return shader;
                else if (shaderName.StartsWith("Legacy Shaders/"))
                {
                    bool specular = shaderName.Contains("Specular");
                    bool bumped = shaderName.Contains("Bumped");
                    if (specular && bumped)
                        return Shader.Find("BlackRoseProjects/InstancedAnimationSystem/Built-in/Legacy Shaders/Bumped Specular");
                    else if (specular && !bumped)
                        return Shader.Find("BlackRoseProjects/InstancedAnimationSystem/Built-in/Legacy Shaders/Specular");
                    else if (!specular && bumped)
                        return Shader.Find("BlackRoseProjects/InstancedAnimationSystem/Built-in/Legacy Shaders/Bumped Diffuse");
                    else
                        return Shader.Find("BlackRoseProjects/InstancedAnimationSystem/Built-in/Legacy Shaders/Diffuse");
                }
                else if (shaderName.Contains("Lit"))
                {
                    return Shader.Find("BlackRoseProjects/InstancedAnimationSystem/Built-in/Standard");
                }
                else
                    return Shader.Find("BlackRoseProjects/InstancedAnimationSystem/Built-in/Standard");
            }
            return Shader.Find("BlackRoseProjects/InstancedAnimationSystem/Built-in/Standard");
        }

        internal static bool HasDefinition(string definition)
        {
            switch (definition)
            {
                case BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES:
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
                    return true;
#else
                    return false;
#endif
                case BLACKROSE_INSTANCING_PROFILING:
#if BLACKROSE_INSTANCING_PROFILING
                    return true;
#else
                    return false;
#endif
                case BLACKROSE_INSTANCING_SAFETY_CHECKS:
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
                return true;
#else
                    return false;
#endif
                case BLACKROSE_INSTANCING_BURST:
#if BLACKROSE_INSTANCING_BURST
                    return true;
#else
                return false;
#endif
                case BLACKROSE_INSTANCING_COLLECTIONS:
#if BLACKROSE_INSTANCING_COLLECTIONS
                    return true;
#else
                return false;
#endif
                case BLACKROSE_INSTANCING_MATH:
#if BLACKROSE_INSTANCING_MATH
                    return true;
#else
                return false;
#endif
            }
            return false;
        }
        internal static void FixMaterialData(Material material, int bonePerVertex)
        {
            material.enableInstancing = true;
            material.EnableKeyword("USE_CONSTANT_BUFFER");
            material.DisableKeyword("USE_COMPUTE_BUFFER");
            switch (bonePerVertex)
            {
                case 2:
                    material.EnableKeyword("_BONE2");
                    material.DisableKeyword("_BONE3");
                    material.DisableKeyword("_BONE4");
                    break;
                case 3:
                    material.DisableKeyword("_BONE2");
                    material.EnableKeyword("_BONE3");
                    material.DisableKeyword("_BONE4");
                    break;
                case 4:
                    material.DisableKeyword("_BONE2");
                    material.DisableKeyword("_BONE3");
                    material.EnableKeyword("_BONE4");
                    break;
                default:
                    material.DisableKeyword("_BONE2");
                    material.DisableKeyword("_BONE3");
                    material.DisableKeyword("_BONE4");
                    break;
            }
        }
#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS

        internal static float3 Rotate(Matrix4x4 matrix, float3 point)
        {
            Quaternion rotation = matrix.rotation;
            float num = rotation.x * 2f;
            float num2 = rotation.y * 2f;
            float num3 = rotation.z * 2f;
            float num4 = rotation.x * num;
            float num5 = rotation.y * num2;
            float num6 = rotation.z * num3;
            float num7 = rotation.x * num2;
            float num8 = rotation.x * num3;
            float num9 = rotation.y * num3;
            float num10 = rotation.w * num;
            float num11 = rotation.w * num2;
            float num12 = rotation.w * num3;
            float3 result = default(float3);
            result.x = (1f - (num5 + num6)) * point.x + (num7 - num12) * point.y + (num8 + num11) * point.z;
            result.y = (num7 + num12) * point.x + (1f - (num4 + num6)) * point.y + (num9 - num10) * point.z;
            result.z = (num8 - num11) * point.x + (num9 + num10) * point.y + (1f - (num4 + num5)) * point.z;
            return result;
        }

        internal static int FindAnimationIndex(AnimationInfo[] aniInfo, string name)
        {
            if (aniInfo == null || string.IsNullOrEmpty(name))
                return -1;
            animationSearchInfo.animationNameHash = name.GetHashCode();
            return Array.BinarySearch(aniInfo, animationSearchInfo, animationComparer);
        }

        internal static unsafe void CopyToFast<T>(this NativeSlice<T> nativeSlice, T[] array) where T : struct
        {
            if (array == null)
                throw new NullReferenceException(nameof(array) + " is null");

            int nativeArrayLength = nativeSlice.Length;
            if (array.Length < nativeArrayLength)
            {
                throw new IndexOutOfRangeException(
                    nameof(array) + " is shorter than " + nameof(nativeSlice));
            }
            int byteLength = nativeSlice.Length * UnsafeUtility.SizeOf<T>();
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            void* nativeBuffer = nativeSlice.GetUnsafePtr();
            UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);
        }

        internal static unsafe void CopyToFast<T>(this NativeArray<T> nativeArray, T[] array, int elements) where T : struct
        {
            if (array == null)
                throw new NullReferenceException(nameof(array) + " is null");

            int nativeArrayLength = nativeArray.Length;
            if (elements > array.Length || elements > nativeArrayLength)
            {
                throw new IndexOutOfRangeException(
                    nameof(array) + " is shorter than " + nameof(nativeArray));
            }
            int byteLength = elements * UnsafeUtility.SizeOf<T>();
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);
        }

        internal static unsafe void CopyFromFast<T>(this NativeArray<T> nativeArray, T[] array, int elements) where T : struct
        {
            if (array == null)
                throw new NullReferenceException(nameof(array) + " is null");

            int nativeArrayLength = nativeArray.Length;
            if (nativeArrayLength < elements || array.Length < elements)
            {
                throw new IndexOutOfRangeException(
                    nameof(array) + " is shorter than " + nameof(nativeArray));
            }
            int byteLength = elements * UnsafeUtility.SizeOf<T>();
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(nativeBuffer, managedBuffer, byteLength);
        }
#endif

        internal static bool CheckIfIncludeInstanced(Material material)
        {
            string[] shared = material.shaderKeywords;
            LocalKeyword[] keywords = material.enabledKeywords;

            bool instancing = material.enableInstancing;
            bool bone = false;

            for (int i = 0; i < keywords.Length; ++i)
            {
                string name = keywords[i].name;
                switch (name)
                {
                    case "_BONE2":
                    case "_BONE3":
                    case "_BONE4":
                        bone = true;
                        break;
                }
            }
            if (instancing && bone)
                return true;

            material.EnableKeyword("_BONE2");
            LocalKeyword[] keywords2 = material.enabledKeywords;
            for (int i = 0; i < keywords2.Length; ++i)
            {
                string name = keywords2[i].name;
                switch (name)
                {
                    case "_BONE2":
                    case "_BONE3":
                    case "_BONE4":
                        bone = true;
                        break;
                }
            }
            material.enabledKeywords = keywords;
            material.shaderKeywords = shared;

            return instancing && bone;
        }


        private static int CompareTransform(Transform t1, Transform t2)
        {
            return t1.name.CompareTo(t2.name);
        }

        private static void SortBones(SkinnedMeshRenderer rend)
        {
            List<Transform> tList = new List<Transform>(rend.bones);

            tList.Sort(CompareTransform);
            Dictionary<int, int> remap = new Dictionary<int, int>();

            for (int i = 0; i < rend.bones.Length; i++)
                remap[i] = tList.IndexOf(rend.bones[i]);

            BoneWeight[] bw = rend.sharedMesh.boneWeights;
            for (int i = 0; i < bw.Length; i++)
            {
                bw[i].boneIndex0 = remap[bw[i].boneIndex0];
                bw[i].boneIndex1 = remap[bw[i].boneIndex1];
                bw[i].boneIndex2 = remap[bw[i].boneIndex2];
                bw[i].boneIndex3 = remap[bw[i].boneIndex3];
            }
            Matrix4x4[] bp = new Matrix4x4[rend.sharedMesh.bindposes.Length];
            for (int i = 0; i < bp.Length; i++)
                bp[remap[i]] = rend.sharedMesh.bindposes[i];

            rend.bones = tList.ToArray();
            rend.sharedMesh.boneWeights = bw;
            rend.sharedMesh.bindposes = bp;
        }

        public static Transform[] MergeBone(SkinnedMeshRenderer[] meshRender, List<Matrix4x4> bindPose, bool fixBones = false)
        {
            if (fixBones)
            {
                for (int i = 0; i != meshRender.Length; ++i)
                    SortBones(meshRender[i]);
            }

            List<Transform> listTransform = new List<Transform>(150);
            for (int i = 0; i != meshRender.Length; ++i)
            {
                if (meshRender[i].sharedMesh == null)
                    continue;
                Transform[] bones = meshRender[i].bones;
                Matrix4x4[] checkBindPose = meshRender[i].sharedMesh.bindposes;
                for (int j = 0; j != bones.Length; ++j)
                {
                    Transform bone = bones[j];
                    int index = listTransform.IndexOf(bone);
                    if (index < 0)
                    {
                        listTransform.Add(bone);
                        bindPose.Add(checkBindPose[j]);
                    }
                    else if (bindPose[index] != checkBindPose[j])
                        Debug.LogWarning("Bind pose difference for skinned mesh index: " + i + " and boneIndex: " + j + ". This might results in rig glitching in this renderer. Bone: " + meshRender[0].bones[j].name);
                }
            }
            return listTransform.ToArray();
        }

        public static Quaternion QuaternionFromMatrix(Matrix4x4 mat)
        {
            Vector3 forward;
            forward.x = mat.m02;
            forward.y = mat.m12;
            forward.z = mat.m22;

            Vector3 upwards;
            upwards.x = mat.m01;
            upwards.y = mat.m11;
            upwards.z = mat.m21;

            return Quaternion.LookRotation(forward, upwards);
        }

#if UNITY_EDITOR
        internal static void StringField(GUIContent content, string value, int labelSize = 150)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = false;
            EditorGUILayout.TextField(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        internal static void StringField(string content, string value, int labelSize = 150)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = false;
            EditorGUILayout.TextField(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        internal static bool ToggleField(GUIContent content, bool value, int labelSize = 150, bool isActive = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = isActive;
            value = EditorGUILayout.Toggle(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        internal static bool ToggleField(string content, bool value, int labelSize = 150, bool isActive = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = isActive;
            value = EditorGUILayout.Toggle(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        internal static Vector3 Vector3Field(GUIContent content, Vector4 value, int labelSize = 150, bool isActive = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = isActive;
            value = EditorGUILayout.Vector3Field("", value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        internal static int LayerField(GUIContent content, int value, int labelSize = 150, bool isActive = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = isActive;
            value = EditorGUILayout.LayerField(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        internal static Enum EnumField(GUIContent content, Enum value, int labelSize = 150, bool isActive = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = isActive;
            value = EditorGUILayout.EnumPopup(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        internal static float FloatField(GUIContent content, float value, int labelSize = 150, bool isActive = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = isActive;
            value = EditorGUILayout.FloatField(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            return value;
        }
        internal static float FloatField(string content, float value, int labelSize = 150, bool isActive = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = isActive;
            value = EditorGUILayout.FloatField(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        internal static float IntField(string content, int value, int labelSize = 150, bool isActive = false)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(content, GUILayout.Width(labelSize));
            GUI.enabled = isActive;
            value = EditorGUILayout.IntField(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            return value;
        }
#endif
    }
}
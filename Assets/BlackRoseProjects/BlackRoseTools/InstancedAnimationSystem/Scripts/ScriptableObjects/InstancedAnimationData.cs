#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// Store complex data about baked animations and structure of Meshses and Materials to render
    /// </summary>
#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile(FloatMode = Unity.Burst.FloatMode.Fast, FloatPrecision = Unity.Burst.FloatPrecision.Low, OptimizeFor = Unity.Burst.OptimizeFor.Performance)]
#endif
    [Icon("Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Icons/Icon_AssetAnimation.png")]
    [HelpURL("http://docs.blackrosetools.com/InstancedAnimations/html/_instanced_animation_data_inspector.html")]
    public class InstancedAnimationData : ScriptableObject
    {
        #region Nested Classes
        private struct VertexHeper
        {
            public float4 vert;
            public float3 upward;
            public float3 normal;
            public int boneIndex;

            public float frameIndex;
            public float preFrameIndex;
            public float transition;
        }

        private struct BoneDataHolder
        {
            public int _blockCount;
            public int blockHeight;
            public int blockWidth;
            public int _boneTextureWidth;
            public int _matCount;
            public int _boneTextureHeight;
        }

        private class BoneSyncData
        {
            public int _blockCount;
            public int _matCount;
            public int _boneTextureWidth;
            public int _boneTextureHeight;

            public float4[] colors;

            public BoneSyncData(InstancedAnimationData parent)
            {
                InstancedAnimationHelper.BeginSample("PrepareSyncBoneData()");
                _blockCount = parent.animationTexture.width / parent.blockWidth;
                _matCount = parent.blockWidth / 4;

                _boneTextureWidth = parent.animationTexture.width;
                _boneTextureHeight = parent.animationTexture.height;

                Color[] c = parent.animationTexture.GetPixels();
                colors = new float4[c.Length];
                for (int i = 0; i < colors.Length; ++i)
                {
                    Color v = c[i];
                    colors[i] = new float4(v.r, v.g, v.b, v.a);
                }
                InstancedAnimationHelper.EndSample();
            }

            public float4 GetTexture(int2 pos)
            {
                return colors[mod(pos.y, _boneTextureHeight) * _boneTextureWidth + mod(pos.x, _boneTextureWidth)];
            }
        }
        #endregion

        [SerializeField] internal AnimationInfo[] animations;

        [SerializeField] internal Texture2D animationTexture;
        [SerializeField] internal int textureWidth;
        [SerializeField] internal int textureHeight;
        [SerializeField] internal int blockWidth;
        [SerializeField] internal int blockHeight;
        [SerializeField] internal int bonePerVertex;
        [SerializeField] internal int[] bonesHashes;
        /// <summary>
        /// bind poses are used only for bone sync and for attachments 
        /// </summary>
        [SerializeField] internal Matrix4x4[] bindPoses;
#if UNITY_EDITOR
        [SerializeField] internal string[] bonesNames;//keep these only for editor mode to allow display of bone names in inspector
        [SerializeField] internal InstancedAnimationData parent;
        [SerializeField] internal List<InstancedAnimationData> variants;
#endif

        [SerializeField] internal InstancingLODData[] LOD;
        [SerializeField] internal float boundingSphereRadius;
        [SerializeField] internal float3 boundingSphereOffset;
        [SerializeField] internal float4 lodFloat;

        [SerializeField] internal List<CustomValueFloatHolder> customFloats;
        [SerializeField] internal List<CustomValueVectorHolder> customVectors;

        //runtime only
        [NonSerialized, HideInInspector] internal int _RuntimeTextureID = -1;
        [NonSerialized, HideInInspector] private sbyte isValid = 0;

        [NonSerialized, HideInInspector] internal CustomValueFloatHolder[][][] customFloatIndex;
        [NonSerialized, HideInInspector] internal CustomValueVectorHolder[][][] customVectorIndex;

        [NonSerialized, HideInInspector] private BoneSyncData boneSyncData;
        [NonSerialized, HideInInspector] private BoneDataHolder boneDataHolder;

        //this to work require enabled reload domain on play mode when using in editor
        internal void OnEnable()
        {
            _RuntimeTextureID = -1;
            isValid = 0;
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
                CheckInitValid();
        }
#if UNITY_EDITOR
        internal void CopyTo(InstancedAnimationData output, bool addAsVariant)
        {
            output.animations = new AnimationInfo[animations.Length];
            for (int i = 0; i < animations.Length; ++i)
                output.animations[i] = animations[i].Copy();
            output.LOD = new InstancingLODData[LOD.Length];
            for (int i = 0; i < LOD.Length; ++i)
                output.LOD[i] = LOD[i].Copy();

            output.bonesHashes = new int[bonesHashes.Length];
            bonesHashes.CopyTo(output.bonesHashes, 0);
            output.bonesNames = new string[bonesNames.Length];
            bonesNames.CopyTo(output.bonesNames, 0);
            output.bindPoses = new Matrix4x4[bindPoses.Length];
            bindPoses.CopyTo(output.bindPoses, 0);

            output.customFloats = new List<CustomValueFloatHolder>(customFloats);
            output.customVectors = new List<CustomValueVectorHolder>(customVectors);

            output.animationTexture = animationTexture;

            output.textureWidth = textureWidth;
            output.textureHeight = textureHeight;
            output.blockWidth = blockWidth;
            output.blockHeight = blockHeight;
            output.bonePerVertex = bonePerVertex;

            output.boundingSphereRadius = boundingSphereRadius;
            output.boundingSphereOffset = boundingSphereOffset;
            output.lodFloat = lodFloat;
            output.parent = this;
            if (addAsVariant)
            {
                if (variants == null)
                    variants = new List<InstancedAnimationData>();
                variants.Add(output);
            }
        }

        internal void UpdateVariantsOnBaking()
        {
            if (variants == null || variants.Count == 0)
                return;
            for (int i = 0; i < variants.Count; ++i)
            {
                InstancedAnimationData data = variants[i];
                if (data == null)
                {
                    variants.RemoveAt(i);
                    --i;
                    continue;
                }
                UnityEditor.EditorUtility.SetDirty(data);
                if (data.bonesNames.Length != bonesNames.Length)
                {
                    CopyTo(data, false);
                    Debug.LogWarning($"Baked Instanced Animation Data {name} contains variant that has different animation settings. Reseting variant data for variant {data.name}", data);
                }
                else
                {
                    data.textureWidth = textureWidth;
                    data.textureHeight = textureHeight;
                    data.blockWidth = blockWidth;
                    data.blockHeight = blockHeight;
                    data.bonePerVertex = bonePerVertex;

                    data.animations = new AnimationInfo[animations.Length];
                    for (int j = 0; j < animations.Length; ++j)
                        data.animations[j] = animations[j].Copy();
                }
            }
        }

        internal bool IsRelativeTo(InstancedAnimationData other)
        {
            if (this == other)
                return true;
            if (parent != null && other.parent != null)
                return parent == other.parent;
            if (parent != null && other.variants != null)
                return other.variants.Contains(this);
            if (other.parent != null && variants != null)
                return variants.Contains(other);
            return false;
        }
#endif

        internal void GenerateCustomFloat()
        {
            customFloats = new List<CustomValueFloatHolder>();
            for (int i = 0; i < LOD.Length; ++i)
            {//lod
                for (int j = 0; j < LOD[i].instancingMeshData.Length; ++j)
                {//mesh
                    for (int k = 0; k < LOD[i].instancingMeshData[j].floatCustomData.Length; ++k)
                    {//
                        CustomValueFloatHolder cvh = new CustomValueFloatHolder();
                        cvh.customIndex = customFloats.Count;
                        cvh.lodID = i;
                        cvh.meshID = j;
                        cvh.defaultFloat = LOD[i].instancingMeshData[j].floatCustomData[k].defaultValue;
                        cvh.submeshID = LOD[i].instancingMeshData[j].floatCustomData[k].submeshID;
                        cvh.shaderPropertyID = Shader.PropertyToID(LOD[i].instancingMeshData[j].floatCustomData[k].propertyName);
                        cvh.propertyName = LOD[i].instancingMeshData[j].floatCustomData[k].propertyName;
                        cvh.groupName = LOD[i].instancingMeshData[j].floatCustomData[k].groupName;
                        customFloats.Add(cvh);
                    }
                }
            }
        }

        private void GenerateCustomFloatIndex()
        {
            if (customFloats.Count == 0)
                return;
            customFloatIndex = new CustomValueFloatHolder[LOD.Length][][];
            for (int i = 0; i < customFloatIndex.Length; ++i)
            {
                int meshes = LOD[i].instancingMeshData.Length;
                customFloatIndex[i] = new CustomValueFloatHolder[meshes][];
                if (meshes == 0)
                    continue;


                for (int j = 0; j < customFloatIndex[i].Length; ++j)
                {
                    int customValues = GetCustomValuesForMesh(i, j, customFloats);
                    if (customValues == 0)
                        continue;

                    customFloatIndex[i][j] = new CustomValueFloatHolder[customValues];
                    int customIndex = 0;

                    for (int x = 0; x < customFloats.Count; ++x)
                    {
                        CustomValueFloatHolder cvh = customFloats[x];
                        if (cvh.lodID == i && cvh.meshID == j)
                        {
                            customFloatIndex[i][j][customIndex++] = cvh;
                        }
                    }
                }
            }
        }

        private int GetCustomValuesForMesh(int lod, int mesh, List<CustomValueFloatHolder> customList)
        {
            int count = 0;
            for (int x = 0; x < customList.Count; ++x)
            {
                CustomValueFloatHolder cvh = customList[x];
                if (cvh.lodID == lod && cvh.meshID == mesh)
                    count++;
            }
            return count;
        }
        internal void GenerateCustomVector()
        {
            customVectors = new List<CustomValueVectorHolder>();
            for (int i = 0; i < LOD.Length; ++i)
            {//lod
                for (int j = 0; j < LOD[i].instancingMeshData.Length; ++j)
                {//mesh
                    for (int k = 0; k < LOD[i].instancingMeshData[j].vectorCustomData.Length; ++k)
                    {//
                        CustomValueVectorHolder cvh = new CustomValueVectorHolder();
                        cvh.customIndex = customVectors.Count;
                        cvh.lodID = i;
                        cvh.meshID = j;
                        cvh.defaultVector = LOD[i].instancingMeshData[j].vectorCustomData[k].defaultValue;
                        cvh.submeshID = LOD[i].instancingMeshData[j].vectorCustomData[k].submeshID;
                        cvh.shaderPropertyID = Shader.PropertyToID(LOD[i].instancingMeshData[j].vectorCustomData[k].propertyName);
                        cvh.propertyName = LOD[i].instancingMeshData[j].vectorCustomData[k].propertyName;
                        cvh.groupName = LOD[i].instancingMeshData[j].vectorCustomData[k].groupName;
                        customVectors.Add(cvh);
                    }
                }
            }
        }

        private void GenerateCustomVectorIndex()
        {
            if (customVectors.Count == 0)
            {
                return;
            }
            customVectorIndex = new CustomValueVectorHolder[LOD.Length][][];
            for (int i = 0; i < customVectorIndex.Length; ++i)
            {
                int meshes = LOD[i].instancingMeshData.Length;
                customVectorIndex[i] = new CustomValueVectorHolder[meshes][];
                if (meshes == 0)
                    continue;


                for (int j = 0; j < customVectorIndex[i].Length; ++j)
                {
                    int customValues = GetCustomValuesForMesh_Vector(i, j, customVectors);
                    if (customValues == 0)
                        continue;

                    customVectorIndex[i][j] = new CustomValueVectorHolder[customValues];
                    int customIndex = 0;

                    for (int x = 0; x < customVectors.Count; ++x)
                    {
                        CustomValueVectorHolder cvh = customVectors[x];
                        if (cvh.lodID == i && cvh.meshID == j)
                        {
                            customVectorIndex[i][j][customIndex++] = cvh;
                        }
                    }
                }
            }
        }

        private int GetCustomValuesForMesh_Vector(int lod, int mesh, List<CustomValueVectorHolder> customList)
        {
            int count = 0;
            for (int x = 0; x < customList.Count; ++x)
            {
                CustomValueVectorHolder cvh = customList[x];
                if (cvh.lodID == lod && cvh.meshID == mesh)
                    count++;
            }
            return count;
        }

        internal bool CheckInitValid()
        {
            if (isValid == 0)
            {
                GenerateCustomFloatIndex();
                GenerateCustomVectorIndex();

                isValid = 1;
            }
            return isValid == 1;
        }

        /// <summary>
        /// Get group of custom values of given type. Result can be catched and used to set all custom values af same type without additional checks and allocations
        /// </summary>
        /// <param name="groupName">Name of group</param>
        /// <param name="groupType">Type of custom value</param>
        /// <returns>Group handle for given group and type. If no group match, group type is invalid</returns>
        public CustomValueGroupHandle GetCustomValueGroupHandle(string groupName, CustomValueGroupHandle.GroupType groupType)
        {
            switch (groupType)
            {
                case CustomValueGroupHandle.GroupType.Float:
                    {
                        if (customFloats == null)
                            return new CustomValueGroupHandle(groupName, CustomValueGroupHandle.GroupType.Invalid, new int[0]);
                        List<int> list = new List<int>();
                        for (int i = 0; i < customFloats.Count; ++i)
                        {
                            if (customFloats[i].groupName == groupName)
                                list.Add(customFloats[i].customIndex);
                        }
                        return new CustomValueGroupHandle(groupName, groupType, list.ToArray());
                    }
                case CustomValueGroupHandle.GroupType.Vector:
                    {
                        if (customVectors == null)
                            return new CustomValueGroupHandle(groupName, CustomValueGroupHandle.GroupType.Invalid, new int[0]);
                        List<int> list = new List<int>();
                        for (int i = 0; i < customVectors.Count; ++i)
                        {
                            if (customVectors[i].groupName == groupName)
                                list.Add(customVectors[i].customIndex);
                        }
                        return new CustomValueGroupHandle(groupName, groupType, list.ToArray());
                    }
            }
            return new CustomValueGroupHandle(groupName, CustomValueGroupHandle.GroupType.Invalid, new int[0]);
        }

        /// <summary>
        /// Get array of defined custom shader float values
        /// </summary>
        /// <returns>Return array of custom shader float values or null if no custom shader float values defined</returns>
        public CustomValueFloatHolder[] GetCustomShaderFloatValues()
        {
            return customFloats != null ? customFloats.ToArray() : null;
        }

        /// <summary>
        ///  Get array of defined custom shader vector values
        /// </summary>
        /// <returns>Return array of custom shader vector values or null if no custom shader vector values defined</returns>
        public CustomValueVectorHolder[] GetCustomShaderVectorValues()
        {
            return customVectors != null ? customVectors.ToArray() : null;
        }

        /// <summary>
        /// Get index of animation with given name
        /// </summary>
        /// <param name="animetionName">Name of animation</param>
        /// <returns>ID of animation or -1 if animation is not found</returns>
        public int FindAnimationIndex(string animetionName)
        {
            return InstancedAnimationHelper.FindAnimationIndex(animations, animetionName);
        }

        /// <summary>
        /// Get id of bone given by name
        /// </summary>
        /// <param name="boneName">Name of bone</param>
        /// <returns>ID of bone or -1 if bone not found</returns>
        public int BoneNameToId(string boneName)
        {
            int hashBone = boneName.GetHashCode();
            for (int i = 0; i < bonesHashes.Length; ++i)
                if (bonesHashes[i] == hashBone)
                    return i;
            return -1;
        }

        internal void TransformateBonePosition(int boneIndex, float frameIndex, float preFrameIndex, float transition, Transform toSync, ref Matrix4x4 myMatrix)
        {
            if (boneSyncData == null)
            {
                boneSyncData = new BoneSyncData(this);
                boneDataHolder = new BoneDataHolder()
                {
                    blockHeight = blockHeight,
                    blockWidth = blockWidth,
                    _blockCount = animationTexture.width / blockWidth,
                    _matCount = blockWidth / 4,
                    _boneTextureWidth = animationTexture.width,
                    _boneTextureHeight = animationTexture.height
                };
            }

            Matrix4x4 bindPose = bindPoses[boneIndex];

            VertexHeper vertex = new VertexHeper()
            {
                frameIndex = frameIndex,
                preFrameIndex = preFrameIndex,
                transition = transition,
                boneIndex = boneIndex,
                vert = bindPose.GetColumn(3),
                upward = new float3(bindPose.m01, bindPose.m11, bindPose.m21),
                normal = new float3(bindPose.m02, bindPose.m12, bindPose.m22),
            };

#if BLACKROSE_INSTANCING_BURST
            unsafe
            {
                fixed (float4* color = &boneSyncData.colors[0])
                {
                    TransformateBurst(ref vertex, in boneDataHolder, in color);
                }
            }
#else
            Transformate(ref vertex);
#endif
            toSync.SetPositionAndRotation(myMatrix.MultiplyPoint(new Vector3(vertex.vert.x, vertex.vert.y, vertex.vert.z)),
                myMatrix.rotation * Quaternion.LookRotation(vertex.normal, vertex.upward));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int mod(int x, int mod)
        {
            int v = x % mod;
            return v >= 0 ? v : v + mod;
        }

#if BLACKROSE_INSTANCING_BURST
        [Unity.Burst.BurstCompile(FloatMode = Unity.Burst.FloatMode.Fast, FloatPrecision = Unity.Burst.FloatPrecision.Low, OptimizeFor = Unity.Burst.OptimizeFor.Performance), Unity.Burst.CompilerServices.SkipLocalsInit]
        // [Unity.Burst.BurstCompile, Unity.Burst.CompilerServices.SkipLocalsInit]
        static unsafe void TransformateBurst(ref VertexHeper v, in BoneDataHolder boneHolder, in float4* color)
        {
            float curFrame = v.frameIndex;
            float progress = v.transition;
            int preFrame = (int)curFrame;
            int nextFrame = (int)(curFrame + 1.0f);
            GetTransformMatrixBurst(in preFrame, in v.boneIndex, in boneHolder, in color, out float4x4 localToWorldMatrixPre);
            GetTransformMatrixBurst(in nextFrame, in v.boneIndex, in boneHolder, in color, out float4x4 localToWorldMatrixNext);

            float4 localPosPre = math.mul(v.vert, localToWorldMatrixPre);
            float4 localPosNext = math.mul(v.vert, localToWorldMatrixNext);
            float4 localPos = math.lerp(localPosPre, localPosNext, curFrame - preFrame);

            float3x3 localToWorldMatrixPre3x3 = (float3x3)localToWorldMatrixPre;
            float3x3 localToWorldMatrixNext3x3 = (float3x3)localToWorldMatrixNext;

            float3 localNormPre = math.mul(v.normal, localToWorldMatrixPre3x3);
            float3 localNormNext = math.mul(v.normal, localToWorldMatrixNext3x3);
            float3 localTanPre = math.mul(v.upward, localToWorldMatrixPre3x3);
            float3 localTanNext = math.mul(v.upward, localToWorldMatrixNext3x3);

            if (progress < 1f)
            {
                //for animation blending in crossFade
                float preAniFrame = v.preFrameIndex;
                int preFrame_ = (int)preAniFrame;
                int nextFrame_ = (int)(preAniFrame + 1.0f);
                GetTransformMatrixBurst(in preFrame_, in v.boneIndex, in boneHolder, in color, out float4x4 localToWorldMatrixPreAni);
                GetTransformMatrixBurst(in nextFrame_, in v.boneIndex, in boneHolder, in color, out float4x4 localToWorldMatrixPreAniNext);
                float4 localPosPreAni = math.mul(v.vert, localToWorldMatrixPreAni);
                float4 localPosPreAniNext = math.mul(v.vert, localToWorldMatrixPreAniNext);
                float4 localPrePos = math.lerp(localPosPreAni, localPosPreAniNext, preAniFrame - preFrame_);
                localPos = math.lerp(localPos, localPrePos, (1.0f - progress));

                float3 localNormPreAni = math.mul(v.normal, (float3x3)localToWorldMatrixPreAni);
                float3 localNormNextAni = math.mul(v.normal, (float3x3)localToWorldMatrixPreAniNext);
                float3 localTanPreAni = math.mul(v.upward, (float3x3)localToWorldMatrixPreAni);
                float3 localTanNextAni = math.mul(v.upward, (float3x3)localToWorldMatrixPreAniNext);
                v.normal = math.normalize(math.lerp(math.lerp(localNormPre, localNormNext, curFrame - preFrame), math.lerp(localNormPreAni, localNormNextAni, preAniFrame - preFrame_), (1.0f - progress)));
                v.upward = math.normalize(math.lerp(math.lerp(localTanPre, localTanNext, curFrame - preFrame), math.lerp(localTanPreAni, localTanNextAni, preAniFrame - preFrame_), (1.0f - progress)));

            }
            else
            {
                v.normal = math.normalize(math.lerp(localNormPre, localNormNext, curFrame - preFrame));
                v.upward = math.normalize(math.lerp(localTanPre, localTanNext, curFrame - preFrame));
            }
            v.vert = localPos;
        }

        [Unity.Burst.BurstCompile(FloatMode = Unity.Burst.FloatMode.Fast, FloatPrecision = Unity.Burst.FloatPrecision.Low, OptimizeFor = Unity.Burst.OptimizeFor.Performance), Unity.Burst.CompilerServices.SkipLocalsInit]
        //[Unity.Burst.BurstCompile, Unity.Burst.CompilerServices.SkipLocalsInit]
        static unsafe void GetTransformMatrixBurst(in int frameIndex, in int boneIndex, in BoneDataHolder boneData, in float4* colors, out float4x4 result)
        {
            int2 uv;
            uv.y = frameIndex / boneData._blockCount * boneData.blockHeight;
            uv.x = boneData.blockWidth * (frameIndex - boneData._boneTextureWidth / boneData.blockWidth * uv.y);
            uv.x += (boneIndex % boneData._matCount) * 4;
            uv.y += boneIndex / boneData._matCount;
            GetColor(in uv, in boneData._boneTextureHeight, in boneData._boneTextureWidth, in colors, out float4 c1);
            uv.x += 1;
            GetColor(in uv, in boneData._boneTextureHeight, in boneData._boneTextureWidth, in colors, out float4 c2);
            uv.x += 1;
            GetColor(in uv, in boneData._boneTextureHeight, in boneData._boneTextureWidth, in colors, out float4 c3);
            result = new float4x4(c1, c2, c3, new float4(0, 0, 0, 1));
        }

        [Unity.Burst.BurstCompile, Unity.Burst.CompilerServices.SkipLocalsInit]
        static unsafe void GetColor(in int2 uv, in int _boneTextureHeight, in int _boneTextureWidth, in float4* colors, out float4 result)
        {
            result = colors[mod(uv.y, _boneTextureHeight) * _boneTextureWidth + mod(uv.x, _boneTextureWidth)];
        }
#else

        //color is: 1f, -0.1f, -0.1f, -0.1f
        //vertex is: boneIndex,0,0,0
        float4x4 GetTransformMatrix(int frameIndex, int boneIndex)
        {
            int2 uv;
            uv.y = frameIndex / boneSyncData._blockCount * blockHeight;
            uv.x = blockWidth * (frameIndex - boneSyncData._boneTextureWidth / blockWidth * uv.y);
            uv.x += (boneIndex % boneSyncData._matCount) * 4;
            uv.y += boneIndex / boneSyncData._matCount;
            float4 c1 = boneSyncData.GetTexture(uv);
            uv.x += 1;
            float4 c2 = boneSyncData.GetTexture(uv);
            uv.x += 1;
            float4 c3 = boneSyncData.GetTexture(uv);
            return new float4x4(c1, c2, c3, new float4(0, 0, 0, 1));
        }


        //color is: 1f, -0.1f, -0.1f, -0.1f
        //vertex is: boneIndex,0,0,0
        void Transformate(ref VertexHeper v)
        {
            float curFrame = v.frameIndex;
            float progress = v.transition;
            int preFrame = (int)curFrame;
            int nextFrame = (int)(curFrame + 1.0f);
            float4x4 localToWorldMatrixPre = GetTransformMatrix(preFrame, v.boneIndex);
            float4x4 localToWorldMatrixNext = GetTransformMatrix(nextFrame, v.boneIndex);

            float4 localPosPre = math.mul(v.vert, localToWorldMatrixPre);
            float4 localPosNext = math.mul(v.vert, localToWorldMatrixNext);
            float4 localPos = math.lerp(localPosPre, localPosNext, curFrame - preFrame);

            float3x3 localToWorldMatrixPre3x3 = (float3x3)localToWorldMatrixPre;
            float3x3 localToWorldMatrixNext3x3 = (float3x3)localToWorldMatrixNext;

            float3 localNormPre = math.mul(v.normal, localToWorldMatrixPre3x3);
            float3 localNormNext = math.mul(v.normal, localToWorldMatrixNext3x3);
            float3 localTanPre = math.mul(v.upward, localToWorldMatrixPre3x3);
            float3 localTanNext = math.mul(v.upward, localToWorldMatrixNext3x3);

            if (progress < 1f)
            {
                //for animation blending in crossFade
                float preAniFrame = v.preFrameIndex;
                int preFrame_ = (int)preAniFrame;
                int nextFrame_ = (int)(preAniFrame + 1.0f);
                float4x4 localToWorldMatrixPreAni = GetTransformMatrix(preFrame_, v.boneIndex);
                float4x4 localToWorldMatrixPreAniNext = GetTransformMatrix(nextFrame_, v.boneIndex);
                float4 localPosPreAni = math.mul(v.vert, localToWorldMatrixPreAni);
                float4 localPosPreAniNext = math.mul(v.vert, localToWorldMatrixPreAniNext);
                float4 localPrePos = math.lerp(localPosPreAni, localPosPreAniNext, preAniFrame - preFrame_);

                localPos = math.lerp(localPos, localPrePos, (1.0f - progress));

                float3 localNormPreAni = math.mul(v.normal, (float3x3)localToWorldMatrixPreAni);
                float3 localNormNextAni = math.mul(v.normal, (float3x3)localToWorldMatrixPreAniNext);
                float3 localTanPreAni = math.mul(v.upward, (float3x3)localToWorldMatrixPreAni);
                float3 localTanNextAni = math.mul(v.upward, (float3x3)localToWorldMatrixPreAniNext);
                v.normal = math.normalize(math.lerp(math.lerp(localNormPre, localNormNext, curFrame - preFrame), math.lerp(localNormPreAni, localNormNextAni, preAniFrame - preFrame_), (1.0f - progress)));
                v.upward = math.normalize(math.lerp(math.lerp(localTanPre, localTanNext, curFrame - preFrame), math.lerp(localTanPreAni, localTanNextAni, preAniFrame - preFrame_), (1.0f - progress)));
            }
            else
            {
                v.normal = math.normalize(math.lerp(localNormPre, localNormNext, curFrame - preFrame));
                v.upward = math.normalize(math.lerp(localTanPre, localTanNext, curFrame - preFrame));
            }

            v.vert = localPos;
        }
#endif
    }

    [Serializable]
    internal class InstancingLODData
    {
        [SerializeField] internal float height;
        [SerializeField] internal InstancingMeshData[] instancingMeshData;
        [SerializeField] internal ShadowCastingMode[] shadowsMode;
        [SerializeField] internal bool[] receiveShadows;
        [SerializeField] internal int[] layer;

        [NonSerialized] internal InstancedAnimationManager.VertexCache[] vertexCacheList;
        [NonSerialized] internal InstancedAnimationManager.MaterialBlock[] materialBlockList;
#if UNITY_EDITOR
        internal InstancingLODData Copy()
        {
            InstancingLODData copy = new InstancingLODData();
            copy.height = height;
            copy.instancingMeshData = new InstancingMeshData[instancingMeshData.Length];
            copy.shadowsMode = new ShadowCastingMode[shadowsMode.Length];
            copy.receiveShadows = new bool[receiveShadows.Length];
            copy.layer = new int[layer.Length];

            for (int i = 0; i < instancingMeshData.Length; ++i)
                copy.instancingMeshData[i] = instancingMeshData[i].Copy();

            shadowsMode.CopyTo(copy.shadowsMode, 0);
            receiveShadows.CopyTo(copy.receiveShadows, 0);
            layer.CopyTo(copy.layer, 0);
            return copy;
        }
#endif
    }

    [Serializable]
    internal class InstancingCustomFloatData
    {
        public string propertyName;
        public string groupName;
        public int submeshID;
        public float defaultValue;

        internal InstancingCustomFloatData Copy()
        {
            InstancingCustomFloatData copy = new InstancingCustomFloatData();
            copy.propertyName = propertyName;
            copy.groupName = groupName;
            copy.submeshID = submeshID;
            copy.defaultValue = defaultValue;
            return copy;
        }
    }

    [Serializable]
    internal class InstancingCustomVector4Data
    {
        public string propertyName;
        public string groupName;
        public int submeshID;
        public Vector4 defaultValue;

        internal InstancingCustomVector4Data Copy()
        {
            InstancingCustomVector4Data copy = new InstancingCustomVector4Data();
            copy.propertyName = propertyName;
            copy.groupName = groupName;
            copy.submeshID = submeshID;
            copy.defaultValue = defaultValue;
            return copy;
        }
    }


    [Serializable]
    internal class InstancingMeshData
    {
        [SerializeField] internal Mesh mesh;
        [SerializeField] internal Material[] fixedMaterials;
        [SerializeField] internal InstancingCustomFloatData[] floatCustomData;
        [SerializeField] internal InstancingCustomVector4Data[] vectorCustomData;

#if UNITY_EDITOR
        [SerializeField] internal Material[] originalMaterials;

        internal InstancingMeshData Copy()
        {
            InstancingMeshData copy = new InstancingMeshData();
            copy.mesh = mesh;
            copy.fixedMaterials = new Material[fixedMaterials.Length];
            copy.floatCustomData = new InstancingCustomFloatData[floatCustomData.Length];
            copy.vectorCustomData = new InstancingCustomVector4Data[vectorCustomData.Length];
            copy.originalMaterials = new Material[originalMaterials.Length];

            for (int i = 0; i < floatCustomData.Length; ++i)
                copy.floatCustomData[i] = floatCustomData[i].Copy();
            for (int i = 0; i < vectorCustomData.Length; ++i)
                copy.vectorCustomData[i] = vectorCustomData[i].Copy();

            fixedMaterials.CopyTo(copy.fixedMaterials, 0);
            originalMaterials.CopyTo(copy.originalMaterials, 0);

            return copy;
        }
#endif
    }

    [Serializable]
    internal class AnimationEvent
    {
        [SerializeField] internal string function;
        [SerializeField] internal int intParameter;
        [SerializeField] internal float floatParameter;
        [SerializeField] internal string stringParameter;
        [SerializeField] internal string objectParameter;
        [SerializeField] internal float time;

        internal AnimationEvent Copy()
        {
            AnimationEvent copy = new AnimationEvent();
            copy.function = function;
            copy.intParameter = intParameter;
            copy.floatParameter = floatParameter;
            copy.stringParameter = stringParameter;
            copy.objectParameter = objectParameter;
            copy.time = time;
            return copy;
        }
    }

    [Serializable]
    internal class AnimationInfo
    {
        [SerializeField] internal string animationName;
        [SerializeField] internal int animationNameHash;
        [SerializeField] internal int totalFrame;
        [SerializeField] internal int fps;
        [SerializeField] internal int animationIndex;
        [SerializeField] internal bool rootMotion;
        [SerializeField] internal WrapMode wrapMode;
        [SerializeField] internal Vector3[] velocity;
        [SerializeField] internal Vector3[] angularVelocity;
        [SerializeField] internal AnimationEvent[] eventList;

        public override string ToString()
        {
            return $"Name: {animationName}, fps: {fps}, velocityCount: {velocity.Length}";
        }

        internal static void Sort(List<AnimationInfo> animations)
        {
            animations.Sort(new AnimationComparer());
        }

        internal AnimationInfo Copy()
        {
            AnimationInfo copy = new AnimationInfo();
            copy.animationName = animationName;
            copy.animationNameHash = animationNameHash;
            copy.totalFrame = totalFrame;
            copy.fps = fps;
            copy.animationIndex = animationIndex;
            copy.rootMotion = rootMotion;
            copy.rootMotion = rootMotion;
            copy.wrapMode = wrapMode;
            if (velocity != null)
            {
                copy.velocity = new Vector3[velocity.Length];
                velocity.CopyTo(copy.velocity, 0);
            }
            else
                copy.velocity = null;
            if (angularVelocity != null)
            {
                copy.angularVelocity = new Vector3[angularVelocity.Length];
                angularVelocity.CopyTo(copy.angularVelocity, 0);
            }
            else
                copy.angularVelocity = null;
            copy.eventList = new AnimationEvent[eventList.Length];
            for (int i = 0; i < eventList.Length; ++i)
                copy.eventList[i] = eventList[i].Copy();
            return copy;
        }
    }

    internal class AnimationComparer : IComparer<AnimationInfo>
    {
        public int Compare(AnimationInfo x, AnimationInfo y)
        {
            return x.animationNameHash.CompareTo(y.animationNameHash);
        }
    }

    #region Custom Shader Values
    /// <summary>
    /// Handle that holds ID's for given custom shader group. Group can be reused for any instance of AnimationData it was get from
    /// </summary>
    public struct CustomValueGroupHandle
    {
        /// <summary>
        /// Type of group.
        /// </summary>
        [Serializable]
        public enum GroupType
        {
            /// <summary>
            /// Invalid group, that didn't match any valid name and will not affect any values
            /// </summary>
            Invalid,
            /// <summary>
            /// Group for custom float value
            /// </summary>
            Float,
            /// <summary>
            /// Group for custom Vector4 value
            /// </summary>
            Vector
        }

        internal string groupName;
        internal int[] indexes;
        internal GroupType groupType;

        internal CustomValueGroupHandle(string groupName, GroupType groupType, int[] indexes)
        {
            this.groupName = groupName;
            this.groupType = groupType;
            this.indexes = indexes;
        }

        /// <summary>
        /// Get group name
        /// </summary>
        public string GroupName { get { return groupName; } }
        /// <summary>
        /// get group type
        /// </summary>
        public GroupType Type { get { return groupType; } }
        /// <summary>
        /// Get number of registered properties that will be modified by this group
        /// </summary>
        public int AffectedValues { get { return indexes.Length; } }
    }

    /// <summary>
    /// Custom shader value holder stores data about float property for AnimationData model
    /// </summary>
    [Serializable]
    public struct CustomValueFloatHolder
    {
        [SerializeField] internal int lodID;
        [SerializeField] internal int meshID;
        [SerializeField] internal int submeshID;
        [SerializeField] internal int customIndex;
        [SerializeField] internal float defaultFloat;
        [SerializeField] internal int shaderPropertyID;
        [SerializeField] internal string propertyName;
        [SerializeField] internal string groupName;

        /// <summary>
        /// Index of LOD this holder is binded
        /// </summary>
        public int LodIndex { get { return lodID; } }

        /// <summary>
        /// Index of mesh in given LOD this holder is binded
        /// </summary>
        public int MeshIndex { get { return meshID; } }

        /// <summary>
        /// Index of submesh for given mesh this holder is binded
        /// </summary>
        public int SubmeshIndex { get { return submeshID; } }

        /// <summary>
        /// Maped index of this holder. Use for changing shader custom value by InstancedRendere
        /// </summary>
        public int IdentifierIndex { get { return customIndex; } }

        /// <summary>
        /// Name of shader property value this holder is changing
        /// </summary>
        public string ShaderProperty { get { return propertyName; } }

        /// <summary>
        /// Name of group this holder is a member
        /// </summary>
        public string GroupName { get { return groupName; } }

        /// <summary>
        /// Get default value for this holder
        /// </summary>
        public float DefaultValue { get { return defaultFloat; } }
    }

    /// <summary>
    /// Custom shader value holder stores data about Vector property for AnimationData model
    /// </summary>
    [Serializable]
    public struct CustomValueVectorHolder
    {
        [SerializeField] internal int lodID;
        [SerializeField] internal int meshID;
        [SerializeField] internal int submeshID;
        [SerializeField] internal int customIndex;
        [SerializeField] internal Vector4 defaultVector;
        [SerializeField] internal int shaderPropertyID;
        [SerializeField] internal string propertyName;
        [SerializeField] internal string groupName;

        /// <summary>
        /// Index of LOD this holder is binded
        /// </summary>
        public int LodIndex { get { return lodID; } }

        /// <summary>
        /// Index of mesh in given LOD this holder is binded
        /// </summary>
        public int MeshIndex { get { return meshID; } }

        /// <summary>
        /// Index of submesh for given mesh this holder is binded
        /// </summary>
        public int SubmeshIndex { get { return submeshID; } }

        /// <summary>
        /// Maped index of this holder. Use for changing shader custom value by InstancedRendere
        /// </summary>
        public int IdentifierIndex { get { return customIndex; } }

        /// <summary>
        /// Name of shader property value this holder is changing
        /// </summary>
        public string ShaderProperty { get { return propertyName; } }

        /// <summary>
        /// Name of group this holder is a member
        /// </summary>
        public string GroupName { get { return groupName; } }

        /// <summary>
        /// Get default value for this holder
        /// </summary>
        public Vector4 DefaultValue { get { return defaultVector; } }
    }
    #endregion
}
#endif
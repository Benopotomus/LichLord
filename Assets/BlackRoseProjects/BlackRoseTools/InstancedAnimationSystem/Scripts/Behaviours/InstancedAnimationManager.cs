#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;
using BlackRoseProjects.InstancedAnimationSystem.Exceptions;

/// <summary>
/// Instancing Animation System is tool to convert and draw multiple Skinned Mesh Renderers as Instanced Animation Renderers
/// </summary>
namespace BlackRoseProjects.InstancedAnimationSystem
{
    [AddComponentMenu("", 0)]
    internal sealed class InstancedAnimationManager : MonoBehaviour
    {
        #region nested classes

        internal class CustomFloatData
        {
            public int shaderID;
            public List<float[]> data;

            public CustomFloatData(int shaderID)
            {
                this.shaderID = shaderID;
                this.data = new List<float[]>();
            }
        }

        internal class CustomVectorData
        {
            public int shaderID;
            public List<Vector4[]> data;

            public CustomVectorData(int shaderID)
            {
                this.shaderID = shaderID;
                this.data = new List<Vector4[]>();
            }
        }

        internal class InstanceData
        {
            public List<Matrix4x4[]> worldMatrix;
            public List<float[]> frameIndex;
            public List<float[]> preFrameIndex;
            public List<float[]> transitionProgress;
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
            public List<CustomFloatData>[] customFloatData;//array - submesh index, list of custom floats for this submesh
            public List<CustomVectorData>[] customVectorData;//array - submesh index, list of custom floats for this submesh
#endif
            public InstanceData()
            {
                worldMatrix = new List<Matrix4x4[]>();
                frameIndex = new List<float[]>();
                preFrameIndex = new List<float[]>();
                transitionProgress = new List<float[]>();
            }
        }

        internal class InstancingPackage
        {
            public Material[] material;
            public ushort subMeshCount = 1;
            public ushort instancingCount;
            public MaterialPropertyBlock propertyBlock;
        }

        internal class MaterialBlock
        {
            public InstanceData instanceData;
            public int runtimePackageIndex;
            public bool hasInstancedMaterials;
            public List<InstancingPackage> packageList;

            internal void Clear()
            {
                if (hasInstancedMaterials)
                    for (int j = 0; j < packageList.Count; ++j)
                        for (int k = 0; k < packageList[j].material.Length; ++k)
                            Destroy(packageList[j].material[k]);
            }
        }

        internal class VertexCache
        {
            public int nameCode;
            internal Mesh mesh = null;
            internal bool isMeshInstance;
            internal Dictionary<int, MaterialBlock> instanceBlockList;

            internal Material[] materials = null;

            internal int boneTextureIndex = -1;
            internal int bonePerVertex;

            internal ShadowCastingMode shadowcastingMode;
            internal bool receiveShadow;
            internal int layer;
            #region EditorOnly
#if UNITY_EDITOR
            internal Vector4[] weight;
            internal Vector4[] boneIndex;
            internal Matrix4x4[] bindPose;
            internal Transform[] bonePose;
            internal int meshID;
            internal InstancingLODData lodData;
#endif
            #endregion

            internal void Clear()
            {
                if (isMeshInstance)
                    Destroy(mesh);
                foreach (KeyValuePair<int, MaterialBlock> obj in instanceBlockList)
                    obj.Value.Clear();
            }
        }

        internal class AnimationTexture
        {
            public readonly string name;
            public readonly Texture2D boneTexture;
            public readonly int blockWidth;
            public readonly int blockHeight;

            public AnimationTexture(string name, Texture2D boneTexture, int blockWidth, int blockHeight)
            {
                this.name = name;
                this.boneTexture = boneTexture;
                this.blockWidth = blockWidth;
                this.blockHeight = blockHeight;
            }
        }

        #endregion

        #region variables
        internal static InstancedAnimationManager instance;
        internal static bool isApplicationAlive = true;

        //shader properties catche
        internal int shader_frameIndex;
        internal int shader_preFrameIndex;
        internal int shader_transitionProgress;
        internal int shader__boneTexture;
        internal int shader__boneTextureWidth;
        internal int shader__boneTextureHeight;
        internal int shader__boneTextureBlockWidth;
        internal int shader__boneTextureBlockHeight;
        internal int shader__blockCount;
        internal int shader__matCount;

        private Camera currentCamera;
        internal bool renderOnlyToCurrentCamera;
        private Transform cameraTransform;
        internal List<InstancedRenderer> instancedRenderersList;
        private Dictionary<int, VertexCache> vertexCachePool;
        private Dictionary<int, RuntimeInstancingSharedAttachment> attachments;
        private readonly List<AnimationTexture> animationTextureList = new List<AnimationTexture>();

        private int instancingPackageSize;
        internal bool useInstancing = true;
        private InstancedAnimationSystemSettings settings;

        //job system
        internal static AnimationTmpHolder smartHolder;
        private float4[] frustumNormal;
        private Plane[] frustumPlanes;

        private int maxSize;
        private bool nativeInit;
        internal NativeArray<float> sizes_native;
        internal NativeArray<float3> offsets_native;
        internal NativeArray<float4> LodHeights_native;
        internal NativeArray<Matrix4x4> localToWorldMatrix_native;
        internal NativeArray<Matrix4x4> staticMatrices_native;
        internal NativeArray<byte> visable_native;

        internal Matrix4x4[] localToWorldMatrix;
        private byte[] visable;

        internal NativeArray<BaseAnimData> baseAnimData_native;
        internal NativeArray<TransitionAnimData> transitionAnimData_native;
        internal NativeArray<StandardAnimData> standardAnimData_native;

        private TransformAccessArray transformArray_native;

        #endregion

        #region public properties
        /// <summary>
        /// Switch current camera for Instanced rendering. Camera determinate culling and calculating LOD
        /// </summary>
        public Camera CurrentCamera
        {
            get { return currentCamera; }
            set
            {
                if (value == null)
                {
                    Debug.LogError("Instanced Rendering disabled cause of not set Current Camera!");
                    currentCamera = null;
                    cameraTransform = null;
                    return;
                }
                currentCamera = value;
                cameraTransform = currentCamera != null ? currentCamera.transform : null;
            }
        }

        /// <summary>
        /// Get instance of Animation Instancing Manager
        /// </summary>
        public static InstancedAnimationManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;
                else if (isApplicationAlive)
                {
                    instance = FindObjectOfType<InstancedAnimationManager>();
                    if (instance == null)
                    {
                        instance = new GameObject(nameof(InstancedAnimationManager)).AddComponent<InstancedAnimationManager>();
                        instance.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    }
                    return instance;
                }
                else
                    return null;
            }
        }

        /// <summary>
        /// Has instancing manager been destroyed
        /// </summary>
        public static bool IsDestroyed
        {
            get { return instance == null; }
        }

        #endregion

        #region public methods
        /// <summary>
        /// Clear all buffers
        /// </summary>
        public void Clear()
        {
            ReleaseBuffer();
            instancedRenderersList.Clear();
            attachments.Clear();
        }

        public InstancedRenderer CreateInstancedRendererInstance(InstancedAnimationData animData, int defaultAnim, bool applyRotMotion, InstancingCullingMode cullingMode)
        {
            if (animData == null)
                throw new ArgumentNullException("animData");
            if (!useInstancing)
                throw new InstancingNotEnabledException("Instancing has not been enabled! In order to use InstancedRenderer InstancedRenderingManager.UseInstancing must be enabled");

            InstancedRenderer renderer = new InstancedRenderer(animData, null, defaultAnim, applyRotMotion, cullingMode);

            renderer.Initialize(null);
            return renderer;
        }

        public InstancedRenderer CreateInstancedRendererInstance(InstancedAnimationData animData, InstancedAnimatorData bakedAnimator, bool applyRotMotion, InstancingCullingMode cullingMode)
        {
            if (animData == null)
                throw new ArgumentNullException("animData");
            if (bakedAnimator == null)
                throw new ArgumentNullException("bakedAnimator");
            if (!useInstancing)
                throw new InstancingNotEnabledException("Instancing has not been enabled! In order to use InstancedRenderer InstancedRenderingManager.UseInstancing must be enabled");

            InstancedRenderer renderer = new InstancedRenderer(animData, bakedAnimator, 0, applyRotMotion, cullingMode);
            renderer.Initialize(null);
            return renderer;
        }

        #endregion

        #region Unity calls

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ExecuteAtInit()
        {
            InstancedAnimationSystemSettings settings = InstancedAnimationSystemSettings.GetSettings();
            GlobalKeyword keyword = GlobalKeyword.Create(InstancedAnimationHelper.INSTANCING_NORMAL_TRANSITION_BLENDING);
            if (settings.transitionsBlending)
                Shader.EnableKeyword(keyword);
            else
                Shader.DisableKeyword(keyword);
            if (settings.initAtStartup)
                Instance.ToString();
        }

        private void Awake()
        {
            isApplicationAlive = true;//unity calls awake only when is alive
            if (instance != null && this != instance)
            {//try create new instance
                Destroy(this);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

#if UNITY_EDITOR
            if (UnityEditor.EditorSettings.enterPlayModeOptionsEnabled && (UnityEditor.EditorSettings.enterPlayModeOptions & UnityEditor.EnterPlayModeOptions.DisableDomainReload) != 0)
            {//force refresh of all scriptable InstancedAnimationData to refresh their ID's 
                EditorOnly_ResetScriptableInstanceID();
            }
#endif

            settings = InstancedAnimationSystemSettings.GetSettings();

            instancingPackageSize = settings.instancingPackageSize;
            renderOnlyToCurrentCamera = settings.renderOnlyToCurrentCamera;

            shader_frameIndex = Shader.PropertyToID("frameIndex");
            shader_preFrameIndex = Shader.PropertyToID("preFrameIndex");
            shader_transitionProgress = Shader.PropertyToID("transitionProgress");

            shader__boneTexture = Shader.PropertyToID("_boneTexture");
            shader__boneTextureBlockHeight = Shader.PropertyToID("_boneTextureBlockHeight");
            shader__boneTextureBlockWidth = Shader.PropertyToID("_boneTextureBlockWidth");
            shader__boneTextureHeight = Shader.PropertyToID("_boneTextureHeight");
            shader__boneTextureWidth = Shader.PropertyToID("_boneTextureWidth");
            shader__blockCount = Shader.PropertyToID("_blockCount");
            shader__matCount = Shader.PropertyToID("_matCount");

            vertexCachePool = new Dictionary<int, VertexCache>();
            attachments = new Dictionary<int, RuntimeInstancingSharedAttachment>();

            CurrentCamera = Camera.main;

            InitNative(settings.maxInstancedObjects);
            frustumPlanes = new Plane[6];
            frustumNormal = new float4[6];
        }

        private void OnEnable()
        {
            instancedRenderersList = new List<InstancedRenderer>(1000);
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
            {
                Debug.LogError("Instancing not avaible on this platform!");
                instancingPackageSize = 1;
                useInstancing = false;
                enabled = false;
            }
        }

        private void OnDisable()
        {
            ReleaseBuffer();
        }

        private void Update()
        {
            InternalUpdate();
        }

        private void OnApplicationQuit()
        {
            isApplicationAlive = false;
        }

        private void OnDestroy()
        {
            isApplicationAlive = false;
            for (int i = 0; i < instancedRenderersList.Count; ++i)
                instancedRenderersList[i].Destroy();

            DisposeNative();
        }

        //#if !UNITY_ANDROID && !UNITY_IPHONE
        //        private void OnApplicationFocus(bool focus)
        //        {
        //            if (focus)
        //            {
        //                RefreshMaterial();
        //            }
        //        }
        //#endif

        #endregion

        #region private custom updates
        internal void InternalUpdate()
        {
            int aliveInstances = instancedRenderersList.Count;
            if (aliveInstances == 0)
                return;
            if (currentCamera == null && Camera.main != null)
                CurrentCamera = Camera.main;

            InstancedAnimationHelper.BeginSample("UpdateInstances()");
            UpdateInstances(aliveInstances);
            InstancedAnimationHelper.EndSample();

            InstancedAnimationHelper.BeginSample("Render()");
            UpdateRender();
            InstancedAnimationHelper.EndSample();
        }

        internal void UpdateInstances(int aliveInstances)
        {
            InstancedAnimationHelper.BeginSample("Prepare Jobs");
            Vector3 cameraPosition = cameraTransform.position;
            float deltaTime = Time.deltaTime;
            #region EditorOnly
#if UNITY_EDITOR
            if (!Application.isPlaying)
                deltaTime = 0;
#endif
            #endregion
            GeometryUtility.CalculateFrustumPlanes(currentCamera, frustumPlanes);
            for (int i = 0; i < 6; i++)
                frustumNormal[i] = new float4(frustumPlanes[i].normal.x, frustumPlanes[i].normal.y, frustumPlanes[i].normal.z, frustumPlanes[i].distance);

            staticMatrices_native.CopyFromFast(localToWorldMatrix, aliveInstances);

            JobHandle fillMatrixJob = new FillMatricesJob()
            {
                matrix = localToWorldMatrix_native,
                staticMatrices = staticMatrices_native
            }.ScheduleReadOnly(transformArray_native, 32);

            JobHandle updateCullingJob = new UpdateCullingJob()
            {
                cameraPos = cameraPosition,
                deltaTime = deltaTime,
                lodBias = QualitySettings.lodBias,
                frustumPlanes0 = frustumNormal[0],
                frustumPlanes1 = frustumNormal[1],
                frustumPlanes2 = frustumNormal[2],
                frustumPlanes3 = frustumNormal[3],
                frustumPlanes4 = frustumNormal[4],
                frustumPlanes5 = frustumNormal[5],
                matrices = localToWorldMatrix_native,
                unscaledOffsets = offsets_native,
                unscaledSized = sizes_native,
                visabilities = visable_native,
                LodHeights = LodHeights_native
            }.Schedule(aliveInstances, 32, fillMatrixJob);

            JobHandle updateTransitions = new UpdateTransitionsJob()
            {
                deltaTime = deltaTime,
                baseAnimDataArray = baseAnimData_native,
                transitionAnimDataArray = transitionAnimData_native,
                visabilityArray = visable_native
            }.Schedule(aliveInstances, 32, updateCullingJob);

            JobHandle job = new UpdateAnimJob()
            {
                deltaTime = deltaTime,
                baseAnimDataArray = baseAnimData_native,
                standardAnimDataArray = standardAnimData_native,
                visabilityArray = visable_native
            }.Schedule(aliveInstances, 32, updateTransitions);

            InstancedAnimationHelper.EndSample();

            job.Complete();

            InstancedAnimationHelper.BeginSample("Copy Native Data");
            localToWorldMatrix_native.CopyToFast(localToWorldMatrix, aliveInstances);
            visable_native.CopyToFast(visable, aliveInstances);
            smartHolder.CopyData(aliveInstances);
            InstancedAnimationHelper.EndSample();

            for (int i = 0; i < aliveInstances; ++i)
            {
                InstancedRenderer instance = instancedRenderersList[i];

                if (instance.CanSkip())
                    continue;
                smartHolder.Clear();
                smartHolder.instanceID = i;
                smartHolder.LoadBaseAnimData();

                float aniIndex = smartHolder.baseAnimData.aniIndex;
                byte lodLevel = visable[i];
                if (aniIndex < 0.0f || !instance.InternalUpdate(deltaTime, lodLevel, smartHolder))
                    continue;
                lodLevel = lodLevel < 3 ? lodLevel : (byte)0;
                InstancingLODData lod = instance.curretnLod;

                InstancedAnimationHelper.BeginSample("Preparing package()");
                for (int j = 0, j_size = lod.vertexCacheList.Length; j < j_size; ++j)
                    PutPackageInto(lod.materialBlockList[j], lod.vertexCacheList[j], instance, lodLevel, j);
                for (int j = 0, j_size = instance.attachments.Count; j < j_size; ++j)
                {
                    InstancedAnimationAttachment r = instance.attachments[j];
                    if (!r.render)
                        continue;
                    RuntimeInstancingSharedAttachment ria = r.shared;
                    if (ria.maxLOD < lodLevel)
                        continue;
                    PutPackageInto(ria.materialBlockList, ria.vertexCacheList, instance);
                }
                InstancedAnimationHelper.EndSample();
            }
            InstancedAnimationHelper.BeginSample("Copy Native Data back");
            smartHolder.CopyDataBack(aliveInstances);
            InstancedAnimationHelper.EndSample();
        }

        private void PutPackageInto(MaterialBlock block, VertexCache cache, InstancedRenderer instance, int LOD = -1, int mesh = -1)
        {
            int packageIndex = block.runtimePackageIndex;
            InstancingPackage package = block.packageList[packageIndex];
            if (package.instancingCount + 1 > instancingPackageSize)
            {
                block.runtimePackageIndex = ++packageIndex;
                if (packageIndex >= block.packageList.Count)
                {
                    InstancingPackage newPackage = CreatePackage(block.instanceData, cache.mesh, cache.materials);
                    block.packageList.Add(newPackage);
                    PreparePackageMaterial(newPackage, cache);
                    newPackage.instancingCount = 1;
                    package = newPackage;
                }
                else
                {
                    package = block.packageList[packageIndex];
                    package.instancingCount = 1;
                }
            }
            else
                ++package.instancingCount;

            int count = package.instancingCount - 1;
            if (count >= 0)
            {
                InstanceData data = block.instanceData;

                float preFrameIndex = -1;
                float transition = smartHolder.baseAnimData.transitionProgress;
                if (transition < 1f)
                    preFrameIndex = instance.previousAnimationInfo.animationIndex + smartHolder.baseAnimData.preFrame;

                data.worldMatrix[packageIndex][count] = localToWorldMatrix[instance.InstanceJobId];
                data.frameIndex[packageIndex][count] = instance.currentAnimationInfo.animationIndex + smartHolder.baseAnimData.curFrame;
                data.preFrameIndex[packageIndex][count] = preFrameIndex;
                data.transitionProgress[packageIndex][count] = transition;

#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
                if (data.customFloatData != null)
                {
                    CustomValueFloatHolder[] customValues = instance.animationData.customFloatIndex[LOD][mesh];
                    if (customValues != null)
                    {
                        for (int i = 0; i < customValues.Length; ++i)
                        {
                            CustomValueFloatHolder cvh = customValues[i];
                            data.customFloatData[cvh.submeshID][i].data[packageIndex][count] = instance.customFloatValues[cvh.customIndex];

                        }
                    }
                }
                if (data.customVectorData != null)
                {
                    CustomValueVectorHolder[] customValues = instance.animationData.customVectorIndex[LOD][mesh];
                    if (customValues != null)
                    {
                        for (int i = 0; i < customValues.Length; ++i)
                        {
                            CustomValueVectorHolder cvh = customValues[i];
                            data.customVectorData[cvh.submeshID][i].data[packageIndex][count] = instance.customVectorValues[cvh.customIndex];

                        }
                    }
                }
#endif
            }
        }

        internal void UpdateRender()
        {
            foreach (KeyValuePair<int, VertexCache> obj in vertexCachePool)
            {
                VertexCache vertexCache = obj.Value;
                foreach (KeyValuePair<int, MaterialBlock> block in vertexCache.instanceBlockList)
                {
                    MaterialBlock materialBlock = block.Value;
                    List<InstancingPackage> packageList = materialBlock.packageList;
                    for (int i = 0, i_size = packageList.Count; i < i_size; ++i)
                    {
                        InstancingPackage package = packageList[i];
                        if (package.instancingCount == 0)
                            continue;
                        InstanceData data = materialBlock.instanceData;
                        //if at least one of submeshes has custom value, this will be true
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
                        bool hasFloats = data.customFloatData != null;
                        bool hasVector = data.customVectorData != null;
#endif

                        for (ushort j = 0, j_size = package.subMeshCount; j < j_size; ++j)
                        {
                            if (useInstancing)
                            {
                                #region EditorOnly
#if UNITY_EDITOR
                                if (vertexCache.lodData != null)
                                {//update values if user change anything in inspector
                                    vertexCache.shadowcastingMode = vertexCache.lodData.shadowsMode[vertexCache.meshID];
                                    vertexCache.receiveShadow = vertexCache.lodData.receiveShadows[vertexCache.meshID];
                                    vertexCache.layer = vertexCache.lodData.layer[vertexCache.meshID];
                                }
                                PreparePackageMaterial(package, vertexCache);//this restore materials data after assets refresh
#endif
                                #endregion
                                MaterialPropertyBlock propertyBlock = package.propertyBlock;
                                propertyBlock.SetFloatArray(shader_frameIndex, data.frameIndex[i]);
                                propertyBlock.SetFloatArray(shader_preFrameIndex, data.preFrameIndex[i]);
                                propertyBlock.SetFloatArray(shader_transitionProgress, data.transitionProgress[i]);
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
                                if (hasFloats)
                                {
                                    List<CustomFloatData> floatList = data.customFloatData[j];
                                    if (floatList != null)
                                    {
                                        for (int k = 0, k_size = floatList.Count; k < k_size; ++k)
                                        {
                                            CustomFloatData cfd = floatList[k];
                                            propertyBlock.SetFloatArray(cfd.shaderID, cfd.data[i]);
                                        }
                                    }
                                }
                                if (hasVector)
                                {
                                    List<CustomVectorData> vectorList = data.customVectorData[j];
                                    if (vectorList != null)
                                    {
                                        for (int k = 0, k_size = vectorList.Count; k < k_size; ++k)
                                        {
                                            CustomVectorData cfd = vectorList[k];
                                            propertyBlock.SetVectorArray(cfd.shaderID, cfd.data[i]);
                                        }
                                    }
                                }
#endif

                                if (renderOnlyToCurrentCamera)
                                    Graphics.DrawMeshInstanced(vertexCache.mesh, j,
                                        package.material[j], data.worldMatrix[i],
                                        package.instancingCount, propertyBlock,
                                        vertexCache.shadowcastingMode, vertexCache.receiveShadow,
                                        vertexCache.layer, currentCamera);
                                else
                                    Graphics.DrawMeshInstanced(vertexCache.mesh, j,
                                        package.material[j], data.worldMatrix[i],
                                        package.instancingCount, propertyBlock,
                                        vertexCache.shadowcastingMode, vertexCache.receiveShadow,
                                        vertexCache.layer);
                            }
                            else
                            {
                                package.material[j].SetFloat(shader_frameIndex, data.frameIndex[i][0]);
                                package.material[j].SetFloat(shader_preFrameIndex, data.preFrameIndex[i][0]);
                                package.material[j].SetFloat(shader_transitionProgress, data.transitionProgress[i][0]);
                                Graphics.DrawMesh(vertexCache.mesh, data.worldMatrix[i][0],
                                    package.material[j], 0, null, j);
                            }
                        }
                        package.instancingCount = 0;
                    }
                    materialBlock.runtimePackageIndex = 0;
                }
            }
        }

        #endregion

        #region native management
        private void InitNative(int size)
        {
            DisposeNative();
            nativeInit = true;
            maxSize = size;
            LodHeights_native = new NativeArray<float4>(size, Allocator.Persistent);
            sizes_native = new NativeArray<float>(size, Allocator.Persistent);
            offsets_native = new NativeArray<float3>(size, Allocator.Persistent);
            baseAnimData_native = new NativeArray<BaseAnimData>(size, Allocator.Persistent);
            transitionAnimData_native = new NativeArray<TransitionAnimData>(size, Allocator.Persistent);
            standardAnimData_native = new NativeArray<StandardAnimData>(size, Allocator.Persistent);
            localToWorldMatrix_native = new NativeArray<Matrix4x4>(size, Allocator.Persistent);
            staticMatrices_native = new NativeArray<Matrix4x4>(size, Allocator.Persistent);
            visable_native = new NativeArray<byte>(size, Allocator.Persistent);
            transformArray_native = new TransformAccessArray(size, -1);

            localToWorldMatrix = new Matrix4x4[size];
            visable = new byte[size];
            smartHolder = new AnimationTmpHolder(ref baseAnimData_native, ref transitionAnimData_native, ref standardAnimData_native);
        }

        private void DisposeNative()
        {
            if (!nativeInit)
                return;
            nativeInit = false;
            LodHeights_native.Dispose();
            baseAnimData_native.Dispose();
            standardAnimData_native.Dispose();
            transitionAnimData_native.Dispose();
            offsets_native.Dispose();
            sizes_native.Dispose();
            staticMatrices_native.Dispose();
            localToWorldMatrix_native.Dispose();
            visable_native.Dispose();
            transformArray_native.Dispose();
        }
        #endregion

        #region manage instances register
        internal void AddInstance(InstancedRenderer script)
        {
            if (instancedRenderersList.Count + 1 > maxSize)
                throw new InstancedRenderersLimitReached("Reached limit of Instanced renderers! To increase limit go to Project Settings->Instanced Rendering");

            script.CheckAnimationValid();
            AddInstanceInternal(script);
            script.InitializeAnimation();
        }

        private void AddInstanceInternal(InstancedRenderer instance)
        {
            int id = instancedRenderersList.Count;
            instance.InstanceJobId = id;
            instancedRenderersList.Add(instance);

            transformArray_native.Add(instance.transformReference);
            localToWorldMatrix[id] = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);

            offsets_native[id] = instance.animationData.boundingSphereOffset;
            sizes_native[id] = instance.animationData.boundingSphereRadius;
            LodHeights_native[id] = instance.animationData.lodFloat;
            baseAnimData_native[id] = new()
            {
                aniIndex = 0,
                curFrame = 0,
                globalSpeed = 1f,
                preFrame = -1,
                transitionProgress = 1f,
                cullMode = (byte)instance.cullingMode
            };
            transitionAnimData_native[id] = new();
            standardAnimData_native[id] = new();
        }

        internal void RemoveInstance(InstancedRenderer instance)
        {
            int id = instance.InstanceJobId;
            if (id == -1)
                return;
            instance.InstanceJobId = -1;
            int lastElementID = instancedRenderersList.Count - 1;

            transformArray_native.RemoveAtSwapBack(id);

            if (lastElementID > id)
            {//remove not last element, copy data!
                InstancedRenderer moved = instancedRenderersList[lastElementID];
                moved.InstanceJobId = id;
                instancedRenderersList[id] = moved;

                sizes_native[id] = sizes_native[lastElementID];
                offsets_native[id] = offsets_native[lastElementID];
                LodHeights_native[id] = LodHeights_native[lastElementID];
                baseAnimData_native[id] = baseAnimData_native[lastElementID];
                transitionAnimData_native[id] = transitionAnimData_native[lastElementID];
                standardAnimData_native[id] = standardAnimData_native[lastElementID];
                localToWorldMatrix[id] = localToWorldMatrix[lastElementID];
                //TODO add rest (if any new will be added)
            }
            instancedRenderersList.RemoveAt(lastElementID);
        }
        #endregion

#if UNITY_EDITOR
        private void EditorOnly_ResetScriptableInstanceID()
        {
            string[] paths = UnityEditor.AssetDatabase.FindAssets("t: BlackRoseProjects.InstancedAnimationSystem.InstancedAnimationData");
            for (int i = 0; i < paths.Length; i++)
            {
                InstancedAnimationData data = UnityEditor.AssetDatabase.LoadAssetAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(paths[i]), typeof(InstancedAnimationData)) as InstancedAnimationData;
                if (data != null)
                    data.OnEnable();//force this call when reloading of domain is disabled, to clear ID's from previous runtime
            }
        }
#endif

        private void RefreshMaterial()
        {
            if (vertexCachePool == null)
                return;

            foreach (KeyValuePair<int, VertexCache> obj in vertexCachePool)
            {
                VertexCache cache = obj.Value;
                foreach (KeyValuePair<int, MaterialBlock> block in cache.instanceBlockList)
                {
                    MaterialBlock materialBlock = block.Value;
                    for (int k = 0; k < materialBlock.packageList.Count; ++k)
                    {
                        InstancingPackage package = materialBlock.packageList[k];
                        PreparePackageMaterial(package, cache);
                    }
                }
            }
        }
        private void PreparePackageMaterial(InstancingPackage package, VertexCache vertexCache)
        {
            if (vertexCache.boneTextureIndex < 0)
                return;

            for (ushort i = 0; i < package.subMeshCount; ++i)
            {
                AnimationTexture texture = animationTextureList[vertexCache.boneTextureIndex];
                Texture2D boneText = texture.boneTexture;
                package.material[i].SetTexture(shader__boneTexture, boneText);
                package.material[i].SetInt(shader__boneTextureWidth, boneText.width);
                package.material[i].SetInt(shader__boneTextureHeight, boneText.height);
                package.material[i].SetInt(shader__boneTextureBlockWidth, texture.blockWidth);
                package.material[i].SetInt(shader__boneTextureBlockHeight, texture.blockHeight);
                package.material[i].SetInt(shader__matCount, texture.blockWidth / 4);
                package.material[i].SetInt(shader__blockCount, boneText.width / texture.blockWidth);
            }
        }
        private int GetIdentify(Material[] mat)
        {
            int hash = 0;
            for (int i = 0; i < mat.Length; ++i)
                hash += mat[i].name.GetHashCode();
            return hash;
        }

        private void ReleaseBuffer()
        {
            if (vertexCachePool != null)
            {
                foreach (KeyValuePair<int, VertexCache> obj in vertexCachePool)
                    obj.Value.Clear();
                vertexCachePool.Clear();
            }
        }

        internal InstancingPackage CreatePackage(InstanceData data, Mesh mesh, Material[] originalMaterial)
        {
            InstancingPackage package = new InstancingPackage();
            package.material = new Material[mesh.subMeshCount];
            package.subMeshCount = (ushort)mesh.subMeshCount;
            for (int i = 0; i < mesh.subMeshCount; ++i)
            {
                Material mat = originalMaterial[i];
                package.material[i] = mat;
                package.propertyBlock = new MaterialPropertyBlock();
            }

            Matrix4x4[] matrix = new Matrix4x4[instancingPackageSize];
            float[] frameIndex = new float[instancingPackageSize];
            float[] preFrameIndex = new float[instancingPackageSize];
            float[] transitionProgress = new float[instancingPackageSize];
            data.worldMatrix.Add(matrix);
            data.frameIndex.Add(frameIndex);
            data.preFrameIndex.Add(preFrameIndex);
            data.transitionProgress.Add(transitionProgress);
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
            if (data.customFloatData != null)
            {
                for (int i = 0; i < data.customFloatData.Length; ++i)
                    if (data.customFloatData[i] != null)
                        for (int j = 0; j < data.customFloatData[i].Count; ++j)
                            data.customFloatData[i][j].data.Add(new float[instancingPackageSize]);
            }
            if (data.customVectorData != null)
            {
                for (int i = 0; i < data.customVectorData.Length; ++i)
                    if (data.customVectorData[i] != null)
                        for (int j = 0; j < data.customVectorData[i].Count; ++j)
                            data.customVectorData[i][j].data.Add(new Vector4[instancingPackageSize]);
            }
#endif
            return package;
        }

        internal void AddMeshVertex(InstancedAnimationData animationData)
        {
            for (int lodID = 0, lodSize = animationData.LOD.Length; lodID < lodSize; ++lodID)
            {
                InstancingLODData ild = animationData.LOD[lodID];
                ild.vertexCacheList = new VertexCache[ild.instancingMeshData.Length];
                ild.materialBlockList = new MaterialBlock[ild.instancingMeshData.Length];
                for (int i = 0; i < ild.instancingMeshData.Length; ++i)
                {
                    InstancingMeshData imd = ild.instancingMeshData[i];
                    Mesh m = imd.mesh;
                    if (m is null)
                        continue;

                    int nameCode = m.name.GetHashCode() + animationData.GetHashCode();
                    int identify = GetIdentify(imd.fixedMaterials);

                    if (vertexCachePool.TryGetValue(nameCode, out VertexCache cache))
                    {
                        if (!cache.instanceBlockList.TryGetValue(identify, out MaterialBlock block))
                        {//same mesh, different materials
                            block = CreateBlock(cache, imd.fixedMaterials);
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
                            FillCustomValues(block, imd);
#endif
                            cache.instanceBlockList.Add(identify, block);
                        }
                        ild.vertexCacheList[i] = cache;
                        ild.materialBlockList[i] = block;
                        continue;
                    }

                    VertexCache vertexCache = CreateVertexCache(animationData, nameCode, false, m);
                    // vertexCache.bindPose = animationData.bindPoses;
                    vertexCache.materials = imd.fixedMaterials;
                    MaterialBlock matBlock = CreateBlock(vertexCache, imd.fixedMaterials);
                    vertexCache.instanceBlockList.Add(identify, matBlock);
                    vertexCache.layer = ild.layer[i];
                    vertexCache.shadowcastingMode = ild.shadowsMode[i];
                    vertexCache.receiveShadow = ild.receiveShadows[i];
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
                    FillCustomValues(matBlock, imd);
#endif
                    PreparePackageBase(vertexCache, matBlock, imd.fixedMaterials);

                    ild.vertexCacheList[i] = vertexCache;
                    ild.materialBlockList[i] = matBlock;
                    #region EditorOnly
#if UNITY_EDITOR
                    vertexCache.lodData = ild;
                    vertexCache.meshID = i;
#endif
                    #endregion
                }
            }
        }
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
        private void FillCustomValues(MaterialBlock block, InstancingMeshData imd)
        {
            if (imd.floatCustomData.Length > 0)
            {
                block.instanceData.customFloatData = new List<CustomFloatData>[imd.mesh.subMeshCount];
                for (int f = 0; f < imd.floatCustomData.Length; ++f)
                {
                    List<CustomFloatData> list = block.instanceData.customFloatData[imd.floatCustomData[f].submeshID];
                    if (list == null)
                        block.instanceData.customFloatData[imd.floatCustomData[f].submeshID] = list = new List<CustomFloatData>();
                    CustomFloatData cfd = (new CustomFloatData(Shader.PropertyToID(imd.floatCustomData[f].propertyName)));
                    cfd.data.Add(new float[instancingPackageSize]);
                    list.Add(cfd);
                }
            }

            if (imd.vectorCustomData.Length > 0)
            {
                block.instanceData.customVectorData = new List<CustomVectorData>[imd.mesh.subMeshCount];
                for (int f = 0; f < imd.vectorCustomData.Length; ++f)
                {
                    List<CustomVectorData> list = block.instanceData.customVectorData[imd.vectorCustomData[f].submeshID];
                    if (list == null)
                        block.instanceData.customVectorData[imd.vectorCustomData[f].submeshID] = list = new List<CustomVectorData>();
                    CustomVectorData cvd = (new CustomVectorData(Shader.PropertyToID(imd.vectorCustomData[f].propertyName)));
                    cvd.data.Add(new Vector4[instancingPackageSize]);
                    list.Add(cvd);
                }
            }
        }
#endif

        internal InstancedAnimationAttachment AddMeshVertexAttachment(InstancedAnimationData animationData, InstancedAttachmentData attachment, int codeName, int boneIndex, Bounds parentBounds)
        {
            if (attachment.mesh is null)
                throw new MeshNotReadableException($"Attachment data {attachment.name} is missing mesh!");
            if (!attachment.mesh.isReadable)
                throw new MeshNotReadableException($"Mesh {attachment.mesh.name} must be set to read/write in order to work as attachment");

            if (!attachments.TryGetValue(codeName, out RuntimeInstancingSharedAttachment sharedAttachment))
            {
                sharedAttachment = new RuntimeInstancingSharedAttachment();
                sharedAttachment.attachmentData = attachment;
                sharedAttachment.maxLOD = attachment.maxRenderLOD;
                sharedAttachment.boneIndex = boneIndex;
                sharedAttachment.originalMesh = attachment.mesh;
                sharedAttachment.vertices = attachment.mesh.vertices;
                sharedAttachment.normals = attachment.mesh.normals;
                attachments[codeName] = sharedAttachment;

                VertexCache vertexCache = CreateVertexCache(animationData, codeName, true, attachment.mesh);
                vertexCache.mesh.bounds = parentBounds;

                vertexCache.materials = GenerateAttachmentMaterials(attachment.materials);
                MaterialBlock matBlock = CreateBlock(vertexCache, vertexCache.materials);
                int identify = GetIdentify(attachment.materials);
                vertexCache.instanceBlockList.Add(identify, matBlock);
                vertexCache.layer = attachment.layer;
                vertexCache.shadowcastingMode = attachment.shadowMode;
                vertexCache.receiveShadow = attachment.receiveShadow;

                sharedAttachment.vertexCacheList = vertexCache;
                sharedAttachment.materialBlockList = matBlock;

                PreparePackageBase(vertexCache, matBlock, attachment.materials);
            }

            InstancedAnimationAttachment ild = new InstancedAnimationAttachment(sharedAttachment);
            return ild;
        }

        private Material[] GenerateAttachmentMaterials(Material[] materials)
        {
            Material[] result = new Material[materials.Length];
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = Instantiate(materials[i]);
                InstancedAnimationHelper.FixMaterialData(result[i], 1);
            }
            return result;
        }

        private MaterialBlock CreateBlock(VertexCache cache, Material[] materials)
        {
            MaterialBlock block = new MaterialBlock();
            block.instanceData = new InstanceData();
            block.packageList = new List<InstancingPackage>();
            InstancingPackage package = CreatePackage(block.instanceData, cache.mesh, materials);
            block.packageList.Add(package);
            PreparePackageMaterial(package, cache);
            package.instancingCount = 1;
            block.runtimePackageIndex = 0;

            return block;
        }

        private VertexCache CreateVertexCache(InstancedAnimationData animationPackName, int renderName, bool isAttachment, Mesh mesh)
        {
            VertexCache vertexCache = new VertexCache();
            vertexCachePool[renderName] = vertexCache;
            vertexCache.nameCode = renderName;
            if (isAttachment)
            {
                vertexCache.mesh = Instantiate(mesh);
                vertexCache.isMeshInstance = true;
                vertexCache.bonePerVertex = 1;
            }
            else
            {
                vertexCache.mesh = mesh;
                vertexCache.bonePerVertex = animationPackName.bonePerVertex;
            }
            vertexCache.boneTextureIndex = GetTextureID(animationPackName);
            vertexCache.instanceBlockList = new Dictionary<int, MaterialBlock>();
            return vertexCache;
        }

        private void PreparePackageBase(VertexCache vertexCache, MaterialBlock block, Material[] materials)
        {
            InstancingPackage package = CreatePackage(block.instanceData, vertexCache.mesh, materials);
            block.packageList.Add(package);
            PreparePackageMaterial(package, vertexCache);
        }

        internal static void BindAttachment(Matrix4x4 bindPose, InstancedAnimationAttachment attachmentCache, Vector3 scale, Vector3 positionOffset, Quaternion rotationOffset)
        {
            BindPoseToMesh(attachmentCache.shared.vertexCacheList.mesh,
                attachmentCache.shared.vertices, attachmentCache.shared.normals, attachmentCache.shared.boneIndex,
                attachmentCache.shared.meshBakedRigBoneIndex != attachmentCache.shared.boneIndex,
                bindPose, scale, positionOffset, rotationOffset);
            attachmentCache.shared.meshBakedRigBoneIndex = attachmentCache.shared.boneIndex;
        }

        internal static void BindPoseToMesh(Mesh mesh, Vector3[] vertices, Vector3[] normals, int boneIndex, bool bakeRig, Matrix4x4 bindPose, Vector3 scale, Vector3 positionOffset, Quaternion rotationOffset)
        {
            positionOffset = rotationOffset * positionOffset;
            Vector3 offset = bindPose.GetColumn(3);
            int vertexCount = vertices.Length;
            NativeArray<Vector3> nativeVertices = new NativeArray<Vector3>(vertices, Allocator.TempJob);
            BindMeshAttachment job = new BindMeshAttachment()
            {
                positionOffset = offset + positionOffset,
                rotationOffset = rotationOffset,
                scale = scale,
                vertex = nativeVertices
            };
            job.Schedule(vertexCount, 64).Complete();
            mesh.SetVertices(nativeVertices);
            nativeVertices.Dispose();

            if (bakeRig)
            {
                NativeArray<Vector4> boneInd = new NativeArray<Vector4>(vertexCount, Allocator.TempJob);
                NativeArray<Color> colors = new NativeArray<Color>(vertexCount, Allocator.TempJob);
                new GenerateMeshBakedRig()
                {
                    bone = boneIndex,
                    boneIndex = boneInd,
                    colors = colors
                }.Schedule(colors.Length, 64).Complete();
                mesh.SetColors(colors);
                mesh.SetUVs(2, boneInd);
                boneInd.Dispose();
                colors.Dispose();

                if (normals != null && normals.Length > 0)
                {
                    NativeArray<Vector3> nativeNormals = new NativeArray<Vector3>(normals, Allocator.TempJob);
                    new ConvertAttachmentNormal()
                    {
                        normal = nativeNormals,
                        rotationOffset = rotationOffset
                    }.Schedule(normals.Length, 64).Complete();
                    mesh.SetNormals(nativeNormals);
                    nativeNormals.Dispose();
                }
            }
            mesh.UploadMeshData(false);
        }
        #region Animations loading and management

        private int GetTextureID(InstancedAnimationData data)
        {
            if (data._RuntimeTextureID == -1)
            {
                AnimationTexture aniTexture = new AnimationTexture(data.animationTexture.name, data.animationTexture, data.blockWidth, data.blockHeight);
                data._RuntimeTextureID = animationTextureList.Count;
                animationTextureList.Add(aniTexture);
            }
            return data._RuntimeTextureID;
        }
        #endregion
    }
}
#endif
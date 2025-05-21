#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// Culling mode for Instanced Renderer that determinate if animations are calculated when object is not visable
    /// </summary>
    public enum InstancingCullingMode
    {
        /// <summary>
        /// Calculate animations, root motions and bone sync even when object is not visable
        /// </summary>
        AlwaysAnimate = 0,
        /// <summary>
        /// Stop calculating animations, root motions and bone sync when object is not visable
        /// </summary>
        CullCompletely = 1
    }


    /// <summary>
    /// Instance of Instanced Renderer that store individual values as well allow to manage rendering object
    /// <br/>InstancedRenderer is native class that allow to mantain rendering without use of GameObject
    /// </summary>
#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    public sealed class InstancedRenderer
    {
        internal struct BoneSyncData
        {
            public Transform transformToSync;
            public int boneIndex;
            public bool enabled;

            public BoneSyncData(Transform transformToSync, int boneIndex)
            {
                this.transformToSync = transformToSync;
                this.boneIndex = boneIndex;
                this.enabled = true;
            }
        }
        #region parameters
        internal InstancedAnimationData animationData;
        private InstancedAnimatorData animator;
        private int defaultAnimation = 0;
        private bool applyRootMotion = false;

        internal InstancingCullingMode cullingMode = InstancingCullingMode.AlwaysAnimate;
        internal Transform transformReference;
        internal bool hasTransform;
        internal GameObject eventsReceiver;

        private int queuedAnim;
        internal bool isHidden;
        internal bool isPaused;
        private float pauseSpeedStored;
        private bool hasEvents;
        internal AnimationInfo currentAnimationInfo, previousAnimationInfo;
        internal AnimationInfo[] aniInfo;
        internal InstancingLODData curretnLod;

        internal List<InstancedAnimationAttachment> attachments;
        internal List<BoneSyncData> boneSyncDatas;
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
        internal float[] customFloatValues;
        internal Vector4[] customVectorValues;
#endif
        private int eventIndex = -1;
        private AnimationEvent aniEvent = null;
        internal InstancedAnimator _animator;

        internal int InstanceJobId = -1;
        #endregion

        internal InstancedRenderer(InstancedAnimationData animData, InstancedAnimatorData animator, int defaultAnimation, bool applyRootMotion, InstancingCullingMode cullingMode)
        {
            this.animationData = animData;
            this.animator = animator;
            this.defaultAnimation = defaultAnimation;
            this.applyRootMotion = applyRootMotion;
            this.cullingMode = cullingMode;
        }

        #region public properties
        /// <summary>
        /// Current animation mode. Animator override this setting
        /// </summary>
        public WrapMode AnimationMode
        {
            get { return InstanceJobId >= 0 ? (WrapMode)InstancedAnimationManager.instance.standardAnimData_native[InstanceJobId].wrapMode : WrapMode.Default; }
            set
            {
                if (InstanceJobId >= 0 && _animator == null)
                {
                    StandardAnimData sad = InstancedAnimationManager.instance.standardAnimData_native[InstanceJobId];
                    sad.wrapMode = (byte)value;
                    InstancedAnimationManager.instance.standardAnimData_native[InstanceJobId] = sad;
                }
            }
        }

        /// <summary>
        /// Controlls what is updated when object is not visable
        /// </summary>
        public InstancingCullingMode CullingMode
        {
            get { return InstanceJobId >= 0 ? (InstancingCullingMode)InstancedAnimationManager.instance.baseAnimData_native[InstanceJobId].cullMode : InstancingCullingMode.AlwaysAnimate; }
            set
            {
                if (InstanceJobId >= 0)
                {
                    BaseAnimData bad = InstancedAnimationManager.instance.baseAnimData_native[InstanceJobId];
                    bad.cullMode = (byte)value;
                    InstancedAnimationManager.instance.baseAnimData_native[InstanceJobId] = bad;
                }
                cullingMode = value;
            }
        }

        /// <summary>
        /// Is animation paused
        /// </summary>
        /// <returns>true if animation is paused</returns>
        public bool IsPause { get { return isPaused; } }

        /// <summary>
        /// is this renderer hidden from rendering and calculating
        /// <br/>This value is also automatically set during OnDisable nad OnEnable
        /// </summary>
        public bool IsHidden { get { return isHidden; } set { isHidden = value; } }

        /// <summary>
        /// Multiplier for all animations played by this Instanced Renderer
        /// </summary>
        public float Speed
        {
            get { return InstanceJobId >= 0 ? InstancedAnimationManager.instance.baseAnimData_native[InstanceJobId].globalSpeed : 0f; }
            set
            {
                if (InstanceJobId >= 0)
                {
                    BaseAnimData bad = InstancedAnimationManager.instance.baseAnimData_native[InstanceJobId];
                    bad.globalSpeed = value;
                    InstancedAnimationManager.instance.baseAnimData_native[InstanceJobId] = bad;
                }
            }
        }

        /// <summary>
        /// Apply root motion to this Instanced renderer
        /// </summary>
        public bool RootMotion
        {
            get { return applyRootMotion; }
            set { applyRootMotion = value; }
        }

        /// <summary>
        /// Get Instanced Animator if any is created for this renderer
        /// </summary>
        public InstancedAnimator InstancedAnimator { get { return _animator; } }

        /// <summary>
        /// Gets if this Instanced Renderer has loaded animations
        /// </summary>
        public bool AnimationsReady { get { return aniInfo != null; } }

        /// <summary>
        /// Gets count of animations loaded for this Instanced Renderer
        /// </summary>
        public int AnimationsCount { get { return aniInfo != null ? aniInfo.Length : 0; } }

        /// <summary>
        /// Get names of animations loaded for this Instanced Renderer. Animations indexes matches animations id
        /// </summary>
        public string[] AnimationsNames
        {
            get
            {
                if (aniInfo == null)
                    return null;
                string[] names = new string[aniInfo.Length];
                for (int i = 0; i < names.Length; ++i)
                    names[i] = aniInfo[i].animationName;
                return names;
            }
        }

        /// <summary>
        /// The world space position of this Renderer
        /// </summary>
        public Vector3 position
        {
            get
            {
                if (hasTransform)
                    return transformReference.position;
                else if (InstanceJobId >= 0)
                    return InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId].GetPosition();
                return Vector3.zero;
            }
            set
            {
                if (hasTransform)
                    transformReference.position = value;
                else if (InstanceJobId >= 0)
                {
                    Matrix4x4 m = InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId];
                    m.m03 = value.x;
                    m.m13 = value.y;
                    m.m23 = value.z;
                    InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId] = m;
                }
            }
        }

        /// <summary>
        /// World space Matrix of this Renderer 
        /// </summary>
        public Matrix4x4 matrix
        {
            get
            {
                if (hasTransform)
                    return transformReference.localToWorldMatrix;
                else if (InstanceJobId >= 0)
                    return InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId];
                return Matrix4x4.identity;
            }
            set
            {
                if (!hasTransform && InstanceJobId >= 0)
                    InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId] = value;
            }
        }

        /// <summary>
        /// A Quaternion that stores the rotation of this Renderer in world space.
        /// </summary>
        public Quaternion rotation
        {
            get
            {
                if (hasTransform)
                    return transformReference.rotation;
                else if (InstanceJobId >= 0)
                    return InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId].rotation;
                return Quaternion.identity;
            }
            set
            {
                if (hasTransform)
                    transformReference.rotation = value;
                else if (InstanceJobId >= 0)
                {
                    Matrix4x4 mat = InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId];
                    InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId] = Matrix4x4.TRS(mat.GetPosition(), value, mat.lossyScale);
                }
            }
        }

        /// <summary>
        /// Scale of this Renderer. While using Transform rendering this value is local scale. While Transformless rendering, this value is absolute scale
        /// </summary>
        public Vector3 scale
        {
            get
            {
                if (hasTransform)
                    return transformReference.localScale;
                else if (InstanceJobId >= 0)
                    return InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId].lossyScale;
                return Vector3.one;
            }
            set
            {
                if (hasTransform)
                    transformReference.localScale = value;
                else if (InstanceJobId >= 0)
                {
                    Matrix4x4 mat = InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId];
                    InstancedAnimationManager.instance.localToWorldMatrix[InstanceJobId] = Matrix4x4.TRS(mat.GetPosition(), mat.rotation, value);
                }
            }
        }

        /// <summary>
        /// GameObject that will receive animations events. While using Transform rendering, this object is set to Transform's GameObject
        /// </summary>
        public GameObject EventsReceiver
        {
            get { return eventsReceiver; }
            set { eventsReceiver = value; }
        }
        #endregion

        #region Internal

        internal void Initialize(Transform transformReference)
        {
            if (animator != null)
                _animator = new InstancedAnimator(this, animator);

            this.transformReference = transformReference;
            hasTransform = this.transformReference != null;
            if (hasTransform) eventsReceiver = transformReference.gameObject;
            attachments = new List<InstancedAnimationAttachment>();

            curretnLod = animationData.LOD[0];

            InstancedAnimationManager.Instance.AddInstance(this);
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
            if (animationData.customFloats != null)
            {
                customFloatValues = new float[animationData.customFloats.Count];
                for (int i = 0; i < customFloatValues.Length; ++i)
                    customFloatValues[i] = animationData.customFloats[i].defaultFloat;
            }

            if (animationData.customVectors != null)
            {
                customVectorValues = new Vector4[animationData.customVectors.Count];
                for (int i = 0; i < customVectorValues.Length; ++i)
                    customVectorValues[i] = animationData.customVectors[i].defaultVector;
                // customVectorValues[i] = new Vector4(UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f), UnityEngine.Random.Range(0, 1f),1); //animationData.customVectors[i].defaultVector;
            }
#endif
        }

        internal void CheckAnimationValid()
        {
            if (animationData == null || !animationData.CheckInitValid() || animationData.LOD.Length == 0)
                throw new NullReferenceException("animationData cannot be null or empty while creating InstancedRenderer");
        }

        /// <summary>
        /// Initialize animations for this renderer. Should be called only from Animation Instancing Manager
        /// </summary>
        internal void InitializeAnimation()
        {
            aniInfo = animationData.animations;
            Prepare(aniInfo);
        }

        /// <summary>
        /// Prepare this instance by attaching animations and registering to manager
        /// </summary>
        /// <param name="infoList">animations to attach</param>
        /// <param name="extraBoneInfo">additional bones to attach</param>
        internal void Prepare(AnimationInfo[] infoList)
        {
            aniInfo = infoList;
            InstancedAnimationManager.Instance.AddMeshVertex(animationData);

            if (animator != null)
            {
                defaultAnimation = -1;
                _animator.Init();
            }
            else
                PlayAnimation(defaultAnimation >= 0 ? defaultAnimation : 0, 0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool CanSkip()
        {
            return isHidden || (aniInfo == null || currentAnimationInfo == null);
        }

        internal bool InternalUpdate(float deltaTime, byte lodLevel, AnimationTmpHolder smartData)
        {
            if (lodLevel <= 3)
                curretnLod = animationData.LOD[lodLevel];

            bool isVisable = lodLevel <= 3;
            if (isPaused || deltaTime == 0f || !(isVisable || cullingMode == InstancingCullingMode.AlwaysAnimate))
                return isVisable;

            InstancedAnimationHelper.BeginSample("[ControlSample]");
            InstancedAnimationHelper.EndSample();

            //bool update = isVisable || cullingMode == CullingMode.AlwaysAnimate;
            InstancedAnimationHelper.BeginSample("RootMotion()");
            if (applyRootMotion && currentAnimationInfo.rootMotion)
                ApplyRootMotion(deltaTime, smartData.baseAnimData.curFrame);
            InstancedAnimationHelper.EndSample();

            if (boneSyncDatas != null)
            {
                int syncElements = boneSyncDatas.Count;
                if (syncElements > 0)
                {
                    InstancedAnimationHelper.BeginSample("SyncBoneTransforms()");
                    float preFrameIndex = 0;
                    float frameIndex = currentAnimationInfo.animationIndex + smartData.baseAnimData.curFrame;
                    float transition = smartData.baseAnimData.transitionProgress;
                    if (transition < 1f) preFrameIndex = previousAnimationInfo.animationIndex + smartData.baseAnimData.preFrame;
                    Matrix4x4 myMatrix = matrix;
                    for (int i = 0; i < syncElements; ++i)
                    {
                        BoneSyncData byd = boneSyncDatas[i];
                        if (byd.enabled)
                            animationData.TransformateBonePosition(byd.boneIndex, frameIndex, preFrameIndex, transition, byd.transformToSync, ref myMatrix);
                    }
                    InstancedAnimationHelper.EndSample();
                }
            }

            InstancedAnimationHelper.BeginSample("Events()");
            if (hasEvents)
                UpdateAnimationEvent(smartData);
            InstancedAnimationHelper.EndSample();

            InstancedAnimationHelper.BeginSample("Animator()");
            if (_animator != null)
            {
                if (isVisable || cullingMode != InstancingCullingMode.CullCompletely)
                    _animator.Update(smartData);
            }
            else
            {
                if (queuedAnim >= 0)
                {
                    smartData.LoadStandardAnimData();
                    if (smartData.standardAnimData.speedParameter == 0.0f && smartData.baseAnimData.curFrame >= smartData.standardAnimData.totalFrame - 1)
                    {//start play queued anim
                        PlayAnimation(queuedAnim, 0, false, smartData);
                        queuedAnim = -1;
                    }
                }
            }
            InstancedAnimationHelper.EndSample();
            smartData.SaveData();
            return isVisable;
        }

        internal void PlayAnimation(int animationIndex, float crossFadeDuration, bool fixedDuration, AnimationTmpHolder data)
        {
            if (aniInfo == null)
                return;
            data.LoadTransitionAnimData();//only transitions are needed, cause other data is catched by animator and default
            if (animationIndex < aniInfo.Length)
            {
                if (data.baseAnimData.transitionProgress < 1f)
                {
                    if (animationIndex == data.transitionAnimData.preIndex)
                    {//not allow to that!
                        previousAnimationInfo = currentAnimationInfo;

                        data.transitionAnimData.preIndex = data.baseAnimData.aniIndex;
                        data.transitionAnimData.fps = data.standardAnimData.fps;
                        data.transitionAnimData.totalFrame = data.standardAnimData.totalFrame;
                        data.baseAnimData.preFrame = data.baseAnimData.curFrame;
                        data.transitionAnimData.preSpeedParameter = data.standardAnimData.speedParameter;
                        data.transitionAnimData.preWrapMode = data.standardAnimData.wrapMode;
                    }
                }
                else
                {
                    previousAnimationInfo = currentAnimationInfo;
                    data.transitionAnimData.preIndex = data.baseAnimData.aniIndex;
                    data.baseAnimData.preFrame = data.baseAnimData.curFrame;
                    data.transitionAnimData.fps = data.standardAnimData.fps;
                    data.transitionAnimData.totalFrame = data.standardAnimData.totalFrame;
                    data.transitionAnimData.preSpeedParameter = data.standardAnimData.speedParameter;
                    data.transitionAnimData.preWrapMode = data.standardAnimData.wrapMode;
                }
                if (animationIndex >= 0)
                {
                    currentAnimationInfo = aniInfo[animationIndex];
                    hasEvents = currentAnimationInfo.eventList != null;
                    eventIndex = -1;

                    data.baseAnimData.curFrame = 0.0f;
                    data.baseAnimData.aniIndex = animationIndex;
                    data.standardAnimData.fps = currentAnimationInfo.fps;
                    data.standardAnimData.totalFrame = currentAnimationInfo.totalFrame;
                    data.standardAnimData.wrapMode = (byte)currentAnimationInfo.wrapMode;
                    data.standardAnimData.speedParameter = 1.0f;
                }
                else
                {
                    data.standardAnimData.speedParameter = 0.0f;
                }
            }
            else
            {

                Debug.LogError($"Invalid animation index! ({animationIndex})");
                return;
            }
            if (crossFadeDuration > 0.0f)
            {
                data.transitionAnimData.transitionTimer = 0.0f;
                data.baseAnimData.transitionProgress = 0.0f;
                data.transitionAnimData.transitionDuration = fixedDuration ? crossFadeDuration : math.clamp(crossFadeDuration, 0f, 1f) * (currentAnimationInfo.totalFrame / currentAnimationInfo.fps);
            }
            else
            {
                data.transitionAnimData.transitionDuration = 0.0f;
                data.baseAnimData.transitionProgress = 1.0f;
            }
            data.standardAnimData_modified = 2;
            data.transitionAnimData_modified = 2;
        }
        #endregion

        #region private
#if BLACKROSE_INSTANCING_BURST
        [Unity.Burst.BurstCompile, Unity.Burst.CompilerServices.SkipLocalsInit]
        private static unsafe void BurstRootMotion(in float curFrame, in Vector3* velocity, in Vector3* angularVelocity, in float deltaTime, ref Vector3 localPosition, ref Quaternion localQuaternion)
        {
            int preSampleFrame = (int)curFrame;
            int nextSampleFrame = (int)(curFrame + 1.0f);
            float frameStep = curFrame - preSampleFrame;

            float3 v = math.lerp(velocity[preSampleFrame], velocity[nextSampleFrame], frameStep);
            float3 av = math.lerp(angularVelocity[preSampleFrame], angularVelocity[nextSampleFrame], frameStep);
            Quaternion delta = Quaternion.Euler(av * deltaTime);
            localQuaternion *= delta;
            localPosition += localQuaternion * (v * deltaTime);
        }

        [Unity.Burst.BurstCompile, Unity.Burst.CompilerServices.SkipLocalsInit]
        private static unsafe void BurstRootMotion(in float curFrame, in Vector3* velocity, in Vector3* angularVelocity, in float deltaTime, ref Matrix4x4 matrix)
        {
            Quaternion localQuaternion = matrix.rotation;
            Vector3 localPosition = matrix.GetPosition();

            int preSampleFrame = (int)curFrame;
            int nextSampleFrame = (int)(curFrame + 1.0f);
            float frameStep = curFrame - preSampleFrame;

            Vector3 v = Vector3.Lerp(velocity[preSampleFrame], velocity[nextSampleFrame], frameStep);
            Vector3 av = Vector3.Lerp(angularVelocity[preSampleFrame], angularVelocity[nextSampleFrame], frameStep);
            Quaternion delta = Quaternion.Euler(av * deltaTime);
            localQuaternion *= delta;

            Vector3 offset = v * deltaTime;
            offset = localQuaternion * offset;
            localPosition += offset;
            matrix.SetTRS(localPosition, localQuaternion, matrix.lossyScale);
        }

        private void ApplyRootMotion(float deltaTime, float curFrame)
        {
            int preSampleFrame = (int)curFrame;
            int nextSampleFrame = (int)(curFrame + 1.0f);
            if (nextSampleFrame >= currentAnimationInfo.totalFrame)
                return;
            if (hasTransform)
            {
                Quaternion localQuaternion = transformReference.rotation;
                Vector3 localPosition = transformReference.position;
                unsafe
                {
                    fixed (Vector3* velo = &currentAnimationInfo.velocity[0])
                    {
                        fixed (Vector3* anguVelo = &currentAnimationInfo.angularVelocity[0])
                        {
                            BurstRootMotion(in curFrame, in velo, in anguVelo, in deltaTime, ref localPosition, ref localQuaternion);
                        }
                    }
                }
                transformReference.SetPositionAndRotation(localPosition, localQuaternion);
            }
            else
            {
                Matrix4x4 matrix = this.matrix;
                unsafe
                {
                    fixed (Vector3* velo = &currentAnimationInfo.velocity[0])
                    {
                        fixed (Vector3* anguVelo = &currentAnimationInfo.angularVelocity[0])
                        {
                            BurstRootMotion(in curFrame, in velo, in anguVelo, in deltaTime, ref matrix);
                        }
                    }
                }
                this.matrix = matrix;
            }
        }
#else

        /// <summary>
        /// Apply root motion to this renderer
        /// </summary>
        /// <param name="deltaTime">delta time of this frame</param>
        private void ApplyRootMotion(float deltaTime, float curFrame)
        {
            int preSampleFrame = (int)curFrame;
            int nextSampleFrame = (int)(curFrame + 1.0f);
            if (nextSampleFrame >= currentAnimationInfo.totalFrame)//TODO zmienic na matrix
                return;
            float frameStep = curFrame - preSampleFrame;
            if (hasTransform)
            {
                Quaternion localQuaternion = transformReference.rotation;
                Vector3 localPosition = transformReference.position;

                Vector3 velocity = Vector3.Lerp(currentAnimationInfo.velocity[preSampleFrame], currentAnimationInfo.velocity[nextSampleFrame], frameStep);
                Vector3 angularVelocity = Vector3.Lerp(currentAnimationInfo.angularVelocity[preSampleFrame], currentAnimationInfo.angularVelocity[nextSampleFrame], frameStep);
                Quaternion delta = Quaternion.Euler(angularVelocity * deltaTime);
                localQuaternion *= delta;

                Vector3 offset = velocity * deltaTime;
                offset = localQuaternion * offset;
                localPosition += offset;
                transformReference.SetPositionAndRotation(localPosition, localQuaternion);
            }
            else
            {
                Matrix4x4 matrix = this.matrix;
                Quaternion localQuaternion = matrix.rotation;
                Vector3 localPosition = matrix.GetPosition();

                Vector3 velocity = Vector3.Lerp(currentAnimationInfo.velocity[preSampleFrame], currentAnimationInfo.velocity[nextSampleFrame], frameStep);
                Vector3 angularVelocity = Vector3.Lerp(currentAnimationInfo.angularVelocity[preSampleFrame], currentAnimationInfo.angularVelocity[nextSampleFrame], frameStep);
                Quaternion delta = Quaternion.Euler(angularVelocity * deltaTime);
                localQuaternion *= delta;

                Vector3 offset = velocity * deltaTime;
                offset = localQuaternion * offset;
                localPosition += offset;
                matrix.SetTRS(localPosition, localQuaternion, matrix.lossyScale);
                this.matrix = matrix;
            }
        }
#endif
        /// <summary>
        /// Update animation events
        /// </summary>
        private void UpdateAnimationEvent(AnimationTmpHolder manager)
        {
            float time = manager.baseAnimData.curFrame / currentAnimationInfo.fps;
            if (aniEvent == null)
            {
                for (int i = eventIndex >= 0 ? eventIndex : 0, i_size = currentAnimationInfo.eventList.Length; i < i_size; ++i)
                {
                    if (currentAnimationInfo.eventList[i].time > time)
                    {
                        aniEvent = currentAnimationInfo.eventList[i];
                        eventIndex = i;
                        break;
                    }
                }
            }

            if (aniEvent != null)
            {
                if (aniEvent.time <= time)
                {
                    if (eventsReceiver != null)
                        eventsReceiver.SendMessage(aniEvent.function, aniEvent);
                    aniEvent = null;
                }
            }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Destroy this instance
        /// </summary>
        public void Destroy()
        {
            while (attachments.Count > 0)
                Deattach(attachments[0]);
            if (InstancedAnimationManager.isApplicationAlive)
                InstancedAnimationManager.instance.RemoveInstance(this);
            InstanceJobId = -1;
            if (_animator != null)
            {
                _animator.Destroy();
                _animator = null;
            }
        }

        /// <summary>
        /// Set animator for this Instanced renderer. Using null will remove current animator. Override current animator if any exist
        /// </summary>
        /// <param name="bakedAnimatiorData">Baked animator data which will be used to create animator.</param>
        public void SetAnimator(InstancedAnimatorData bakedAnimatiorData)
        {
            if (bakedAnimatiorData == null)
            {
                if (_animator != null)
                {
                    _animator.Destroy();
                    _animator = null;
                }
            }
            else if (InstanceJobId >= 0)
            {
                if (_animator != null)
                    _animator.Destroy();
                _animator = new InstancedAnimator(this, bakedAnimatiorData);
                _animator.Init();
            }
        }

        /// <summary>
        /// Play animation of given name
        /// <br/>For better performance use <see cref="PlayAnimation(int)"/>
        /// </summary>
        /// <param name="name">Name of animation</param>
        public void PlayAnimation(string name)
        {
            int index = FindAnimationIndex(name);
            PlayAnimation(index, 0f);
        }

        /// <summary>
        /// Play animation of given index
        /// <br/>Animation index can be get from <see cref="FindAnimationIndex(string)"/>
        /// </summary>
        /// <param name="animationIndex">Id of animation</param>
        public void PlayAnimation(int animationIndex)
        {
            PlayAnimation(animationIndex, 0f);
        }

        /// <summary>
        /// Play animation of given index with cross fade from current animation
        /// <br/>Animation index can be get from <see cref="FindAnimationIndex(string)"/>
        /// </summary>
        /// <param name="animationIndex">Index of animation to play</param>
        /// <param name="crossFadeDuration">Duration of cross fade</param>
        /// <param name="FixedDuration">If true cross fade is in seconds, if false, cross fade time duration is a % of target state cycle (0.0 - 1.0)</param>
        public void PlayAnimation(int animationIndex, float crossFadeDuration, bool FixedDuration = false)
        {
            if (aniInfo == null)
                return;
            AnimationTmpHolder data = InstancedAnimationManager.smartHolder;
            data.instanceID = InstanceJobId;
            data.LoadDataFromNative();
            PlayAnimation(animationIndex, crossFadeDuration, FixedDuration, data);
            data.WriteDataToNative();
        }

        /// <summary>
        /// Play animation as cross fade. This animation will start with smooth transition from currently playing one
        /// <br/>For better performance use <see cref="PlayAnimation(int, float, bool)"/>
        /// </summary>
        /// <param name="animationName">Name of animation to play</param>
        /// <param name="crossFadeDuration">Transition time in seconds</param>
        public void PlayAnimation(string animationName, float crossFadeDuration)
        {
            PlayAnimation(FindAnimationIndex(animationName), crossFadeDuration);
        }

        /// <summary>
        /// Queue animation to start playing after currently playing animation ends. Works only without Instanced Animator
        /// <br/>For better performance use <see cref="QueueAnimation(int)"/>
        /// </summary>
        /// <param name="animationName">Animation to queue</param>
        public void QueueAnimation(string animationName)
        {
            queuedAnim = FindAnimationIndex(animationName);
        }

        /// <summary>
        /// Queue animation to start playing after currently playing animation ends. Works only without animator
        /// <br/>Animation index can be get from <see cref="FindAnimationIndex(string)"/>
        /// </summary>
        /// <param name="animationID">Id of animation to queue</param>
        public void QueueAnimation(int animationID)
        {
            queuedAnim = animationID;
        }

        /// <summary>
        /// Pause current animation
        /// </summary>
        public void Pause()
        {
            isPaused = true;
            StandardAnimData sad = InstancedAnimationManager.instance.standardAnimData_native[InstanceJobId];
            pauseSpeedStored = sad.speedParameter;
            sad.speedParameter = 0f;
            InstancedAnimationManager.instance.standardAnimData_native[InstanceJobId] = sad;
        }

        /// <summary>
        /// Resume currentAnimation
        /// </summary>
        public void Resume()
        {
            isPaused = false;
            StandardAnimData sad = InstancedAnimationManager.instance.standardAnimData_native[InstanceJobId];
            sad.speedParameter = pauseSpeedStored;
            InstancedAnimationManager.instance.standardAnimData_native[InstanceJobId] = sad;
        }

        /// <summary>
        /// Stop playing animation
        /// </summary>
        public void Stop()
        {
            currentAnimationInfo = null;
            hasEvents = false;
        }

        /// <summary>
        /// Search for animation index of given name
        /// </summary>
        /// <param name="animationName">Name of animation</param>
        /// <returns>Index of animation that match given name or -1 if no animation with that name found</returns>
        public int FindAnimationIndex(string animationName)
        {
            return InstancedAnimationHelper.FindAnimationIndex(aniInfo, animationName);
        }

        /// <summary>
        /// Attach attachment to this Instanced Renderer
        /// </summary>
        /// <param name="attachmentData">Attachment data used to create new attachment</param>
        /// <returns>Created attachment or null if unable to create</returns>
        public InstancedAnimationAttachment Attach(InstancedAttachmentData attachmentData)
        {
            if (attachmentData == null)
                throw new ArgumentNullException(nameof(attachmentData), "attachmentData cannot be null!");
            int index = -1;
            int hashBone = attachmentData.boneName.GetHashCode();

            for (int i = 0; i < animationData.bonesHashes.Length; ++i)
            {
                if (animationData.bonesHashes[i] == hashBone)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                Debug.LogError("Can't find the bone with name: " + attachmentData.boneName);
                return null;
            }
            InstancedAnimationManager.VertexCache parentCache = animationData.LOD[0].vertexCacheList[0];
            int nameCode = attachmentData.boneName.GetHashCode() + attachmentData.mesh.name.GetHashCode() + parentCache.nameCode + attachmentData.groupID;
            InstancedAnimationAttachment ild = InstancedAnimationManager.Instance.AddMeshVertexAttachment(animationData, attachmentData, nameCode, index, parentCache.mesh.bounds);
            if (ild == null)
                return null;
            InstancedAnimationManager.VertexCache cache = ild.shared.vertexCacheList;
            if (cache == null)
            {
                Debug.LogError("Can't find the VertexCache of shared attachment data");
                return null;
            }
            ild.parent = this;
            cache.boneTextureIndex = parentCache.boneTextureIndex;
            attachments.Add(ild);
            ild.shared.instancesCount++;
            InstancedAnimationManager.BindAttachment(animationData.bindPoses[index], ild, attachmentData.scale, attachmentData.positionOffset, Quaternion.Euler(attachmentData.rotationOffsetReal));
            return ild;
        }

        /// <summary>
        /// Set transform values of given attachement. Affected will be all shared attachments
        /// </summary>
        /// <param name="attachment">Attachment that will be modified</param>
        /// <param name="positionOffset">Position offset relative to bone</param>
        /// <param name="rotationOffset">Rotation offset relative to bone</param>
        /// <param name="scale">Scale of attachment</param>
        public void ConfigAttachment(InstancedAnimationAttachment attachment, Vector3 positionOffset, Vector3 rotationOffset, Vector3 scale)
        {
            ConfigAttachment(attachment, positionOffset, Quaternion.Euler(rotationOffset), scale);
        }

        /// <summary>
        /// Set transform values of given attachement. Modified will be all shared attachments
        /// </summary>
        /// <param name="attachment">Attachment that will be modified</param>
        /// <param name="positionOffset">Position offset relative to bone</param>
        /// <param name="rotationOffset">Rotation offset relative to bone</param>
        /// <param name="scale">Scale of attachment</param>
        public void ConfigAttachment(InstancedAnimationAttachment attachment, Vector3 positionOffset, Quaternion rotationOffset, Vector3 scale)
        {
            if (!attachments.Contains(attachment))
                return;

            InstancedAnimationManager.VertexCache parentCache = animationData.LOD[0].vertexCacheList[0];
            InstancedAnimationManager.VertexCache cache = attachment.shared.vertexCacheList;
            if (cache == null)
            {
                Debug.LogError("Can't find the VertexCache.");
                return;
            }
            InstancedAnimationManager.BindAttachment(animationData.bindPoses[attachment.shared.boneIndex], attachment, scale, positionOffset, rotationOffset);

            cache.boneTextureIndex = parentCache.boneTextureIndex;
        }

        /// <summary>
        /// Remove given attachment from this instanced renderer
        /// </summary>
        /// <param name="attachment">Attachment to remove form this Instanced Renderer</param>
        public void Deattach(InstancedAnimationAttachment attachment)
        {
            int id = attachments.IndexOf(attachment);
            if (id < 0 || attachment.parent != this)
                return;
            attachments.RemoveAt(id);
            attachment.shared.instancesCount--;
            attachment.parent = null;
            attachment.shared = null;
        }

        /// <summary>
        /// Get list of all attachments for this InstancedRenderer
        /// </summary>
        /// <returns>New array with all attachments</returns>
        public InstancedAnimationAttachment[] GetAttachments()
        {
            InstancedAnimationAttachment[] att = new InstancedAnimationAttachment[attachments.Count];
            for (int i = 0; i < att.Length; ++i)
                att[i] = attachments[i];
            return att;
        }

        /// <summary>
        /// Get count of all attachments for this InstancedRenderer
        /// </summary>
        /// <returns>Count of all attachments pinned to this Instanced Renderer, both enabled and disabled.</returns>
        public int GetAttachmentsCount()
        {
            return attachments.Count;
        }

        /// <summary>
        /// Start synchronization of given transform to bone.
        /// <br/>This is expensive feature and it's recommended to use only when neded
        /// </summary>
        /// <param name="transformToSync">Transform that will be synchronized position and rotation</param>
        /// <param name="boneName">Name of bone</param>
        /// <returns>True if synchronization has been enabled</returns>
        public bool StartBoneSyncTransform(Transform transformToSync, string boneName)
        {
            int index = -1;
            int hashBone = boneName.GetHashCode();

            for (int i = 0; i < animationData.bonesHashes.Length; ++i)
            {
                if (animationData.bonesHashes[i] == hashBone)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                Debug.LogError("Can't find the bone with name: " + boneName);
                return false;
            }
            if (transformToSync == null)
            {
                Debug.LogError("Try to start bone synchronization with Transform being null!");
                return false;
            }
            if (boneSyncDatas == null)
                boneSyncDatas = new List<BoneSyncData>();
            else
            {
                for (int i = 0; i < boneSyncDatas.Count; ++i)
                    if (boneSyncDatas[i].boneIndex == index)
                        throw new Exceptions.DuplicateBoneSynchronizationException("Trying to start bone synchronization for already synchronizing bone: " + boneName + ". This is not allowed");
            }
            boneSyncDatas.Add(new BoneSyncData(transformToSync, index));
            return true;
        }

        /// <summary>
        /// Stop all synchronization for given bone
        /// </summary>
        /// <param name="boneName">Name of bone to stop synchronization</param>
        /// <returns>True if successfully stoped bone synchronization</returns>
        public bool StopBoneSync(string boneName)
        {
            if (boneSyncDatas == null)
                return false;
            int index = -1;
            int hashBone = boneName.GetHashCode();

            for (int i = 0; i < animationData.bonesHashes.Length; ++i)
            {
                if (animationData.bonesHashes[i] == hashBone)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                Debug.LogError("Can't find the bone with name: " + boneName);
                return false;
            }
            for (int i = 0; i < boneSyncDatas.Count; ++i)
                if (boneSyncDatas[i].boneIndex == index)
                {
                    boneSyncDatas.RemoveAt(i);
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Check if given bone has transform synchronization enabled. This will return true even for paused synchronizations
        /// </summary>
        /// <param name="boneName">Name of bone</param>
        /// <returns>True if bone is synchronizing even if is paused</returns>
        public bool IsBoneSync(string boneName)
        {
            if (boneSyncDatas == null)
                return false;
            int index = -1;
            int hashBone = boneName.GetHashCode();

            for (int i = 0; i < animationData.bonesHashes.Length; ++i)
            {
                if (animationData.bonesHashes[i] == hashBone)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                Debug.LogError("Can't find the bone with name: " + boneName);
                return false;
            }
            for (int i = 0; i < boneSyncDatas.Count; ++i)
                if (boneSyncDatas[i].boneIndex == index)
                    return true;
            return false;
        }

        /// <summary>
        /// Check if given bone has paused synchronization
        /// </summary>
        /// <param name="boneName">Bone to check</param>
        /// <returns>True if synchronization is paused, otherwise return false even for not synchronizing bones</returns>
        public bool IsBoneSyncPaused(string boneName)
        {
            if (boneSyncDatas == null)
                return false;
            int index = -1;
            int hashBone = boneName.GetHashCode();

            for (int i = 0; i < animationData.bonesHashes.Length; ++i)
            {
                if (animationData.bonesHashes[i] == hashBone)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                Debug.LogError("Can't find the bone with name: " + boneName);
                return false;
            }
            for (int i = 0; i < boneSyncDatas.Count; ++i)
                if (boneSyncDatas[i].boneIndex == index)
                    return !boneSyncDatas[i].enabled;
            return false;
        }

        /// <summary>
        /// Set given bone synchronization to paused/unpaused
        /// </summary>
        /// <param name="boneName">Name of bone to switch synchronization pause</param>
        /// <param name="pause">State of pause</param>
        /// <returns>True if successfuy set state for bone synchronization</returns>
        public bool SetBoneSyncPause(string boneName, bool pause)
        {
            if (boneSyncDatas == null)
                return false;
            int index = -1;
            int hashBone = boneName.GetHashCode();

            for (int i = 0; i < animationData.bonesHashes.Length; ++i)
            {
                if (animationData.bonesHashes[i] == hashBone)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                Debug.LogError("Can't find the bone with name: " + boneName);
                return false;
            }
            for (int i = 0; i < boneSyncDatas.Count; ++i)
                if (boneSyncDatas[i].boneIndex == index)
                {
                    BoneSyncData bsy = boneSyncDatas[i];
                    bsy.enabled = !pause;
                    boneSyncDatas[i] = bsy;
                    return true;
                }
            return false;
        }

        #region Custom shader values

        /// <summary>
        /// Get array of defined custom shader float values
        /// </summary>
        /// <returns>Returns new array of handlers or null if no custom shader float values defined</returns>
        public CustomValueFloatHolder[] GetCustomShaderFloatValues()
        {
            return animationData.customFloats != null ? animationData.customFloats.ToArray() : null;
        }

        /// <summary>
        ///  Get array of defined custom shader vector values
        /// </summary>
        /// <returns>Returns new array of handlers or null if no custom shader vector values defined</returns>
        public CustomValueVectorHolder[] GetCustomShaderVectorValues()
        {
            return animationData.customVectors != null ? animationData.customVectors.ToArray() : null;
        }

        /// <summary>
        /// Get handle for custom shader values. Handle can be reused by any instance of InstancedRenderer that share same AnimationData.
        /// </summary>
        /// <param name="groupName">Name of group</param>
        /// <param name="groupType">Variable type of group</param>
        /// <returns>Group handle for given group and type. If no group match, group type is invalid</returns>
        public CustomValueGroupHandle GetCustomShaderValueGroup(string groupName, CustomValueGroupHandle.GroupType groupType)
        {
            return animationData.GetCustomValueGroupHandle(groupName, groupType);
        }

        /// <summary>
        /// Set custom shader float value for given Identifier. Identifier can be get from <see cref="CustomValueFloatHolder"/>
        /// </summary>
        /// <param name="value">Value to set in custom shader</param>
        /// <param name="IdentifierIndex">Parameter index. Can be get from <see cref="GetCustomShaderFloatValues"/></param>
        public void SetCustomShaderFloatValue(float value, int IdentifierIndex)
        {
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (customFloatValues == null)
            {
                Debug.LogError("This InstancedRendered don't have defined any custom shader float!");
                return;
            }
            else if (customFloatValues.Length <= IdentifierIndex && IdentifierIndex < 0)
            {
                Debug.LogError("IdentifierIndex is out of range!");
                return;
            }
#endif
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
            customFloatValues[IdentifierIndex] = value;
#else
            Debug.LogWarning("Trying to set Custom Shader Float Value while this feature is disabled! You can enable it in Project Settings");
#endif
        }

        /// <summary>
        /// Set custom shader float values for group of renderers. Group must match type
        /// <br/>Group Handle can be get from <see cref="GetCustomShaderValueGroup(string, CustomValueGroupHandle.GroupType)"/>
        /// </summary>
        /// <param name="value">Value to set in custom shader</param>
        /// <param name="floatGroup">Handler for this set of values</param>
        public void SetCustomShaderFloatValue(float value, in CustomValueGroupHandle floatGroup)
        {
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (customFloatValues == null)
            {
                Debug.LogError("This InstancedRendered don't have defined any custom shader float!");
                return;
            }
#endif
            if (floatGroup.groupType != CustomValueGroupHandle.GroupType.Float)
                throw new ArgumentException("Using non float CustomValueGroupHandle", nameof(floatGroup));
            for (int i = 0; i < floatGroup.indexes.Length; ++i)
            {
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
                if (floatGroup.indexes[i] >= customFloatValues.Length || floatGroup.indexes[i] < 0)
                {
                    Debug.LogError("IdentifierIndex from this group is out of range " + floatGroup.groupName);
                    continue;
                }
#endif
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
                customFloatValues[floatGroup.indexes[i]] = value;
#else
                Debug.LogWarning("Trying to set Custom Shader Float Value while this feature is disabled! You can enable it in Project Settings");
#endif
            }
        }

        /// <summary>
        /// Set custom shader Vector value for given Identifier. Identifier can be get from <see cref="CustomValueVectorHolder"/> 
        /// </summary>
        /// <param name="value">Value to set in custom shader</param>
        /// <param name="IdentifierIndex">Parameter index. Can be get from <see cref="CustomValueVectorHolder"/></param>
        public void SetCustomShaderVectorValue(Vector4 value, int IdentifierIndex)
        {
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (customVectorValues == null)
            {
                Debug.LogError("This InstancedRendered don't have defined any custom shader vector!");
                return;
            }
            else if (customVectorValues.Length <= IdentifierIndex && IdentifierIndex < 0)
            {
                Debug.LogError("IdentifierIndex is out of range!");
                return;
            }
#endif
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
            customVectorValues[IdentifierIndex] = value;
#else
            Debug.LogWarning("Trying to set Custom Shader Float Value while this feature is disabled! You can enable it in Project Settings");
#endif
        }

        /// <summary>
        /// Set custom shader vector values for group of renderers. Group must match type
        /// <br/>Group Handle can be get from <see cref="GetCustomShaderValueGroup(string, CustomValueGroupHandle.GroupType)"/>
        /// </summary>
        /// <param name="value">Value to set in custom shader</param>
        /// <param name="vectorGroup">Handler of this set of values</param>
        public void SetCustomShaderVectorValue(Vector4 value, in CustomValueGroupHandle vectorGroup)
        {
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (customVectorValues == null)
            {
                Debug.LogError("This InstancedRendered don't have defined any custom shader vector!");
                return;
            }
#endif
            if (vectorGroup.groupType != CustomValueGroupHandle.GroupType.Vector)
                throw new ArgumentException("Using non vector CustomValueGroupHandle", nameof(vectorGroup));
            for (int i = 0; i < vectorGroup.indexes.Length; ++i)
            {
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
                if (vectorGroup.indexes[i] >= customVectorValues.Length || vectorGroup.indexes[i] < 0)
                {
                    Debug.LogError("IdentifierIndex from this group is out of range " + vectorGroup.groupName);
                    continue;
                }
#endif
#if BLACKROSE_INSTANCING_CUSTOM_SHADER_VALUES
                customVectorValues[vectorGroup.indexes[i]] = value;
#else
                Debug.LogWarning("Trying to set Custom Shader Float Value while this feature is disabled! You can enable it in Project Settings");
#endif
            }
        }
        #endregion
        #endregion

        #region EditorOnly
#if UNITY_EDITOR
        internal float editor_frameIndex;
        internal float editor_preFrameIndex = 0;
        internal float editor_transition = 1;
        internal int editor_currentLODLevel = -1;
        internal static bool editor_transitionUpdate = false;
        internal static float editor_transitionScale = 1f;

        private static readonly int shader_frameIndex = Shader.PropertyToID("frameIndex");
        private static readonly int shader_preFrameIndex = Shader.PropertyToID("preFrameIndex");
        private static readonly int shader_transitionProgress = Shader.PropertyToID("transitionProgress");
        private static readonly int shader__boneTexture = Shader.PropertyToID("_boneTexture");
        private static readonly int shader__boneTextureBlockHeight = Shader.PropertyToID("_boneTextureBlockHeight");
        private static readonly int shader__boneTextureBlockWidth = Shader.PropertyToID("_boneTextureBlockWidth");
        private static readonly int shader__boneTextureHeight = Shader.PropertyToID("_boneTextureHeight");
        private static readonly int shader__boneTextureWidth = Shader.PropertyToID("_boneTextureWidth");
        private static readonly int shader__blockCount = Shader.PropertyToID("_blockCount");
        private static readonly int shader__matCount = Shader.PropertyToID("_matCount");

        private static readonly List<float> editor_reusableList = new List<float>(1) { 0f };
        private static readonly Matrix4x4[] editor_matrixArray = new Matrix4x4[1];
        private static readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        internal void EditorOnly_Initialize(Transform transformReference)
        {
            this.transformReference = transformReference;
            hasTransform = true;
            curretnLod = animationData.LOD[0];
            EditorOnly_RefreshMaterials();
        }

        internal void EditorOnly_RefreshMaterials()
        {
            for (int i = 0; i < animationData.LOD.Length; ++i)
                for (int j = 0; j < animationData.LOD[i].instancingMeshData.Length; ++j)
                    for (int k = 0; k < animationData.LOD[i].instancingMeshData[j].fixedMaterials.Length; ++k)
                    {
                        Material mat = animationData.LOD[i].instancingMeshData[j].fixedMaterials[k];
                        mat.enableInstancing = true;
                        mat.SetTexture(shader__boneTexture, animationData.animationTexture);
                        mat.SetInt(shader__boneTextureWidth, animationData.animationTexture.width);
                        mat.SetInt(shader__boneTextureHeight, animationData.animationTexture.height);
                        mat.SetInt(shader__boneTextureBlockWidth, animationData.blockWidth);
                        mat.SetInt(shader__boneTextureBlockHeight, animationData.blockHeight);
                        mat.SetInt(shader__matCount, animationData.blockWidth / 4);
                        mat.SetInt(shader__blockCount, animationData.animationTexture.width / animationData.blockWidth);
                    }
        }

        internal void EditorOnly_Render(List<Camera> cameras)
        {
            if (editor_currentLODLevel == -1)
                return;
            editor_matrixArray[0] = transformReference.localToWorldMatrix;
            editor_reusableList[0] = editor_frameIndex;
            propertyBlock.SetFloatArray(shader_frameIndex, editor_reusableList);
            editor_reusableList[0] = editor_preFrameIndex;
            propertyBlock.SetFloatArray(shader_preFrameIndex, editor_reusableList);
            editor_reusableList[0] = editor_transition;
            propertyBlock.SetFloatArray(shader_transitionProgress, editor_reusableList);
            InstancingLODData lod = animationData.LOD[editor_currentLODLevel];
            for (int i = 0; i < lod.instancingMeshData.Length; ++i)
            {
                InstancingMeshData imd = lod.instancingMeshData[i];
                for (int j = 0; j < imd.fixedMaterials.Length; ++j)
                    for (int k = 0; k < cameras.Count; ++k)
                    {
                        if (lod.instancingMeshData[i].fixedMaterials[j].enableInstancing)
                            Graphics.DrawMeshInstanced(imd.mesh, j, lod.instancingMeshData[i].fixedMaterials[j], editor_matrixArray, 1, propertyBlock, curretnLod.shadowsMode[0], lod.receiveShadows[i], lod.layer[i], cameras[k]);
                    }
            }
        }

        internal void EditorOnly_RenderMesh(Camera camera, Mesh mesh, Material material, int submeshID)
        {
            material.enableInstancing = true;
            material.SetTexture(shader__boneTexture, animationData.animationTexture);
            material.SetInt(shader__boneTextureWidth, animationData.animationTexture.width);
            material.SetInt(shader__boneTextureHeight, animationData.animationTexture.height);
            material.SetInt(shader__boneTextureBlockWidth, animationData.blockWidth);
            material.SetInt(shader__boneTextureBlockHeight, animationData.blockHeight);
            material.SetInt(shader__matCount, animationData.blockWidth / 4);
            material.SetInt(shader__blockCount, animationData.animationTexture.width / animationData.blockWidth);

            editor_matrixArray[0] = transformReference.localToWorldMatrix;
            editor_reusableList[0] = editor_frameIndex;
            propertyBlock.SetFloatArray(shader_frameIndex, editor_reusableList);
            editor_reusableList[0] = editor_preFrameIndex;
            propertyBlock.SetFloatArray(shader_preFrameIndex, editor_reusableList);
            editor_reusableList[0] = editor_transition;
            propertyBlock.SetFloatArray(shader_transitionProgress, editor_reusableList);
            Graphics.DrawMeshInstanced(mesh, submeshID, material, editor_matrixArray, 1, propertyBlock, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0, camera, UnityEngine.Rendering.LightProbeUsage.Off);
        }

        internal void ConfigMaterial(Material material)
        {
            material.enableInstancing = true;
            material.SetTexture(shader__boneTexture, animationData.animationTexture);
            material.SetInt(shader__boneTextureWidth, animationData.animationTexture.width);
            material.SetInt(shader__boneTextureHeight, animationData.animationTexture.height);
            material.SetInt(shader__boneTextureBlockWidth, animationData.blockWidth);
            material.SetInt(shader__boneTextureBlockHeight, animationData.blockHeight);
            material.SetInt(shader__matCount, animationData.blockWidth / 4);
            material.SetInt(shader__blockCount, animationData.animationTexture.width / animationData.blockWidth);
            InstancedAnimationHelper.FixMaterialData(material, animationData.bonePerVertex);
        }

        internal void EditorOnly_Render(List<Camera> cameras, Material material)
        {
            if (editor_currentLODLevel == -1 || !material.enableInstancing)
                return;

            editor_matrixArray[0] = transformReference.localToWorldMatrix;
            editor_reusableList[0] = editor_frameIndex;
            propertyBlock.SetFloatArray(shader_frameIndex, editor_reusableList);
            editor_reusableList[0] = editor_preFrameIndex;
            propertyBlock.SetFloatArray(shader_preFrameIndex, editor_reusableList);
            editor_reusableList[0] = editor_transition;
            propertyBlock.SetFloatArray(shader_transitionProgress, editor_reusableList);
            InstancingLODData lod = animationData.LOD[editor_currentLODLevel];
            for (int i = 0; i < lod.instancingMeshData.Length; ++i)
            {
                InstancingMeshData imd = lod.instancingMeshData[i];
                for (int j = 0; j < imd.fixedMaterials.Length; ++j)
                    for (int k = 0; k < cameras.Count; ++k)
                        Graphics.DrawMeshInstanced(imd.mesh, j, material, editor_matrixArray, 1, propertyBlock, curretnLod.shadowsMode[0], lod.receiveShadows[i], lod.layer[i], cameras[k]);
            }
        }

        internal void EditorOnly_RenderOutline(UnityEngine.Rendering.CommandBuffer cb)
        {
            editor_matrixArray[0] = transformReference.localToWorldMatrix;
            editor_reusableList[0] = editor_frameIndex;
            propertyBlock.SetFloatArray(shader_frameIndex, editor_reusableList);
            editor_reusableList[0] = editor_preFrameIndex;
            propertyBlock.SetFloatArray(shader_preFrameIndex, editor_reusableList);
            editor_reusableList[0] = editor_transition;
            propertyBlock.SetFloatArray(shader_transitionProgress, editor_reusableList);
            InstancingLODData lod = animationData.LOD[^1];

            for (int i = 0; i < lod.instancingMeshData.Length; ++i)
            {
                InstancingMeshData imd = lod.instancingMeshData[i];
                for (int j = 0; j < imd.fixedMaterials.Length; ++j)
                    cb.DrawMeshInstanced(imd.mesh, j, lod.instancingMeshData[i].fixedMaterials[j], 0, editor_matrixArray, 1, propertyBlock);
            }
        }

        internal void EditorOnly_RenderOutlinePlayMode(UnityEngine.Rendering.CommandBuffer cb)
        {
            if (InstanceJobId < 0)
                return;
            AnimationTmpHolder smartHolder = InstancedAnimationManager.smartHolder;
            BaseAnimData bad = smartHolder.baseAnimData_[InstanceJobId];
            float preFrameIndex = 0;
            float frameIndex = currentAnimationInfo.animationIndex + bad.curFrame;
            float transition = bad.transitionProgress;
            if (transition < 1f)
                preFrameIndex = previousAnimationInfo.animationIndex + bad.preFrame;

            editor_matrixArray[0] = transformReference.localToWorldMatrix;
            editor_reusableList[0] = frameIndex;
            propertyBlock.SetFloatArray(shader_frameIndex, editor_reusableList);
            editor_reusableList[0] = preFrameIndex;
            propertyBlock.SetFloatArray(shader_preFrameIndex, editor_reusableList);
            editor_reusableList[0] = transition;
            propertyBlock.SetFloatArray(shader_transitionProgress, editor_reusableList);
            InstancingLODData lod = animationData.LOD[^1];
            for (int i = 0; i < lod.instancingMeshData.Length; ++i)
            {
                InstancingMeshData imd = lod.instancingMeshData[i];
                for (int j = 0; j < imd.fixedMaterials.Length; ++j)
                    cb.DrawMeshInstanced(imd.mesh, j, lod.instancingMeshData[i].fixedMaterials[j], 0, editor_matrixArray, 1, propertyBlock);
            }
        }
#endif
        #endregion
    }
}
#endif
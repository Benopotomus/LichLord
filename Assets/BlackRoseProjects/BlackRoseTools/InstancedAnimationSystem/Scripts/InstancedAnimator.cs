#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// Special animator created from Baked Animator Data. Use optimized Animator states and is strictly connected to Instanced Renderer.
    /// </summary>
    public sealed class InstancedAnimator
    {
        #region parameters
        internal InstancedAnimatorData animator;
        private readonly int anyStateTransitionsCount;
        private readonly int[] animations;
        internal readonly float[] parameters;
        private BakedTransition currentTransition;
        private BakedState currentState;

        private InstancedRenderer renderer;

        internal bool anyParaChanged;
        private bool isInit;
        private float cachedSpeed;
        private int stateSpeedMod;

        private BakedTransition[] transitions;
        private BakedTransition[] anyStateTransitions;
        #endregion
        #region Internal management
        internal InstancedAnimator(InstancedRenderer renderer, InstancedAnimatorData bakedAnimationData)
        {
            this.renderer = renderer;
            animator = bakedAnimationData;
            animations = new int[animator.states.Length];
            parameters = new float[animator.parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
                parameters[i] = animator.parameters[i].value;

            anyStateTransitions = animator.anyStateTransitions;
            anyStateTransitionsCount = anyStateTransitions.Length;
        }

        internal void Init()
        {
            for (int i = 0; i < animations.Length; ++i)
                animations[i] = renderer.FindAnimationIndex(animator.states[i].animationName);
            currentState = animator.states[animator.defaultState];
            stateSpeedMod = currentState.speedParameterActive ? currentState.speedParameter : -1;
            currentTransition = null;
            transitions = currentState.transitions;
            renderer.PlayAnimation(animations[currentState.stateID], 0f);
            isInit = true;
            anyParaChanged = true;
        }

        internal void Destroy()
        {
            isInit = false;
            animator = null;
            renderer = null;
            currentTransition = null;
            transitions = null;
            anyStateTransitions = null;
        }

        private void SetState(BakedState state, BakedTransition transition, AnimationTmpHolder data)
        {
            currentTransition = transition;
            currentState = state;
            anyParaChanged = true;
            transitions = currentState.transitions;
            stateSpeedMod = currentState.speedParameterActive ? currentState.speedParameter : -1;
            renderer.PlayAnimation(animations[state.stateID], transition.duration, transition.fixedDuration, data);
            data.baseAnimData.curFrame = transition.offset * renderer.currentAnimationInfo.totalFrame;//apply animation offset
        }

        /// <summary>
        /// Internal update of this animator
        /// </summary>
        internal void Update(AnimationTmpHolder data)
        {
            data.LoadStandardAnimData();

            float frame = data.baseAnimData.curFrame;
            float totalTime = data.standardAnimData.totalFrame;
            float animProgress = frame / totalTime;

            for (int i = 0; i < anyStateTransitionsCount; ++i)
            {
                BakedTransition transition = anyStateTransitions[i];
                if ((anyParaChanged || transition.hasExitTime) && (transition.canTransitToSelf || (transition.targetState != currentState.stateID)) && transition.CheckConditions(this, animProgress))
                {
                    SetState(animator.states[transition.targetState], transition, data);
                    animProgress = 0;
                    break;
                }
            }
            bool skipOtherConditions = currentTransition != null && currentTransition.interruptionSource == TransitionInterraption.None;
            if (!skipOtherConditions)
            {
                for (int i = 0, size = transitions.Length; i < size; ++i)
                {
                    BakedTransition transition = transitions[i];
                    if ((anyParaChanged || transition.hasExitTime) && (transition.canTransitToSelf || (transition.targetState != currentState.stateID)) && transition.CheckConditions(this, animProgress))
                    {
                        SetState(animator.states[transition.targetState], transition, data);
                        break;
                    }
                }
            }
            if (anyParaChanged)
            {
                cachedSpeed = currentState.animationSpeed;
                if (stateSpeedMod >= 0)
                    cachedSpeed *= parameters[stateSpeedMod];
                anyParaChanged = false;
                data.standardAnimData.speedParameter = cachedSpeed;
                data.standardAnimData_modified = 2;
            }

            if (currentTransition != null && data.baseAnimData.transitionProgress >= 1f)
            {//has transition, has exit time
                currentTransition = null;
                anyParaChanged = true;
            }
        }

        /// <summary>
        /// Internal set property value
        /// </summary>
        /// <param name="id">property id</param>
        /// <param name="value">property value</param>
        private void SetPropertyInternal(int id, float value)
        {
            #region BLACKROSE_INSTANCING_SAFETY_CHECKS
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (!isInit)
            {
                Debug.LogError("Animator isn't initialized yet!");
                return;
            }
            if (id < 0 || id >= parameters.Length)
            {
                Debug.LogError("Invalid property id " + id);
                return;
            }
#endif
            #endregion
            if (parameters[id] == value)
                return;
            anyParaChanged = true;
            parameters[id] = value;
        }

        /// <summary>
        /// Internal set property value
        /// </summary>
        /// <param name="name">property name</param>
        /// <param name="value">property value</param>
        private void SetPropertyInternal(string name, float value)
        {
            #region BLACKROSE_INSTANCING_SAFETY_CHECKS
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (!isInit)
            {
                Debug.LogError("Animator isn't initialized yet!");
                return;
            }
#endif
            #endregion
            int id = animator.PropertyToID(name);
            #region BLACKROSE_INSTANCING_SAFETY_CHECKS
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (id < 0 || id >= parameters.Length)
            {
                Debug.LogError("Invalid property name " + name);
                return;
            }
#endif
            #endregion
            if (parameters[id] == value)
                return;
            anyParaChanged = true;
            parameters[id] = value;
        }

        private float GetPropertyInternal(string name)
        {
            #region BLACKROSE_INSTANCING_SAFETY_CHECKS
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (!isInit)
            {
                Debug.LogError("Animator isn't initialized yet!");
                return 0f;
            }
#endif
            #endregion
            int id = animator.PropertyToID(name);
            #region BLACKROSE_INSTANCING_SAFETY_CHECKS
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (id < 0 || id >= parameters.Length)
            {
                Debug.LogError("Invalid property name " + name);
                return 0f;
            }
#endif
            #endregion
            return parameters[id];
        }

        private float GetPropertyInternal(int id)
        {
            #region BLACKROSE_INSTANCING_SAFETY_CHECKS
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (!isInit)
            {
                Debug.LogError("Animator isn't initialized yet!");
                return 0f;
            }
            if (id < 0 || id >= parameters.Length)
            {
                Debug.LogError("Invalid property id " + id);
                return 0f;
            }
#endif
            #endregion
            return parameters[id];
        }

        #endregion

        #region public management
        /// <summary>
        /// Set float property of this animator. For better performance consider use <see cref="SetFloat(int, float)"/>
        /// </summary>
        /// <param name="propertyName">Name of property</param>
        /// <param name="value">Value to set</param>
        public void SetFloat(string propertyName, float value)
        {
            SetPropertyInternal(propertyName, value);
        }

        /// <summary>
        /// Set float property of this animator
        /// </summary>
        /// <param name="id">Property id. Can be extracted by <see cref="PropertyToID(string)"/></param>
        /// <param name="value">Value to set</param>
        public void SetFloat(int id, float value)
        {
            SetPropertyInternal(id, value);
        }

        /// <summary>
        /// Get float value of this animator. For better performance consider use <see cref="GetFloat(int)"/>
        /// </summary>
        /// <param name="propertyName">Name of property</param>
        /// <returns>Value of property</returns>
        public float GetFloat(string propertyName)
        {
            return GetPropertyInternal(propertyName);
        }

        /// <summary>
        /// Get float value of this animator.
        /// </summary>
        /// <param name="id">Property id. Can be extracted by <see cref="PropertyToID(string)"/></param>
        /// <returns>Value of property</returns>
        public float GetFloat(int id)
        {
            return GetPropertyInternal(id);
        }

        /// <summary>
        /// Set int property of this animator. For better performance consider use <see cref="SetInteger(int, int)"/>
        /// </summary>
        /// <param name="propertyName">Name of property</param>
        /// <param name="value">Value to set</param>
        public void SetInteger(string propertyName, int value)
        {
            SetPropertyInternal(propertyName, value);
        }

        /// <summary>
        /// Set int property of this animator
        /// </summary>
        /// <param name="id">Property id. Can be extracted by <see cref="PropertyToID(string)"/></param>
        /// <param name="value">Value to set</param>
        public void SetInteger(int id, int value)
        {
            SetPropertyInternal(id, value);
        }

        /// <summary>
        /// Get int value of this animator. For better performance consider use <see cref="GetInteger(int)"/>
        /// </summary>
        /// <param name="propertyName">Name of property</param>
        /// <returns>Value of property</returns>
        public int GetInteger(string propertyName)
        {
            return (int)GetPropertyInternal(propertyName);
        }

        /// <summary>
        /// Get int value of this animator.
        /// </summary>
        /// <param name="id">Property id. Can be extracted by <see cref="PropertyToID(string)"/></param>
        /// <returns>Value of property</returns>
        public int GetInteger(int id)
        {
            return (int)GetPropertyInternal(id);
        }

        /// <summary>
        /// Set bool property of this animator. For better performance consider use <see cref="SetBool(int, bool)"/>
        /// </summary>
        /// <param name="propertyName">Name of property</param>
        /// <param name="value">Value to set</param>
        public void SetBool(string propertyName, bool value)
        {
            SetPropertyInternal(propertyName, value ? 1f : 0f);
        }

        /// <summary>
        /// Set bool property of this animator.
        /// </summary>
        /// <param name="id">Property id. Can be extracted by <see cref="PropertyToID(string)"/></param>
        /// <param name="value">value to set</param>
        public void SetBool(int id, bool value)
        {
            SetPropertyInternal(id, value ? 1f : 0f);
        }

        /// <summary>
        /// Get bool value of this animator. For better performance consider use <see cref="GetBool(int)"/>
        /// </summary>
        /// <param name="propertyName">Name of property</param>
        /// <returns>Value of property</returns>
        public bool GetBool(string propertyName)
        {
            return GetPropertyInternal(propertyName) == 1f;
        }
        /// <summary>
        /// Get bool value of this animator.
        /// </summary>
        /// <param name="id">Property id. Can be extracted by <see cref="PropertyToID(string)"/></param>
        /// <returns>Value of property</returns>
        public bool GetBool(int id)
        {
            return GetPropertyInternal(id) == 1f;
        }

        /// <summary>
        /// Set Trigger for this animator. For better performance consider use <see cref="SetTrigger(int)"/>
        /// </summary>
        /// <param name="propertyName">Trigger name</param>
        public void SetTrigger(string propertyName)
        {
            SetPropertyInternal(propertyName, 1f);
        }

        /// <summary>
        /// Set Trigger for this animator.
        /// </summary>
        /// <param name="id">Trigger id. Can be extracted by <see cref="PropertyToID(string)"/></param>
        public void SetTrigger(int id)
        {
            SetPropertyInternal(id, 1f);
        }

        /// <summary>
        /// Reset trigger for this animator. For better performance consider use <see cref="ResetTrigger(int)"/>
        /// </summary>
        /// <param name="propertyName">Trigger name</param>
        public void ResetTrigger(string propertyName)
        {
            SetPropertyInternal(propertyName, 0f);
        }

        /// <summary>
        /// Reset trigger for this animator.
        /// </summary>
        /// <param name="id">Trigger id. Can be extracted by <see cref="PropertyToID(string)"/></param>
        public void ResetTrigger(int id)
        {
            SetPropertyInternal(id, 0f);
        }

        /// <summary>
        /// Returns true if animator is currently processing transition
        /// </summary>
        /// <returns>Returns true if there is a transition, false otherwise.</returns>
        public bool IsInTransition()
        {
            return currentTransition != null;
        }

        /// <summary>
        /// Gets count of parameters
        /// </summary>
        public int ParameterCount { get { return parameters.Length; } }

        /// <summary>
        /// Gets if this animator is initialized
        /// </summary>
        public bool IsInitialized { get { return isInit; } }

        /// <summary>
        /// The playback speed of the Animator. 1 is normal playback speed.
        /// <br/>This value is shared with InstancedAnimationRenderer
        /// </summary>
        public float Speed
        {
            get { return renderer.Speed; }
            set { renderer.Speed = value; }
        }

        /// <summary>
        /// Extract property id from property name.
        /// </summary>
        /// <param name="propertyName">Name of property</param>
        /// <returns>Id of property for this baked animator, or -1 if no property match name</returns>
        public int PropertyToID(string propertyName)
        {
            #region BLACKROSE_INSTANCING_SAFETY_CHECKS
#if BLACKROSE_INSTANCING_SAFETY_CHECKS
            if (!isInit)
            {
                Debug.LogError("Animator isn't initialized yet!");
                return -1;
            }
#endif
            #endregion
            return animator.PropertyToID(propertyName);
        }
        #endregion
        #region UUNITY_EDITOR
#if UNITY_EDITOR
        internal void PrintInspector()
        {
            if (!isInit)
                return;

            InstancedAnimationHelper.StringField("State:", currentState.animationName, 150);
            InstancedAnimationHelper.StringField("StateID:", currentState.stateID.ToString(), 150);
            if (currentTransition != null)
                InstancedAnimationHelper.StringField("Transition to:", animator.states[currentTransition.targetState].animationName, 150);
            else
                InstancedAnimationHelper.StringField("Transition to:", "none", 150);
            UnityEditor.EditorGUILayout.LabelField("Parameters:");
            UnityEditor.EditorGUI.indentLevel++;
            for (int i = 0; i < parameters.Length; ++i)
            {
                switch (animator.parameters[i].type)
                {
                    case AnimatorControllerParameterType.Bool:
                        InstancedAnimationHelper.ToggleField(animator.parameters[i].name, parameters[i] == 1, 120);
                        break;
                    case AnimatorControllerParameterType.Float:
                        InstancedAnimationHelper.FloatField(animator.parameters[i].name, parameters[i], 120);
                        break;
                    case AnimatorControllerParameterType.Int:
                        InstancedAnimationHelper.IntField(animator.parameters[i].name, (int)parameters[i]);
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        InstancedAnimationHelper.ToggleField(animator.parameters[i].name, parameters[i] == 1, 120);
                        break;
                }
            }
            UnityEditor.EditorGUI.indentLevel--;
        }
#endif
        #endregion
    }
}
#endif
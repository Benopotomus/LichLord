#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System;
using UnityEngine;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    /// <summary>
    /// Scriptable Object to store Baked Animator data
    /// </summary>
    [Icon("Assets/BlackRoseProjects/BlackRoseTools/InstancedAnimationSystem/Editor/Icons/Icon_BakedAnimator.png")]
    [HelpURL("http://docs.blackrosetools.com/InstancedAnimations/html/class_black_rose_projects_1_1_instanced_animation_system_1_1_instanced_animator_data.html")]
    public class InstancedAnimatorData : ScriptableObject
    {
        [SerializeField] internal int defaultState;
        [SerializeField] internal BakedState[] states;

        [SerializeField] internal BakedTransition[] anyStateTransitions;
        [SerializeField] internal BakedParameters[] parameters;

        /// <summary>
        /// Get id of property from this baked animator
        /// </summary>
        /// <param name="propertyName">Name of property</param>
        /// <returns>Id of property or -1 if not found property of given name</returns>
        public int PropertyToID(string propertyName)
        {
            for (int i = 0; i < parameters.Length; ++i)
                if (parameters[i].name == propertyName)
                    return i;
            return -1;
        }
    }

    [Serializable]
    internal class BakedCondition
    {
        [SerializeField] public int parameterID;
        [SerializeField] public float value;
        [SerializeField] public ConditionMode condition;

        internal bool Process(InstancedAnimator animator)
        {
            float v = animator.parameters[parameterID];
            switch (condition)
            {
                case ConditionMode.If://bool
                    return v == 1f;
                case ConditionMode.IfNot://bool
                    return v == 0f;
                case ConditionMode.Trigger://trigger
                    return v == 1f;
                case ConditionMode.Greater://float
                    return v > value;
                case ConditionMode.Less://float
                    return v <= value;
                case ConditionMode.Equals:
                    return v == value;
                case ConditionMode.NotEqual:
                    return v != value;
            }
            return false;
        }

        internal void ResetTriggers(InstancedAnimator animator)
        {
            if (condition == ConditionMode.Trigger)
                animator.parameters[parameterID] = 0f;
        }
    }

    [Serializable]
    internal class BakedParameters
    {
        [SerializeField] public string name;
        [SerializeField] public float value;
        [SerializeField] public AnimatorControllerParameterType type;
    }

    [Serializable]
    internal class BakedState
    {
        [SerializeField] public string animationName;
        [SerializeField] public int stateID;
        [SerializeField] public bool speedParameterActive;
        [SerializeField] public int speedParameter;
        [SerializeField] public float animationSpeed;

        [SerializeField] public BakedTransition[] transitions;
    }

    [Serializable]
    internal class BakedTransition
    {
        [SerializeField] public int targetState;
        [SerializeField] public float duration;
        [SerializeField] public float offset;
        [SerializeField] public bool fixedDuration;

        [SerializeField] public bool hasExitTime;
        [SerializeField] public float exitTime;

        [SerializeField] public bool canTransitToSelf;

        [SerializeField] public TransitionInterraption interruptionSource;

        [SerializeField] public BakedCondition[] conditions;
        [SerializeField] public int[] triggerConditions;

        internal bool CheckConditions(InstancedAnimator animator, float animProgress)
        {
            if (hasExitTime && animProgress < exitTime)
                return false;
            for (int i = 0; i < conditions.Length; ++i)
                if (!conditions[i].Process(animator))
                    return false;
            for (int i = 0; i < triggerConditions.Length; ++i)
                conditions[triggerConditions[i]].ResetTriggers(animator);
            return true;
        }
    }

    [Serializable]
    internal enum ConditionMode
    {
        If = 1,
        IfNot = 2,
        Greater = 3,
        Less = 4,
        Equals = 6,
        NotEqual = 7,
        Trigger = 8//custom
    }

    [Serializable]
    internal enum TransitionInterraption
    {
        None = 0,
        Source = 1,
        Destination = 2,
        SourceThenDestination = 3,
        DestinationThenSource = 4
    }
}
#endif
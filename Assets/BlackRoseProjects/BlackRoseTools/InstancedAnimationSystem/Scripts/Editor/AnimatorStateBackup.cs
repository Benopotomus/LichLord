#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using UnityEditor.Animations;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class AnimatorStateBackup
    {
        private AnimatorState state;
        private float speed;
        private bool cycleOffsetParameterActive;
        private bool mirrorParameterActive;
        private bool speedParameterActive;
        private bool timeParameterActive;

        public AnimatorStateBackup(AnimatorState state)
        {
            this.state = state;
            this.speed = state.speed;
            this.cycleOffsetParameterActive = state.cycleOffsetParameterActive;
            this.mirrorParameterActive = state.mirrorParameterActive;
            this.speedParameterActive = state.speedParameterActive;
            this.timeParameterActive = state.timeParameterActive;


            state.speed = 1f;
            state.cycleOffsetParameterActive = false;
            state.mirrorParameterActive = false;
            state.speedParameterActive = false;
            state.timeParameterActive = false;
        }

        public void Revert()
        {
            state.speed = this.speed;
            state.cycleOffsetParameterActive = this.cycleOffsetParameterActive;
            state.mirrorParameterActive = this.mirrorParameterActive;
            state.speedParameterActive = this.speedParameterActive;
            state.timeParameterActive = this.timeParameterActive;
        }
    }
}
#endif
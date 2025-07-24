#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using Unity.Collections;

namespace BlackRoseProjects.InstancedAnimationSystem
{
    internal class AnimationTmpHolder
    {
        internal BaseAnimData baseAnimData;
        internal TransitionAnimData transitionAnimData;
        internal StandardAnimData standardAnimData;

        internal byte transitionAnimData_modified;
        internal byte standardAnimData_modified;
        internal int instanceID;

        internal NativeArray<BaseAnimData> baseAnimData_native;
        internal NativeArray<TransitionAnimData> transitionAnimData_native;
        internal NativeArray<StandardAnimData> standardAnimData_native;

        internal BaseAnimData[] baseAnimData_;
        internal TransitionAnimData[] transitionAnimData_;
        internal StandardAnimData[] standardAnimData_;

        public AnimationTmpHolder(ref NativeArray<BaseAnimData> bad, ref NativeArray<TransitionAnimData> tad, ref NativeArray<StandardAnimData> sad)
        {
            baseAnimData_native = bad;
            transitionAnimData_native = tad;
            standardAnimData_native = sad;

            baseAnimData_ = new BaseAnimData[baseAnimData_native.Length];
            transitionAnimData_ = new TransitionAnimData[transitionAnimData_native.Length];
            standardAnimData_ = new StandardAnimData[standardAnimData_native.Length];
        }

        internal void CopyData(int size)
        {
            baseAnimData_native.CopyToFast(baseAnimData_, size);
            transitionAnimData_native.CopyToFast(transitionAnimData_, size);
            standardAnimData_native.CopyToFast(standardAnimData_, size);
        }

        internal void CopyDataBack(int size)
        {
            baseAnimData_native.CopyFromFast(baseAnimData_, size);
            transitionAnimData_native.CopyFromFast(transitionAnimData_, size);
            standardAnimData_native.CopyFromFast(standardAnimData_, size);
        }

        internal void LoadTransitionAnimData()
        {
            if (transitionAnimData_modified == 0)
            {
                transitionAnimData_modified = 1;
                transitionAnimData = transitionAnimData_[instanceID];
            }
        }

        internal void LoadStandardAnimData()
        {
            if (standardAnimData_modified == 0)
            {
                standardAnimData_modified = 1;
                standardAnimData = standardAnimData_[instanceID];
            }
        }

        internal void LoadBaseAnimData()
        {
            baseAnimData = baseAnimData_[instanceID];
        }

        internal void LoadDataFromNative()
        {
            baseAnimData = baseAnimData_native[instanceID];
            transitionAnimData = transitionAnimData_native[instanceID];
            standardAnimData = standardAnimData_native[instanceID];
        }

        internal void WriteDataToNative()
        {
            baseAnimData_native[instanceID] = baseAnimData;
            transitionAnimData_native[instanceID] = transitionAnimData;
            standardAnimData_native[instanceID] = standardAnimData;
        }

        internal void SaveData()
        {
            baseAnimData_[instanceID] = baseAnimData;

            if (standardAnimData_modified == 2) standardAnimData_[instanceID] = standardAnimData;
            if (transitionAnimData_modified == 2) transitionAnimData_[instanceID] = transitionAnimData;
        }

        internal void Clear()
        {
            transitionAnimData_modified = standardAnimData_modified = 0;
        }
    }

    internal struct BaseAnimData
    {
        internal float transitionProgress;
        internal float curFrame;
        internal float preFrame;
        internal float aniIndex;
        internal float globalSpeed;
        internal byte cullMode;
    }

    internal struct TransitionAnimData
    {
        internal float transitionTimer;
        internal float transitionDuration;
        internal float preIndex;
        internal float preSpeedParameter;
        internal float fps;
        internal int totalFrame;
        internal byte preWrapMode;
    }

    internal struct StandardAnimData
    {
        internal float speedParameter;
        internal float fps;
        internal int totalFrame;
        internal byte wrapMode;
    }
}
#endif
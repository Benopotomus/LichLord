#if BLACKROSE_INSTANCING_MATH && BLACKROSE_INSTANCING_COLLECTIONS
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace BlackRoseProjects.InstancedAnimationSystem
{
#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct MoveAllByOffset : IJobParallelForTransform
    {
        [ReadOnly] public Vector3 offset;
        public void Execute(int index, TransformAccess transform)
        {
            transform.position += offset;
        }
    }

#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct RotateAllByOffset : IJobParallelForTransform
    {
        [ReadOnly] public Quaternion offset;
        public void Execute(int index, TransformAccess transform)
        {
            transform.rotation = offset;
        }
    }

#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct ScaleAllByOffset : IJobParallelForTransform
    {
        [ReadOnly] public Vector3 offset;
        public void Execute(int index, TransformAccess transform)
        {
            transform.localScale += offset;
        }
    }

#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct BindMeshAttachment : IJobParallelFor
    {
        [ReadOnly] public Vector3 positionOffset;
        [ReadOnly] public Quaternion rotationOffset;
        [ReadOnly] public Vector3 scale;
        public NativeArray<Vector3> vertex;

        public void Execute(int index)
        {
            Vector3 vert = vertex[index];

            vert.x *= scale.x;
            vert.y *= scale.y;
            vert.z *= scale.z;

            vert = rotationOffset * vert;
            vert += positionOffset;
            vertex[index] = vert;
        }
    }

#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct GenerateMeshBakedRig : IJobParallelFor
    {
        [ReadOnly] public int bone;
        [WriteOnly] public NativeArray<Vector4> boneIndex;
        [WriteOnly] public NativeArray<Color> colors;

        public void Execute(int index)
        {
            boneIndex[index] = new Vector4(bone, 0, 0, 0);
            colors[index] = new Color(1f, -0.1f, -0.1f, -0.1f);
        }
    }

#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct ConvertAttachmentNormal : IJobParallelFor
    {
        [ReadOnly] public Quaternion rotationOffset;
        public NativeArray<Vector3> normal;
        public void Execute(int index)
        {
            normal[index] = rotationOffset * normal[index];
        }
    }

    #region Editor Jobs
#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct RaycastJobSelection : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Matrix4x4> matrix;
        [ReadOnly] public NativeArray<float> unscaledSized;
        [ReadOnly] public NativeArray<float3> offsets;
        [ReadOnly] public float4 p0;
        [ReadOnly] public float4 p1;
        [ReadOnly] public float4 p2;
        [ReadOnly] public float4 p3;
        [ReadOnly] public float4 p4;
        [ReadOnly] public float4 p5;

        [WriteOnly] public NativeArray<bool> output;

        public void Execute(int index)
        {
            Matrix4x4 mat = matrix[index];
            float3 pos = mat.GetPosition();
            Vector3 lossyScale = mat.lossyScale;
            float3 unscalledOffset = offsets[index];
            unscalledOffset.x *= lossyScale.x;
            unscalledOffset.y *= lossyScale.y;
            unscalledOffset.z *= lossyScale.z;
            pos += InstancedAnimationHelper.Rotate(mat, unscalledOffset);
            // pos += unscalledOffset;
            float size = lossyScale.FlatScale() * unscaledSized[index] * 0.75f;

            output[index] = false;
            float3 tmpPos = new float3();
            if (CalcualteSingleFrustum(pos, size, ref tmpPos, p0)) return;
            if (CalcualteSingleFrustum(pos, size, ref tmpPos, p1)) return;
            if (CalcualteSingleFrustum(pos, size, ref tmpPos, p2)) return;
            if (CalcualteSingleFrustum(pos, size, ref tmpPos, p3)) return;
            if (CalcualteSingleFrustum(pos, size, ref tmpPos, p4)) return;
            if (CalcualteSingleFrustum(pos, size, ref tmpPos, p5)) return;
            output[index] = true;
        }

        bool CalcualteSingleFrustum(float3 position, float size, ref float3 pos, float4 holder)
        {
            pos.x = holder.x > 0 ? position.x + size : position.x - size;
            pos.y = holder.y > 0 ? position.y + size : position.y - size;
            pos.z = holder.z > 0 ? position.z + size : position.z - size;

            if ((math.dot(holder.xyz, pos) + holder.w) < 0)
                return true;
            return false;
        }


        bool CalcualteSingleFrustum(float3 position, float4 holder)
        {
            if ((math.dot(holder.xyz, position) + holder.w) < 0)
                return true;
            return false;
        }
    }


#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct RaycastJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Matrix4x4> matrix;
        [ReadOnly] public NativeArray<float> unscaledSized;
        [ReadOnly] public NativeArray<float3> offsets;
        [ReadOnly] public float3 origin;
        [ReadOnly] public float3 destination;

        [NativeDisableParallelForRestriction] public NativeArray<int> output;
        [NativeSetThreadIndex] int thread;

        public void Execute(int index)
        {
            if (output[thread] >= 0)
                return;
            Matrix4x4 mat = matrix[index];
            float3 pos = mat.GetPosition();
            Vector3 lossyScale = mat.lossyScale;
            float3 unscalledOffset = offsets[index];
            unscalledOffset.x *= lossyScale.x;
            unscalledOffset.y *= lossyScale.y;
            unscalledOffset.z *= lossyScale.z;
            pos += InstancedAnimationHelper.Rotate(mat, unscalledOffset);
            // pos += unscalledOffset;

            //float3 pos = matrix[index].GetPosition();
            float3 bestA = ClosestPointOnLineSegment(origin, destination, pos);

            float distToPoint = math.distancesq(bestA, pos);
            float size = matrix[index].lossyScale.FlatScale() * (unscaledSized[index] * 0.75f);
            size *= size;

            bool isHit = size >= distToPoint;
            if (isHit)
                output[thread] = index;
        }

        float3 ClosestPointOnLineSegment(float3 A, float3 B, float3 Point)
        {
            float3 AB = B - A;
            float t = math.dot(Point - A, AB) / math.dot(AB, AB);
            return A + math.saturate(t) * AB;
        }
    }
    #endregion

#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct UpdateAnimJob : IJobParallelFor
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public NativeArray<byte> visabilityArray;
        public NativeArray<BaseAnimData> baseAnimDataArray;
        public NativeArray<StandardAnimData> standardAnimDataArray;

#if BLACKROSE_INSTANCING_BURST
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        public void Execute(int index)
        {
            StandardAnimData sad = standardAnimDataArray[index];
            float speed = sad.speedParameter * deltaTime;
            if (speed == 0f)
                return;
            BaseAnimData bad = baseAnimDataArray[index];
            speed *= bad.globalSpeed;
            if (speed == 0f || (bad.cullMode == 1 && visabilityArray[index] > 3))
                return;

            float curFrame = bad.curFrame;
            curFrame += speed * sad.fps;
            int totalFrame = sad.totalFrame;
            switch (sad.wrapMode)
            {
                case (byte)WrapMode.Loop:
                    {
                        if (curFrame < 0f) curFrame += (totalFrame - 1);
                        else if (curFrame > totalFrame - 1) curFrame -= (totalFrame - 1);
                        break;
                    }
                case (byte)WrapMode.PingPong:
                    {
                        if (curFrame < 0f)
                        {
                            sad.speedParameter = math.abs(sad.speedParameter);
                            curFrame = math.abs(curFrame);
                        }
                        else if (curFrame > totalFrame - 1)
                        {
                            sad.speedParameter = -math.abs(sad.speedParameter);
                            curFrame = 2 * (totalFrame - 1) - curFrame;
                        }
                        break;
                    }
                case (byte)WrapMode.Default:
                case (byte)WrapMode.Once:
                    {
                        if (curFrame < 0f || curFrame > totalFrame - 1.0f)
                        {
                            sad.speedParameter = 0f;
                        }
                        break;
                    }
            }
            curFrame = math.clamp(curFrame, 0f, totalFrame - 1);
            bad.curFrame = curFrame;

            baseAnimDataArray[index] = bad;
            standardAnimDataArray[index] = sad;
        }
    }
#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct UpdateTransitionsJob : IJobParallelFor
    {
        [ReadOnly] public float deltaTime;
        [ReadOnly] public NativeArray<byte> visabilityArray;
        public NativeArray<BaseAnimData> baseAnimDataArray;
        public NativeArray<TransitionAnimData> transitionAnimDataArray;

#if BLACKROSE_INSTANCING_BURST
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        public void Execute(int index)
        {
            BaseAnimData baseAnimData = baseAnimDataArray[index];
            if (baseAnimData.transitionProgress >= 1)
                return;
            float transSpeed = deltaTime * baseAnimData.globalSpeed;
            if (transSpeed == 0f || (baseAnimData.cullMode == 2 && visabilityArray[index] > 3))
                return;

            TransitionAnimData transitionAnimData = transitionAnimDataArray[index];
            transitionAnimData.transitionTimer += transSpeed;
            float weight = transitionAnimData.transitionTimer / transitionAnimData.transitionDuration;
            baseAnimData.transitionProgress = math.min(weight, 1f);
            if (baseAnimData.transitionProgress >= 1f)
            {
                transitionAnimData.preIndex = -1;
                baseAnimData.preFrame = -1;
            }
            else
            {
                float preFrame = baseAnimData.preFrame;
                float preSpeed = transitionAnimData.preSpeedParameter * deltaTime;
                preFrame += preSpeed * transitionAnimData.fps;
                float totalPreFrame = transitionAnimData.totalFrame - 1f;
                switch (transitionAnimData.preWrapMode)
                {
                    case (byte)WrapMode.Loop:
                        {
                            if (preFrame < 0f)
                                preFrame += totalPreFrame;
                            else if (preFrame > totalPreFrame)
                                preFrame -= totalPreFrame;
                            break;
                        }
                    case (byte)WrapMode.Default:
                    case (byte)WrapMode.Once:
                        {
                            if (preFrame < 0f || preFrame > totalPreFrame)
                                preFrame = totalPreFrame;
                            break;
                        }
                }
                baseAnimData.preFrame = preFrame;
            }
            baseAnimDataArray[index] = baseAnimData;
            transitionAnimDataArray[index] = transitionAnimData;
        }
    }


#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct FillMatricesJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<Matrix4x4> matrix;
        [ReadOnly] public NativeArray<Matrix4x4> staticMatrices;

        public void Execute(int index, TransformAccess transform)
        {
            if (transform.isValid)
                matrix[index] = transform.localToWorldMatrix;
            else
                matrix[index] = staticMatrices[index];
        }
    }

#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct CalculateMeshCenterWithPose : IJob
    {
        [ReadOnly] public NativeArray<Matrix4x4> boneMatrices;
        [ReadOnly] public NativeArray<Vector3> verticles;
        [ReadOnly] public NativeArray<BoneWeight> boneWeights;
        [WriteOnly] public NativeArray<float4> result;

        public void Execute()
        {
            float3 max = new float3(float.MinValue, float.MinValue, float.MinValue);
            float3 min = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            for (int i = 0; i < verticles.Length; ++i)
            {
                BoneWeight weight = boneWeights[i];

                Matrix4x4 bm0 = boneMatrices[weight.boneIndex0];
                Matrix4x4 bm1 = boneMatrices[weight.boneIndex1];
                Matrix4x4 bm2 = boneMatrices[weight.boneIndex2];
                Matrix4x4 bm3 = boneMatrices[weight.boneIndex3];

                Matrix4x4 vertexMatrix = new Matrix4x4();

                for (int n = 0; n < 16; n++)
                {
                    vertexMatrix[n] =
                        bm0[n] * weight.weight0 +
                        bm1[n] * weight.weight1 +
                        bm2[n] * weight.weight2 +
                        bm3[n] * weight.weight3;
                }
                Vector3 pos = vertexMatrix.MultiplyPoint3x4(verticles[i]);

                if (pos.x > max.x)
                    max.x = pos.x;
                if (pos.x < min.x)
                    min.x = pos.x;

                if (pos.y > max.y)
                    max.y = pos.y;
                if (pos.y < min.y)
                    min.y = pos.y;

                if (pos.z > max.z)
                    max.z = pos.z;
                if (pos.z < min.z)
                    min.z = pos.z;
            }

            float3 center = new float3(math.lerp(max.x, min.x, 0.5f), math.lerp(max.y, min.y, 0.5f), math.lerp(max.z, min.z, 0.5f));
            float3 distances = new float3(math.distance(center.x, max.x), math.distance(center.y, max.y), math.distance(center.z, max.z));
            float distance = math.max(math.max(distances.x, distances.y), distances.z);
            result[0] = new float4(center.x, center.y, center.z, distance);
        }
    }

#if BLACKROSE_INSTANCING_BURST
    [Unity.Burst.BurstCompile]
#endif
    internal struct UpdateCullingJob : IJobParallelFor
    {
        const byte NotVisable = byte.MaxValue;

        [ReadOnly] public float deltaTime;
        [ReadOnly] public float3 cameraPos;
        [ReadOnly] public float lodBias;

        [ReadOnly] public float4 frustumPlanes0;
        [ReadOnly] public float4 frustumPlanes1;
        [ReadOnly] public float4 frustumPlanes2;
        [ReadOnly] public float4 frustumPlanes3;
        [ReadOnly] public float4 frustumPlanes4;
        [ReadOnly] public float4 frustumPlanes5;

        [ReadOnly] public NativeArray<Matrix4x4> matrices;
        [ReadOnly] public NativeArray<float> unscaledSized;
        [ReadOnly] public NativeArray<float3> unscaledOffsets;
        [ReadOnly] public NativeArray<float4> LodHeights;

        [WriteOnly] public NativeArray<byte> visabilities;

#if BLACKROSE_INSTANCING_BURST
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        public void Execute(int index)
        {
            Matrix4x4 matrix = matrices[index];
            float3 position = matrix.GetPosition();
            Vector3 lossyScale = matrix.lossyScale;
            float3 unscalledOffset = unscaledOffsets[index];
            unscalledOffset.x *= lossyScale.x;
            unscalledOffset.y *= lossyScale.y;
            unscalledOffset.z *= lossyScale.z;

            position += InstancedAnimationHelper.Rotate(matrix, unscalledOffset);
            float flatScale = lossyScale.FlatScale();
            float scaledScale = flatScale * unscaledSized[index] * 0.75f;
            byte visable;
            if (!CalculateCameraCulling(position, scaledScale))
            {//is not visable
                visable = NotVisable;
            }
            else
            {//is visable, calculate lod for check visability
                visable = CalculateLod(position, LodHeights[index], flatScale);
            }
            visabilities[index] = visable;
        }

#if BLACKROSE_INSTANCING_BURST
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        byte CalculateLod(float3 position, float4 lodScales, float scale)
        {
            if (lodScales.x == -1)
                return 0;
            float sqrLength = math.distancesq(cameraPos, position) / lodBias;
            if (Pow(scale * lodScales.x) > sqrLength) return 0;
            if (Pow(scale * lodScales.y) > sqrLength) return 1;
            if (Pow(scale * lodScales.z) > sqrLength) return 2;
            if (Pow(scale * lodScales.w) > sqrLength) return 3;
            return NotVisable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        float Pow(float v1)
        {
            return v1 * v1;
        }

#if BLACKROSE_INSTANCING_BURST
        [Unity.Burst.CompilerServices.SkipLocalsInit]
#endif
        bool CalculateCameraCulling(float3 position, float size)
        {
            if (CalcualteSingleFrustum(position, size, frustumPlanes0)) return false;
            if (CalcualteSingleFrustum(position, size, frustumPlanes1)) return false;
            if (CalcualteSingleFrustum(position, size, frustumPlanes2)) return false;
            if (CalcualteSingleFrustum(position, size, frustumPlanes3)) return false;
            if (CalcualteSingleFrustum(position, size, frustumPlanes4)) return false;
            if (CalcualteSingleFrustum(position, size, frustumPlanes5)) return false;
            return true;
        }

        [Unity.Burst.CompilerServices.SkipLocalsInit]
        bool CalcualteSingleFrustum(float3 position, float size, float4 holder)
        {
            float3 pos;
            pos.x = holder.x > 0 ? position.x + size : position.x - size;
            pos.y = holder.y > 0 ? position.y + size : position.y - size;
            pos.z = holder.z > 0 ? position.z + size : position.z - size;

            if ((math.dot(holder.xyz, pos) + holder.w) < 0)
                return true;
            return false;
        }
    }
}
#endif
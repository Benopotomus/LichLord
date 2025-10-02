using Fusion;
using LichLord.Props;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord.World
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FStaticPropPosition : INetworkStruct
    {
        [FieldOffset(0)]
        public FChunkPosition ChunkID;
        [FieldOffset(2)]
        public ushort Index;

        public bool IsValid()
        {
            if (ChunkID.X < 0)
                return false;

            if (ChunkID.Y < 0)
                return false;

            return true;
        }

        public bool IsEqual(FStaticPropPosition other)
        {
            if (ChunkID.X == other.ChunkID.X &&
                ChunkID.Y == other.ChunkID.Y &&
                Index == other.Index)
                return true;

            return false;
        }

        public void Copy(FStaticPropPosition other)
        {
            this.ChunkID = other.ChunkID;
            this.Index = other.Index;
        }

        public PropRuntimeState GetPropRuntimeState(SceneContext context, bool hasAuthority)
        {
            Chunk chunk = context.ChunkManager.GetChunk(ChunkID);
            if (chunk != null && chunk.GetRenderState(hasAuthority, Index, out var state))
            {
                return state;
            }

            return null;
        }

        public Vector3 GetPosition(SceneContext context, bool hasAuthority)
        {
            var runtimeState =  GetPropRuntimeState(context, hasAuthority);
            if (runtimeState == null)
                return Vector3.zero;

            return runtimeState.position;
        }
    }
}

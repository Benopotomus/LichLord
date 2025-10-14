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
        public FChunkPosition ChunkPosition;
        [FieldOffset(2)]
        public ushort PropIndex;

        public bool IsValid()
        {
            if (ChunkPosition.X < 0)
                return false;

            if (ChunkPosition.Y < 0)
                return false;

            return true;
        }

        public bool IsEqual(FStaticPropPosition other)
        {
            if (ChunkPosition.X == other.ChunkPosition.X &&
                ChunkPosition.Y == other.ChunkPosition.Y &&
                PropIndex == other.PropIndex)
                return true;

            return false;
        }

        public void Copy(FStaticPropPosition other)
        {
            this.ChunkPosition = other.ChunkPosition;
            this.PropIndex = other.PropIndex;
        }

        public PropRuntimeState GetPropRuntimeState(SceneContext context, bool hasAuthority)
        {
            Chunk chunk = context.ChunkManager.GetChunk(ChunkPosition);
            if (chunk != null && chunk.GetRenderState(hasAuthority, PropIndex, out var state))
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

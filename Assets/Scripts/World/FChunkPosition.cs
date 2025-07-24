using Fusion;
using System;
using System.Runtime.InteropServices;

namespace LichLord.World
{
    [StructLayout(LayoutKind.Explicit)]
    [Serializable]
    public struct FChunkPosition : INetworkStruct
    {
        [FieldOffset(0)]
        public byte X;
        [FieldOffset(1)]
        public byte Y;

        public bool IsEqual(ref FChunkPosition other)
        { 
            if(X != other.X || Y != other.Y)
                return false;

            return true;
        }

        // Override GetHashCode for consistency with Equals
        public override int GetHashCode()
        {
            return (X << 8) | (Y & 0xFF);
        }
    }
}
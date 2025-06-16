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
        public sbyte X;
        [FieldOffset(1)]
        public sbyte Y;

        // Equality operator
        public static bool operator ==(FChunkPosition a, FChunkPosition b)
        {
            return a.X == b.X && a.Y == b.Y;
        }

        // Inequality operator
        public static bool operator !=(FChunkPosition a, FChunkPosition b)
        {
            return !(a == b);
        }

        // Override Equals for object comparison
        public override bool Equals(object obj)
        {
            if (obj is FChunkPosition other)
            {
                return this == other;
            }
            return false;
        }

        // Override GetHashCode for consistency with Equals
        public override int GetHashCode()
        {
            return (X << 8) | (Y & 0xFF);
        }
    }
}
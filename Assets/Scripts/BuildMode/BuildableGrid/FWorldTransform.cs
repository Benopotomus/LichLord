using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord.Buildables
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FWorldTransform : INetworkStruct
    {
        [FieldOffset(0)]
        private Vector3Compressed _position; // 12 bytes
        [FieldOffset(12)]
        private ushort _compressedRotation; // 2 bytes (octahedral encoding)
        // 14

        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        public Vector3 Forward
        {
            get => DecodeOctahedral(_compressedRotation);
            set => _compressedRotation = EncodeOctahedral(value);
        }

        public Quaternion Rotation
        {
            get
            {
                // Convert forward direction to quaternion (assume up is world Y)
                Vector3 forward = Forward;
                return Quaternion.LookRotation(forward, Vector3.up);
            }
            set
            {
                // Extract forward direction from quaternion
                Forward = value * Vector3.forward;
            }
        }

        public bool IsPositionEqual(ref FWorldTransform other)
        {
            return _position.X == other._position.X && _position.Y == other._position.Y;
        }

        public bool IsRotationEqual(ref FWorldTransform other)
        {
            return _compressedRotation == other._compressedRotation;
        }

        public void CopyPosition(ref FWorldTransform other)
        {
            _position = other._position;
        }

        public void CopyRotation(ref FWorldTransform other)
        {
            _compressedRotation = other._compressedRotation;
        }

        private static ushort EncodeOctahedral(Vector3 normal)
        {
            // Normalize input
            normal = normal.normalized;

            // Project to octahedron
            normal /= Mathf.Abs(normal.x) + Mathf.Abs(normal.y) + Mathf.Abs(normal.z);
            float u, v;
            if (normal.z >= 0f)
            {
                u = normal.x;
                v = normal.y;
            }
            else
            {
                u = (1f - Mathf.Abs(normal.y)) * Mathf.Sign(normal.x);
                v = (1f - Mathf.Abs(normal.x)) * Mathf.Sign(normal.y);
            }

            // Map u, v from [-1, 1] to [0, 255]
            byte uByte = (byte)Mathf.RoundToInt((u * 0.5f + 0.5f) * 255f);
            byte vByte = (byte)Mathf.RoundToInt((v * 0.5f + 0.5f) * 255f);

            // Pack into 16 bits
            return (ushort)((vByte << 8) | uByte);
        }

        private static Vector3 DecodeOctahedral(ushort encoded)
        {
            // Unpack u, v
            byte uByte = (byte)(encoded & 0xFF);
            byte vByte = (byte)(encoded >> 8);
            float u = (uByte / 255f) * 2f - 1f;
            float v = (vByte / 255f) * 2f - 1f;

            // Reconstruct normal
            float z = 1f - Mathf.Abs(u) - Mathf.Abs(v);
            Vector3 normal;
            if (z >= 0f)
            {
                normal = new Vector3(u, v, z);
            }
            else
            {
                normal = new Vector3(
                    (1f - Mathf.Abs(v)) * Mathf.Sign(u),
                    (1f - Mathf.Abs(u)) * Mathf.Sign(v),
                    z
                );
            }

            return normal.normalized;
        }

    }
}

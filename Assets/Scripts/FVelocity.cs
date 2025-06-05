using Fusion;
using UnityEngine;

namespace LichLord
{
    public struct FVelocity : INetworkStruct
    {
        private ushort _compressedDirection; // 2 bytes (octahedral encoding for direction)
        private ushort _compressedMagnitude; // 2 bytes (magnitude with 0.01 precision)

        private const float MAX_MAGNITUDE = 100f;
        private const float MAGNITUDE_PRECISION = 0.01f;
        private const int MAX_MAGNITUDE_VALUE = (int)(MAX_MAGNITUDE / MAGNITUDE_PRECISION); // 10,000

        public Vector3 Velocity
        {
            get
            {
                if (_compressedMagnitude == 0)
                {
                    return Vector3.zero; // Zero velocity
                }

                // Decode direction
                Vector3 direction = DecodeOctahedral(_compressedDirection);
                // Decode magnitude (scale from [0,10000] to [0,100])
                float magnitude = (_compressedMagnitude / (float)MAX_MAGNITUDE_VALUE) * MAX_MAGNITUDE;
                return direction * magnitude;
            }
            set
            {
                if (value.sqrMagnitude < 0.0001f) // Handle zero velocity
                {
                    _compressedDirection = 0;
                    _compressedMagnitude = 0;
                    return;
                }

                // Extract and clamp magnitude
                float magnitude = Mathf.Min(value.magnitude, MAX_MAGNITUDE);
                // Encode magnitude (scale to [0,10000] for 0.01 precision)
                _compressedMagnitude = (ushort)Mathf.RoundToInt((magnitude / MAX_MAGNITUDE) * MAX_MAGNITUDE_VALUE);
                // Encode direction
                _compressedDirection = EncodeOctahedral(value.normalized);
            }
        }

        public bool IsVelocityEqual(ref FVelocity other)
        {
            return _compressedDirection == other._compressedDirection &&
                   _compressedMagnitude == other._compressedMagnitude;
        }

        public void CopyVelocity(ref FVelocity other)
        {
            _compressedDirection = other._compressedDirection;
            _compressedMagnitude = other._compressedMagnitude;
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
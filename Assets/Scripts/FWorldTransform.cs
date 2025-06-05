using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FWorldTransform : INetworkStruct
    {
        [FieldOffset(0)]
        private float _positionX; // 4 bytes
        [FieldOffset(4)]
        private short _positionYCompressed; // 2 bytes (scaled to -327.68 to 327.67)
        [FieldOffset(6)]
        private float _positionZ; // 4 bytes
        [FieldOffset(10)]
        private ushort _compressedRotation; // 2 bytes (octahedral encoding)
        // Total: 12 bytes

        private const float Y_SCALE_FACTOR = 100f; // For two decimal places
        private const float Y_MIN = -327.68f;
        private const float Y_MAX = 327.67f;
        private const short Y_MIN_RAW = -32768;
        private const short Y_MAX_RAW = 32767;

        public Vector3 Position
        {
            get => new Vector3(
                _positionX,
                _positionYCompressed / Y_SCALE_FACTOR,
                _positionZ
            );
            set
            {
                _positionX = value.x;
                // Scale and clamp Y to fit -327.68 to 327.67
                float y = Mathf.Clamp(value.y, Y_MIN, Y_MAX);
                _positionYCompressed = (short)Mathf.Round(y * Y_SCALE_FACTOR);
                _positionZ = value.z;
            }
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
                Vector3 forward = Forward;
                return Quaternion.LookRotation(forward, Vector3.up);
            }
            set
            {
                Forward = value * Vector3.forward;
            }
        }

        public bool IsPositionEqual(ref FWorldTransform other)
        {
            return _positionX == other._positionX &&
                   _positionYCompressed == other._positionYCompressed &&
                   _positionZ == other._positionZ;
        }

        public bool IsRotationEqual(ref FWorldTransform other)
        {
            return _compressedRotation == other._compressedRotation;
        }

        public void CopyPosition(ref FWorldTransform other)
        {
            _positionX = other._positionX;
            _positionYCompressed = other._positionYCompressed;
            _positionZ = other._positionZ;
        }

        public void CopyRotation(ref FWorldTransform other)
        {
            _compressedRotation = other._compressedRotation;
        }

        private static ushort EncodeOctahedral(Vector3 normal)
        {
            normal = normal.normalized;
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
            byte uByte = (byte)Mathf.RoundToInt((u * 0.5f + 0.5f) * 255f);
            byte vByte = (byte)Mathf.RoundToInt((v * 0.5f + 0.5f) * 255f);
            return (ushort)((vByte << 8) | uByte);
        }

        private static Vector3 DecodeOctahedral(ushort encoded)
        {
            byte uByte = (byte)(encoded & 0xFF);
            byte vByte = (byte)(encoded >> 8);
            float u = (uByte / 255f) * 2f - 1f;
            float v = (vByte / 255f) * 2f - 1f;
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
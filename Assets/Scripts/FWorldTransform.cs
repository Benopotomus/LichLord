using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord
{
    // Custom 24-bit signed integer struct (unchanged)
    [StructLayout(LayoutKind.Explicit, Size = 3)]
    public struct Int24 : INetworkStruct
    {
        [FieldOffset(0)] private byte byte0;
        [FieldOffset(1)] private byte byte1;
        [FieldOffset(2)] private byte byte2;

        private const int MIN_VALUE = -8388608; // -2^23
        private const int MAX_VALUE = 8388607;  // 2^23 - 1

        public Int24(int value)
        {
            value = Mathf.Clamp(value, MIN_VALUE, MAX_VALUE);
            byte0 = (byte)(value & 0xFF);
            byte1 = (byte)((value >> 8) & 0xFF);
            byte2 = (byte)((value >> 16) & 0xFF);
        }

        public static implicit operator int(Int24 value)
        {
            int result = (value.byte0 | (value.byte1 << 8) | (value.byte2 << 16));
            if ((value.byte2 & 0x80) != 0) result |= unchecked((int)0xFF000000);
            return result;
        }

        public static implicit operator Int24(int value) => new Int24(value);
    }

    [StructLayout(LayoutKind.Explicit, Size = 9)]
    public struct FWorldTransform : INetworkStruct
    {
        [FieldOffset(0)] private Int24 _positionX; // 3 bytes
        [FieldOffset(3)] private short _positionYCompressed; // 2 bytes
        [FieldOffset(5)] private Int24 _positionZ; // 3 bytes
        [FieldOffset(8)] private byte _compressedRotation; // 1 byte
        // Total: 9 bytes

        private const float SCALE_FACTOR = 100f; // For two decimal places
        private const float X_Z_MIN = -83886.08f; // -8,388,608 / 100
        private const float X_Z_MAX = 83886.07f; // 8,388,607 / 100
        private const float Y_MIN = -327.68f; // -32,768 / 100
        private const float Y_MAX = 327.67f; // 32,767 / 100

        public float PositionX
        {
            get => (int)_positionX / SCALE_FACTOR;
            set => _positionX = (int)(Mathf.Clamp(value, X_Z_MIN, X_Z_MAX) * SCALE_FACTOR);
        }

        public float PositionY
        {
            get => _positionYCompressed / SCALE_FACTOR;
            set => _positionYCompressed = (short)(Mathf.Clamp(value, Y_MIN, Y_MAX) * SCALE_FACTOR);
        }

        public float PositionZ
        {
            get => (int)_positionZ / SCALE_FACTOR;
            set => _positionZ = (int)(Mathf.Clamp(value, X_Z_MIN, X_Z_MAX) * SCALE_FACTOR);
        }

        public Vector3 Position
        {
            get => new Vector3(
                (int)_positionX / SCALE_FACTOR,
                _positionYCompressed / SCALE_FACTOR,
                (int)_positionZ / SCALE_FACTOR
            );
            set
            {
                _positionX = (int)(Mathf.Clamp(value.x, X_Z_MIN, X_Z_MAX) * SCALE_FACTOR);
                _positionYCompressed = (short)(Mathf.Clamp(value.y, Y_MIN, Y_MAX) * SCALE_FACTOR);
                _positionZ = (int)(Mathf.Clamp(value.z, X_Z_MIN, X_Z_MAX) * SCALE_FACTOR);
            }
        }

        public Vector3 Forward
        {
            get => DecodeYaw(_compressedRotation);
            set => _compressedRotation = EncodeYaw(value);
        }

        public Quaternion Rotation
        {
            get => Quaternion.LookRotation(Forward, Vector3.up);
            set => Forward = value * Vector3.forward;
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

        public string DebugString()
        {
            return $"Position: ({(int)_positionX / SCALE_FACTOR:F2}, {_positionYCompressed / SCALE_FACTOR:F2}, {(int)_positionZ / SCALE_FACTOR:F2}), " +
                   $"Yaw: {_compressedRotation} ({(DecodeYaw(_compressedRotation).x, DecodeYaw(_compressedRotation).z)})";
        }

        private static byte EncodeYaw(Vector3 forward)
        {
            forward.y = 0;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();
            float yaw = Mathf.Atan2(forward.x, forward.z);
            if (yaw < 0) yaw += 2 * Mathf.PI;
            return (byte)Mathf.RoundToInt((yaw / (2 * Mathf.PI)) * 255f);
        }

        private static Vector3 DecodeYaw(byte encoded)
        {
            float yaw = (encoded / 255f) * 2 * Mathf.PI;
            return new Vector3(Mathf.Sin(yaw), 0, Mathf.Cos(yaw)).normalized;
        }
    }
}
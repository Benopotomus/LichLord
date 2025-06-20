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

    [StructLayout(LayoutKind.Explicit, Size = 6)]
    public struct FWorldPosition : INetworkStruct
    {
        [FieldOffset(0)] private short _positionX; // 2 bytes
        [FieldOffset(2)] private short _positionYCompressed; // 2 bytes
        [FieldOffset(4)] private short _positionZ; // 2 bytes
        // Total: 6 bytes

        private const float SCALE_FACTOR = 20f; // For 0.05 precision (1 / 0.05)
        private const float X_Z_MIN = -1638.35f; // -32767 / 20
        private const float X_Z_MAX = 1638.35f; // 32767 / 20
        private const float Y_MIN = -327.68f; // -32,768 / 100
        private const float Y_MAX = 327.67f; // 32,767 / 100

        public float PositionX
        {
            get => _positionX / SCALE_FACTOR;
            set => _positionX = (short)(Mathf.Clamp(value, X_Z_MIN, X_Z_MAX) * SCALE_FACTOR);
        }

        public float PositionY
        {
            get => _positionYCompressed / 100f; // Y still uses 0.01 precision
            set => _positionYCompressed = (short)(Mathf.Clamp(value, Y_MIN, Y_MAX) * 100f);
        }

        public float PositionZ
        {
            get => _positionZ / SCALE_FACTOR;
            set => _positionZ = (short)(Mathf.Clamp(value, X_Z_MIN, X_Z_MAX) * SCALE_FACTOR);
        }

        public Vector3 Position
        {
            get => new Vector3(
                _positionX / SCALE_FACTOR,
                _positionYCompressed / 100f,
                _positionZ / SCALE_FACTOR
            );
            set
            {
                _positionX = (short)(Mathf.Clamp(value.x, X_Z_MIN, X_Z_MAX) * SCALE_FACTOR);
                _positionYCompressed = (short)(Mathf.Clamp(value.y, Y_MIN, Y_MAX) * 100f);
                _positionZ = (short)(Mathf.Clamp(value.z, X_Z_MIN, X_Z_MAX) * SCALE_FACTOR);
            }
        }
        
        public bool IsPositionEqual(ref FWorldPosition other)
        {
            return _positionX == other._positionX &&
                   _positionYCompressed == other._positionYCompressed &&
                   _positionZ == other._positionZ;
        }

        public void CopyPosition(Vector3 other)
        {
            PositionX = other.x;
            PositionY = other.y;
            PositionZ = other.z;
        }

        public void CopyPosition(ref FWorldPosition other)
        {
            _positionX = other._positionX;
            _positionYCompressed = other._positionYCompressed;
            _positionZ = other._positionZ;
        }

        public string DebugString()
        {
            return $"Position: ({_positionX / SCALE_FACTOR:F2}, {_positionYCompressed / 100f:F2}, {_positionZ / SCALE_FACTOR:F2})";
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct FWorldTransform : INetworkStruct
    {
        [FieldOffset(0)] private FWorldPosition _position; // 6 bytes
        [FieldOffset(6)] private byte _compressedYaw; // 1 byte
        [FieldOffset(7)] private byte _compressedPitch; // 1 byte
        // Total: 8 bytes

        public float PositionX
        {
            get => _position.PositionX;
            set => _position.PositionX = value;
        }

        public float PositionY
        {
            get => _position.PositionY;
            set => _position.PositionY = value;
        }

        public float PositionZ
        {
            get => _position.PositionZ;
            set => _position.PositionZ = value;
        }

        public Vector3 Position
        {
            get => _position.Position;
            set => _position.Position = value;
        }

        public float Yaw // Yaw in degrees (0 to 360)
        {
            get => (_compressedYaw / 255f) * 360f;
            set
            {
                float clampedYaw = value % 360f;
                if (clampedYaw < 0f) clampedYaw += 360f;
                _compressedYaw = (byte)(clampedYaw / 360f * 255f);
            }
        }

        public float Pitch // in degrees: -90 to 90
        {
            get => (_compressedPitch - 127.5f) / 127.5f * 90f;
            set
            {
                float clamped = Mathf.Clamp(value, -90f, 90f);
                _compressedPitch = (byte)(((clamped / 90f) * 127.5f) + 127.5f);
            }
        }

        public Quaternion Rotation
        {
            get => Quaternion.Euler(Pitch, Yaw, 0);
            set
            {
                Vector3 euler = value.eulerAngles;
                Yaw = euler.y; // Yaw in degrees
                Pitch = euler.x; // Pitch in degrees
            }
        }

        public bool IsPositionEqual(ref FWorldTransform other)
        {
            return _position.IsPositionEqual(ref other._position);
        }

        public bool IsRotationEqual(ref FWorldTransform other)
        {
            return _compressedYaw == other._compressedYaw &&
                   _compressedPitch == other._compressedPitch;
        }

        public void CopyPosition(ref FWorldTransform other)
        {
            _position.CopyPosition(ref other._position);
        }

        public void CopyRotation(ref FWorldTransform other)
        {
            _compressedYaw = other._compressedYaw;
            _compressedPitch = other._compressedPitch;
        }

        public string DebugString()
        {
            return $"{_position.DebugString()}, Yaw: {Yaw:F2}°, Pitch: {Pitch:F2}° (bytes: {_compressedYaw}, {_compressedPitch})";
        }
    }
}
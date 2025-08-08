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

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct FWorldTransform : INetworkStruct
    {
        [FieldOffset(0)] private FWorldPosition _position; // 7 bytes
        [FieldOffset(7)] private byte _compressedYaw; // 1 byte
        [FieldOffset(8)] private byte _compressedPitch; // 1 byte
        // Total: 9 bytes

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

        public byte RawCompressedYaw
        {
            get => _compressedYaw;
            set => _compressedYaw = value;
        }

        public float Yaw // Always mapped to byte 0–240
        {
            get
            {
                // If value is over 240, treat as 240 for yaw purposes
                byte yawByte = _compressedYaw > 240 ? (byte)240 : _compressedYaw;
                return (yawByte / 240f) * 360f;
            }
            set
            {
                float clampedYaw = value % 360f;
                if (clampedYaw < 0f) clampedYaw += 360f;

                // Map 0–360 degrees into byte range 0–240
                _compressedYaw = (byte)Mathf.Clamp(
                    Mathf.RoundToInt(clampedYaw / 360f * 240f),
                    0,
                    240
                );
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
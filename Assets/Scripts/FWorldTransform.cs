using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct FWorldTransform : INetworkStruct
    {
        [FieldOffset(0)] private FWorldPosition _position; // 7 bytes
        [FieldOffset(7)] private byte _compressedYaw; // 1 byte
        [FieldOffset(8)] private sbyte _compressedPitch; // 1 byte
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

        public float Pitch // -90 to +90 degrees
        {
            get => Mathf.Clamp((_compressedPitch / 120f) * 90f, -90f, 90f);
            set
            {
                float clamped = Mathf.Clamp(value, -90f, 90f);
                _compressedPitch = (sbyte)Mathf.RoundToInt((clamped / 90f) * 120f);
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
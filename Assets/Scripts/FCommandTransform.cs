using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 9)]
    public struct FCommandTransform : INetworkStruct
    {
        [FieldOffset(0)] private byte _stance; // 1 bytes
        [FieldOffset(1)] private FWorldPosition _position; // 7 bytes
        [FieldOffset(8)] private byte _compressedYaw; // 1 byte
        // Total: 9 bytes

        public ESquadStance Stance
        { 
            get => (ESquadStance)_stance;
            set => _stance = (byte)value;
        }

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

        public Quaternion Rotation
        {
            get
            {
                float yawRad = Yaw * Mathf.Deg2Rad;     // around Y
                float pitchRad = 0;

                float halfYaw = yawRad * 0.5f;
                float halfPitch = pitchRad * 0.5f;

                float sinYaw = Mathf.Sin(halfYaw);
                float cosYaw = Mathf.Cos(halfYaw);
                float sinPitch = Mathf.Sin(halfPitch);
                float cosPitch = Mathf.Cos(halfPitch);

                // Combine yaw (Y) then pitch (X). Roll = 0.
                Quaternion q;
                q.x = cosYaw * sinPitch;   // X component
                q.y = sinYaw * cosPitch;   // Y component
                q.z = -sinYaw * sinPitch;  // Z component
                q.w = cosYaw * cosPitch;   // W component
                return q;
            }
            set
            {
                Vector3 euler = value.eulerAngles;
                Yaw = euler.y; // Yaw in degrees
            }
        }

        public bool IsPositionEqual(ref FCommandTransform other)
        {
            return _position.IsPositionEqual(ref other._position);
        }

        public bool IsRotationEqual(ref FCommandTransform other)
        {
            return _compressedYaw == other._compressedYaw;
        }

        public bool IsEqual(ref FCommandTransform other)
        {
            if (!IsPositionEqual(ref other))
                return false;

            if (!IsRotationEqual(ref other))
                return false;

            return true;
        }

        public void CopyPosition(ref FCommandTransform other)
        {
            _position.CopyPosition(in other._position);
        }

        public void CopyRotation(ref FCommandTransform other)
        {
            _compressedYaw = other._compressedYaw;
        }

        public string DebugString()
        {
            return $"{_position.DebugString()}, Yaw: {Yaw:F2}°, (bytes: {_compressedYaw})";
        }

        public void Copy(in FCommandTransform other)
        {
            _position.CopyPosition(in other._position);
            _compressedYaw = other._compressedYaw;
        }
    }

    public enum ESquadStance
    { 
        Follow,
        Defend,
        Attack,
    }
}
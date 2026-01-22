using UnityEngine;
using System.Runtime.InteropServices;
using Fusion;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct FWorldRotation : INetworkStruct
    {
        [FieldOffset(0)]  private byte _compressedYaw;     // 0–240 range for 0–360°
        [FieldOffset(1)]  private sbyte _compressedPitch; // -120 to +120 range for -90° to +90°

        /// <summary>
        /// Yaw in degrees (0–360°), internally stored as 0–240.
        /// Values outside 0–360 are wrapped.
        /// </summary>
        public float Yaw
        {
            get
            {
                byte yawByte = _compressedYaw > 240 ? (byte)240 : _compressedYaw;
                return (yawByte / 240f) * 360f;
            }
            set
            {
                float clampedYaw = value % 360f;
                if (clampedYaw < 0f) clampedYaw += 360f;
                _compressedYaw = (byte)Mathf.Clamp(
                    Mathf.RoundToInt(clampedYaw / 360f * 240f),
                    0,
                    240
                );
            }
        }

        /// <summary>
        /// Pitch in degrees (-90° to +90°), internally stored as -120 to +120.
        /// </summary>
        public float Pitch
        {
            get => Mathf.Clamp((_compressedPitch / 120f) * 90f, -90f, 90f);
            set
            {
                float clamped = Mathf.Clamp(value, -90f, 90f);
                _compressedPitch = (sbyte)Mathf.RoundToInt((clamped / 90f) * 120f);
            }
        }

        /// <summary>
        /// Raw compressed yaw byte (0–255, but only 0–240 is meaningful).
        /// </summary>
        public byte RawCompressedYaw
        {
            get => _compressedYaw;
            set => _compressedYaw = value;
        }

        /// <summary>
        /// Raw compressed pitch byte (-128 to 127, but only -120 to 120 is meaningful).
        /// </summary>
        public sbyte RawCompressedPitch
        {
            get => _compressedPitch;
            set => _compressedPitch = value;
        }

        /// <summary>
        /// Full rotation as a Quaternion (roll = 0).
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                float yawRad = Yaw * Mathf.Deg2Rad;
                float pitchRad = Pitch * Mathf.Deg2Rad;

                float halfYaw = yawRad * 0.5f;
                float halfPitch = pitchRad * 0.5f;

                float sinYaw = Mathf.Sin(halfYaw);
                float cosYaw = Mathf.Cos(halfYaw);
                float sinPitch = Mathf.Sin(halfPitch);
                float cosPitch = Mathf.Cos(halfPitch);

                // Yaw (around Y) then Pitch (around X)
                return new Quaternion(
                    cosYaw * sinPitch,    // x
                    sinYaw * cosPitch,    // y
                    -sinYaw * sinPitch,    // z
                    cosYaw * cosPitch     // w
                );
            }
            set
            {
                Vector3 euler = value.eulerAngles;
                Yaw = euler.y; // yaw
                Pitch = euler.x; // pitch
            }
        }

        public bool IsEqual(ref FWorldRotation other)
        {
            return _compressedYaw == other._compressedYaw &&
                   _compressedPitch == other._compressedPitch;
        }

        public void Copy(in FWorldRotation other)
        {
            _compressedYaw = other._compressedYaw;
            _compressedPitch = other._compressedPitch;
        }

        public string DebugString()
        {
            return $"Yaw: {Yaw:F2}° (byte: {_compressedYaw}), Pitch: {Pitch:F2}° (byte: {_compressedPitch})";
        }
    }
}
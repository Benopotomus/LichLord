using Fusion;
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FPlayerStatData : INetworkStruct
    {
        [FieldOffset(0)]
        private short _value;

        private const float SCALE = 100f;           // 2 decimal places
        private const float INV_SCALE = 1f / SCALE;
        private const int MAX_RAW = short.MaxValue; // 32767
        private const int MIN_RAW = short.MinValue; // -32768

        public int GetValueAsInt()
        {
            return _value;
        }

        public void SetValueAsInt(int value)
        {
            _value = (short)Math.Clamp(value, MIN_RAW, MAX_RAW);
        }

        public float GetValueAsFloat()
        {
            return _value * INV_SCALE;
        }


        public void SetValueAsFloat(float value)
        {
            float scaled = value * SCALE;
            int raw = (int)Math.Round(scaled);

            if (raw > MAX_RAW)
            {
                Debug.LogWarning($"[FPlayerStatData] SetValueAsFloat attempted to set {value:F2} → clamped from {raw} to {MAX_RAW} (max allowed: ±327.67)");
                raw = MAX_RAW;
            }
            else if (raw < MIN_RAW)
            {
                Debug.LogWarning($"[FPlayerStatData] SetValueAsFloat attempted to set {value:F2} → clamped from {raw} to {MIN_RAW} (max allowed: ±327.67)");
                raw = MIN_RAW;
            }

            _value = (short)raw;
        }

        public override string ToString()
        {
            return $"{GetValueAsFloat():F2} (raw: {_value})";
        }
    }
}
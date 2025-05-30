namespace LichLord.Props
{
    using Fusion;
    using UnityEngine;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct FPropData : INetworkStruct
    {
        [FieldOffset(0)]
        private Vector3Compressed _position;

        [FieldOffset(12)]
        private Vector3Compressed _rotation;

        [FieldOffset(24)]
        private uint _propIndex; // The id of the prop in the world relative to the static or non modified props

        [FieldOffset(28)]
        private uint _packedData; // 4 bytes (16 bits)

        // Correct bit allocations
        private const int PROP_ID_BITS = 12;
        private const int DATA_BITS = 20;

        private const uint PROP_ID_MASK = (1u << PROP_ID_BITS) - 1;
        private const uint DATA_MASK = (1u << DATA_BITS) - 1;

        public uint PropID
        {
            get => _packedData & PROP_ID_MASK;
            set => _packedData = (_packedData & ~PROP_ID_MASK) | (value & PROP_ID_MASK);
        }

        public uint Data
        {
            get => (_packedData >> PROP_ID_BITS) & DATA_MASK;  // Shift and mask correctly
            set => _packedData = (_packedData & PROP_ID_MASK) | ((value & DATA_MASK) << PROP_ID_BITS);
        }

        public Vector3 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public bool IsValid()
        {
            if (PropID == 0)
                return false;

            return true;
        }

        public bool IsPackedDataEqual(FPropData otherChunkProp)
        {
            if (PropID != otherChunkProp.PropID)
                return false;

            if (Data != otherChunkProp.Data)
                return false;

            return true;
        }

        public bool IsPositionEqual(FPropData otherChunkProp)
        {
            if (_position.X != otherChunkProp._position.X)
                return false;

            if (_position.Y != otherChunkProp._position.Y)
                return false;

            return true;
        }

        // Copy the entire state (PropID and Data)
        public void CopyState(FPropData other)
        {
            _packedData = other._packedData;
        }

        // Copy only the position
        public void CopyPosition(FPropData other)
        {
            _position = other._position;
        }
    }
}

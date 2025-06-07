namespace LichLord.NonPlayerCharacters
{
    using Fusion;
    using UnityEngine;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct FNonPlayerCharacterData : INetworkStruct
    {
        [FieldOffset(0)]
        private FWorldTransform _transform; // 9 bytes: Position (8) + Rotation (1)
        [FieldOffset(9)]
        private byte _condition; // 1 byte: NPCState (4 bits) + CurrentSpeedPercent (4 bits)
        [FieldOffset(10)]
        private ushort _configuration; // 2 bytes: Index (9 bits) + DefinitionID (5 bits) + TeamID (2 bits)
        [FieldOffset(12)]
        private ushort _events; // 2 bytes: Health (12 bits) + Status (4 bits)
        // Total: 14 bytes

        public int DefinitionID
        {
            get => NonPlayerCharacterDataUtility.GetDefinitionID(ref this);
            set => NonPlayerCharacterDataUtility.SetDefinitionID(value, ref this);
        }

        public FWorldTransform Transform
        {
            get => _transform;
            set => _transform = value;
        }

        public Vector3 Position
        {
            get => _transform.Position;
            set => _transform.Position = value;
        }

        public float PositionX
        {
            get => _transform.PositionX;
            set => _transform.PositionX = value;
        }

        public float PositionY
        {
            get => _transform.PositionY;
            set => _transform.PositionY = value;
        }

        public float PositionZ
        {
            get => _transform.PositionZ;
            set => _transform.PositionZ = value;
        }

        public Quaternion Rotation
        {
            get => _transform.Rotation;
            set => _transform.Rotation = value;
        }

        public byte Condition
        {
            get => _condition;
            set => _condition = value;
        }

        
        public ushort Configuration
        {
            get => _configuration;
            set => _configuration = value;
        }

        public ushort Events
        {
            get => _events;
            set => _events = value;
        }

        public bool IsValid()
        {
            return DefinitionID != 0;
        }

        public bool IsPropDataEqual(ref FNonPlayerCharacterData other)
        {
            return IsPackedDataEqual(ref other);
        }

        public bool IsPackedDataEqual(ref FNonPlayerCharacterData other)
        {
            return _condition == other._condition &&
                  // _configuration == other._configuration &&
                 //  _events == other._events &&
                   _transform.Equals(other._transform);
        }

        public bool IsBitSet(ref byte flags, int bit)
        {
            return (flags & (1 << bit)) == (1 << bit);
        }

        public byte SetBit(ref byte flags, int bit, bool value)
        {
            if (value)
            {
                return flags |= (byte)(1 << bit);
            }
            else
            {
                return flags &= unchecked((byte)~(1 << bit));
            }
        }

        public byte SetBitNoRef(byte flags, int bit, bool value)
        {
            if (value)
            {
                return flags |= (byte)(1 << bit);
            }
            else
            {
                return flags &= unchecked((byte)~(1 << bit));
            }
        }
    }
}
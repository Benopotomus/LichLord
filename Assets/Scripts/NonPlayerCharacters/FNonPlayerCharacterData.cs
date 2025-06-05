namespace LichLord.NonPlayerCharacters
{
    using Fusion;
    using UnityEngine;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct FNonPlayerCharacterData : INetworkStruct
    {
        [FieldOffset(0)]
        private ushort _definitionId; // 4 bytes. definition id;
        [FieldOffset(2)]
        private FWorldTransform _transform; // 14 bytes
       // [FieldOffset(16)]
        //private FVelocity _velocity; // 4 bytes
        [FieldOffset(16)]
        private int _stateData; // 4 bytes
        [FieldOffset(20)]
        private int _health; // 4 bytes
        //34

        public int DefinitionID
        {
            get => _definitionId;
            set => _definitionId = (ushort)value;
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

        public Quaternion Rotation
        {
            get => _transform.Rotation;
            set => _transform.Rotation = value;
        }

        /*
        public Vector3 Velocity
        {
            get => _velocity.Velocity;
            set => _velocity.Velocity = value;
        }
        */

        public int StateData
        {
            get => _stateData;
            set => _stateData = value;
        }

        public int Health
        {
            get => _health;
            set => _health = value;
        }

        public bool IsValid()
        {
            return DefinitionID != 0;
        }

        public bool IsPropDataEqual(ref FNonPlayerCharacterData other)
        {
            if(_definitionId != other._definitionId)
                return false;

            if (!IsPackedDataEqual(ref other))
                return false;

            return true;
        }

        public bool IsEqualToRuntimeData(NonPlayerCharacterRuntimeState other)
        {
            if (_definitionId != other.definitionId)
                return false;

            return true;
        }

        public bool IsPackedDataEqual(ref FNonPlayerCharacterData other)
        {
            return DefinitionID == other.DefinitionID && StateData == other.StateData;
        }

        public bool IsBitSet(ref byte flags, int bit)
        {
            return (flags & (1 << bit)) == (1 << bit);
        }

        public byte SetBit(ref byte flags, int bit, bool value)
        {
            if (value == true)
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
            if (value == true)
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
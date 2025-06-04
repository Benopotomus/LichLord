namespace LichLord.NonPlayerCharacters
{
    using Fusion;
    using UnityEngine;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct FNonPlayerCharacterData : INetworkStruct
    {
        [FieldOffset(0)]
        private int _guid; // 4 bytes. The world save/load index for this asset
        [FieldOffset(4)]
        private int _definitionId; // 4 bytes. definition id;
        [FieldOffset(8)]
        private FWorldTransform _transform; // 14 bytes
        [FieldOffset(22)]
        private int _stateData; // 4 bytes
        [FieldOffset(26)]
        private int _health; // 4 bytes
        //30

        public int GUID
        {
            get => _guid;
            set => _guid = value;
        }

        public int DefinitionID
        {
            get => _definitionId;
            set => _definitionId = value;
        }

        public FWorldTransform Transform
        {
            get => _transform;
            set => _transform = value;
        }

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
namespace LichLord.Props
{
    using Fusion;
    using UnityEngine;
    using System.Runtime.InteropServices;
    using LichLord.Buildables;

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildableData : INetworkStruct
    {
        [FieldOffset(0)]
        private int _definitionId; // 4 bytes. definition id;
        [FieldOffset(4)]
        private int _stateData; // 4 bytes

        [FieldOffset(8)]
        FGridTransform _transform;
        [FieldOffset(8)]
        FWorldTransform _worldTransform;

        public int DefinitionID
        {
            get => _definitionId;
            set => _definitionId = value;
        }

        public int StateData
        {
            get => _stateData;
            set => _stateData = value;
        }

        public bool IsValid()
        {
            return DefinitionID != 0;
        }

        public bool IsPropDataEqual(ref FBuildableData other)
        {
            if (_definitionId != other._definitionId)
                return false;

            if (!IsPackedDataEqual(ref other))
                return false;

            return true;
        }


        public bool IsPackedDataEqual(ref FBuildableData other)
        {
            return DefinitionID == other.DefinitionID && StateData == other.StateData;
        }

        public void CopyStateData(ref FBuildableData other)
        {
            _stateData = other._stateData;
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
namespace LichLord.Props
{
    using Fusion;
    using LichLord.World;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct FPropData : INetworkStruct
    {
        [FieldOffset(0)]
        private byte _propGUID; // 1 byte
        [FieldOffset(1)]
        private FChunkPosition _chunkPosition; // 2 bytes
        [FieldOffset(3)]
        private ushort _definitionId; // 2 bytes. definition id;
        [FieldOffset(5)]
        private int _stateData; // 4 bytes
        // 9

        public int DefinitionID
        {
            get => _definitionId;
            set => _definitionId = (ushort)value;
        }

        public int StateData
        {
            get => _stateData;
            set => _stateData = value;
        }

        public int GUID
        {
            get => _propGUID;
            set => _propGUID = (byte)value;
        }

        public bool IsValid()
        {
            return DefinitionID != 0;
        }

        public void Copy(ref FPropData other)
        {
            _propGUID = other._propGUID;
            _definitionId = other._definitionId;
            _stateData = other._stateData;
        }

        public void Copy(PropRuntimeState state)
        {
            _propGUID = (byte)state.guid;
            _definitionId = (ushort)state.definitionId;
            _stateData = state.Data.StateData;
        }

        public bool IsPropDataEqual(ref FPropData other)
        {
            if(_definitionId != other._definitionId)
                return false;

            if (!IsPackedDataEqual(ref other))
                return false;

            return true;
        }

        public bool IsEqualToRuntimeData(PropRuntimeState other)
        {
            if (_definitionId != other.definitionId)
                return false;

            if (StateData != other.Data.StateData)
                return false;

            return true;
        }

        public bool IsPackedDataEqual(ref FPropData other)
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
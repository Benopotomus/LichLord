namespace LichLord.Props
{
    using Fusion;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Size = 3)]
    public struct FPropData : INetworkStruct
    {
        [FieldOffset(0)]
        private byte _definitionId; // 2 bytes. definition id;
        [FieldOffset(1)]
        private ushort _stateData; // 2 bytes
        // 3

        public int DefinitionID
        {
            get => _definitionId;
            set => _definitionId = (byte)value;
        }

        public ushort StateData
        {
            get => _stateData;
            set => _stateData = value;
        }

        public bool IsValid()
        {
            return DefinitionID != 0;
        }

        public void Copy(ref FPropData other)
        {
            _definitionId = other._definitionId;
            _stateData = other._stateData;
        }

        public void Copy(PropRuntimeState state)
        {
            _definitionId = (byte)state.definitionId;
            _stateData = state.Data.StateData;
        }

        public bool IsPropDataEqual(ref FPropData other)
        {
            if(_definitionId != other._definitionId)
                return false;

            if (_stateData != other._stateData)
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
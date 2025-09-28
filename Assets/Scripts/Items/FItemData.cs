namespace LichLord.Items
{
    using Fusion;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Size = 6)]
    public struct FItemData : INetworkStruct
    {
        [FieldOffset(0)]
        private ushort _definitionId; // 2 bytes
        [FieldOffset(2)]
        private int _data; // 4 bytes

        // Constants for bit masks

        public int DefinitionID
        {
            get => _definitionId;
            set => _definitionId = (ushort)value;
        }

        public int Data
        {
            get => _data;
            set => _data = value;
        }

        public bool IsValid() => _definitionId != 0;

        public void Clear() => _definitionId = 0;

        public void Copy(in FItemData copiedItem)
        { 
            _definitionId = copiedItem._definitionId;
            _data = copiedItem._data;
        }

        public bool IsEqual(in FItemData otherItem)
        { 
            if(_definitionId != otherItem._definitionId) 
                return false;

            if (_data != otherItem._data)
                return false;

            return true;
        }

    }
}

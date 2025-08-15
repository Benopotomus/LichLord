namespace LichLord.Buildables
{
    using Fusion;
    using LichLord.World;
    using System.Runtime.InteropServices;
    using UnityEngine;

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildableData : INetworkStruct
    {
        [FieldOffset(0)]
        private ushort _definitionId; // 2 bytes. definition id;
        [FieldOffset(2)]
        FWorldTransform _transform; //9
        [FieldOffset(11)]
        private int _stateData; // 4 bytes
        //15 bytes

        public ushort DefinitionID
        {
            get => _definitionId;
            set => _definitionId = value;
        }

        public int StateData
        {
            get => _stateData;
            set => _stateData = value;
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

        public FWorldTransform Transform
        {
            get => _transform;
            set => _transform = value;
        }

        public bool IsValid()
        {
            return DefinitionID != 0;
        }

        public void LoadFromSave(FBuildableSaveState saveState)
        {
            _definitionId = (ushort)saveState.definitionId;
            _transform.Position = saveState.position;
            _transform.Rotation = Quaternion.Euler(saveState.eulerAngles);
            _stateData = saveState.stateData;
        }

        public bool IsBuildDataEqual(ref FBuildableData other)
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

        public void Copy(ref FBuildableData other)
        {
            _definitionId = other._definitionId;
            _transform = other._transform;
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
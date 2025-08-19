namespace LichLord.NonPlayerCharacters
{
    using Fusion;
    using UnityEngine;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct FNonPlayerCharacterData : INetworkStruct
    {
        [FieldOffset(0)]
        private ushort _configuration; // 2 bytes
        [FieldOffset(2)]
        private FWorldTransform _transform; // 9 bytes: Position (6) + Rotation (2)
        [FieldOffset(11)]
        private byte _condition; // 1 byte: NPCState (4 bits)// animation bits
        [FieldOffset(12)]
        private ushort _events; // 2 bytes: Health (12 bits) and storage
        // Total: 14 bytes

        public int DefinitionID
        {
            get => NonPlayerCharacterDataUtility.GetDefinitionID(ref this);
            set => NonPlayerCharacterDataUtility.SetDefinitionID(value, ref this);
        }

        public NonPlayerCharacterDefinition Definition
        {
            get => Global.Tables.NonPlayerCharacterTable.TryGetDefinition(DefinitionID);
        }

        public int Health
        {
            get => NonPlayerCharacterDataUtility.GetHealth(ref this);
            set => NonPlayerCharacterDataUtility.SetHealth(value, ref this);
        }

        public ENonPlayerState State
        {
            get => NonPlayerCharacterDataUtility.GetNPCState(ref this);
            set => NonPlayerCharacterDataUtility.SetNPCState(value, ref this);
        }

        public int AnimationIndex
        {
            get => NonPlayerCharacterDataUtility.GetAnimationIndex(ref this);
            set => NonPlayerCharacterDataUtility.SetAnimationIndex(value, ref this);
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

        public float Yaw
        {
            get => _transform.Yaw;
            set => _transform.Yaw = value;
        }

        public byte RawCompressedYaw
        {
            get => _transform.RawCompressedYaw;
            set => _transform.RawCompressedYaw = value;
        }

        public int TargetPlayerIndex
        {
            get
            {
                if(_transform.RawCompressedYaw > 240)
                    return _transform.RawCompressedYaw - 240;
                else
                    return -1;
            }
        }

        public float Pitch
        {
            get => _transform.Pitch;
            set => _transform.Pitch = value;
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

        public ETeamID Team
        { 
            get => NonPlayerCharacterDataUtility.GetTeamID(ref this);
            set => NonPlayerCharacterDataUtility.SetTeamID(value, ref this);
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

        public bool IsActive()
        {
            return NonPlayerCharacterDataUtility.IsActive(ref this);
        }

        public void Copy(FNonPlayerCharacterData other)
        {
            _transform = other._transform;
            _condition = other._condition;
            _configuration = other._configuration;
            _events = other._events;
        }

        public void Copy(ref FNonPlayerCharacterData other)
        {
            _transform = other._transform;
            _condition = other._condition;
            _configuration = other._configuration;
            _events = other._events;
        }

        public bool IsEqual(ref FNonPlayerCharacterData other)
        {
            return _condition == other._condition &&
                   _configuration == other._configuration &&
                   _events == other._events &&
                   _transform.Equals(other._transform);
        }

        public bool IsStateDataEqual(ref FNonPlayerCharacterData other)
        {
            return _condition == other._condition;
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
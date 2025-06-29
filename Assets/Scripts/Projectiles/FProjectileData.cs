
namespace LichLord.Projectiles
{
    using System.Runtime.InteropServices;
    using Fusion;

    [StructLayout(LayoutKind.Explicit)]
    public struct FProjectileData : INetworkStruct
    {
        // When the projectile has completed its lifetime until it becomes inactive.
        public bool IsActive { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }
        public bool IsFinished { get { return IsBitSet(ref _state, 2); } set { SetBit(ref _state, 2, value); } }
        public bool HasImpacted { get { return IsBitSet(ref _state, 3); } set { SetBit(ref _state, 3, value); } }
        public bool IsHoming { get { return IsBitSet(ref _state, 4); } set { SetBit(ref _state, 4, value); } }
        public bool IsProximityFuseActive { get { return IsBitSet(ref _state, 5); } set { SetBit(ref _state, 5, value); } }
        public bool InstigatorEffectApplied { get { return IsBitSet(ref _state, 6); } set { SetBit(ref _state, 6, value); } }

        [FieldOffset(0)]
        private byte _state;
        [FieldOffset(1)]
        public ushort DefinitionID;
        [FieldOffset(3)]
        public int FireTick;
        [FieldOffset(7)]
        public FWorldPosition Position;
        [FieldOffset(13)]
        public FWorldPosition TargetPosition;
        [FieldOffset(19)]
        public FNetObjectID InstigatorID;
        [FieldOffset(25)]

        // Custom Data
        public FFuseData FuseData;
        [FieldOffset(25)]
        public FBounceData BounceData;
        [FieldOffset(25)]
        public FEncircleData EncircleData;
        [FieldOffset(25)]
        public FHomingData HomingData;
        [FieldOffset(25)]
        public FDyamicSpeedData DynamicSpeedData;
        [FieldOffset(25)]
        public FBeamData BeamData;

        //30

        [StructLayout(LayoutKind.Explicit)]
        public struct FFuseData : INetworkStruct
        {
            [FieldOffset(0)]
            public int FuseCompleteTick;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FDyamicSpeedData : INetworkStruct
        {
            [FieldOffset(0)]
            public float SpeedPercent;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FEncircleData : INetworkStruct
        {
            [FieldOffset(0)]
            public FNetObjectID AttachedActorID;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FBounceData : INetworkStruct
        {
            [FieldOffset(0)]
            public int BounceTick;
            [FieldOffset(4)]
            public byte BounceCount;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FHomingData : INetworkStruct
        {
            [FieldOffset(0)]
            public FNetObjectID TargetActorID;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct FBeamData : INetworkStruct
        {
            [FieldOffset(0)]
            public int ImpactTick;
        }

        public bool IsStateEqual(FProjectileData otherData)
        {
            if (_state != otherData._state)
                return false;

            return true;
        }

        public void Copy(FProjectileData otherData)
        {
            _state = otherData._state;
            DefinitionID = otherData.DefinitionID;
            FireTick = otherData.FireTick;
            Position = otherData.Position;//
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

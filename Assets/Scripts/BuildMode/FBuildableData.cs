namespace LichLord.Props
{
    using Fusion;
    using UnityEngine;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct FBuildableData : INetworkStruct
    {
        // When the prop has completed and will become inactive.
        public bool IsActive { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }

        [FieldOffset(0)]
        private byte _state;
        [FieldOffset(1)]
        private Vector3Compressed _position; // 12 bytes
        [FieldOffset(13)]
        private ushort _compressedRotation; // 2 bytes (octahedral encoding)
        [FieldOffset(15)]
        private int _propGUID; // 4 bytes. The world save/load index for this asset
        [FieldOffset(17)]
        private int _definitionId; // 4 bytes. definition id;
        [FieldOffset(23)]
        private int _stateData; // 4 bytes

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

        public int GUID
        {
            get => _propGUID;
            set => _propGUID = value;
        }

        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        public Vector3 Forward
        {
            get => DecodeOctahedral(_compressedRotation);
            set => _compressedRotation = EncodeOctahedral(value);
        }

        public Quaternion Rotation
        {
            get
            {
                // Convert forward direction to quaternion (assume up is world Y)
                Vector3 forward = Forward;
                return Quaternion.LookRotation(forward, Vector3.up);
            }
            set
            {
                // Extract forward direction from quaternion
                Forward = value * Vector3.forward;
            }
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

            if (!IsPositionEqual(ref other))
                return false;

            if (!IsRotationEqual(ref other))
                return false;

            return true;
        }

        public bool IsEqualToRuntimeData(PropRuntimeState other)
        {
            if (_definitionId != other.definitionId)
                return false;

            if (StateData != other.stateData)
                return false;

            if (Position != other.position)
                return false;

            if (Rotation != other.rotation)
                return false;

            return true;
        }

        public bool IsPackedDataEqual(ref FBuildableData other)
        {
            return DefinitionID == other.DefinitionID && StateData == other.StateData;
        }

        public bool IsPositionEqual(ref FBuildableData other)
        {
            return _position.X == other._position.X && _position.Y == other._position.Y;
        }

        public bool IsRotationEqual(ref FBuildableData other)
        {
            return _compressedRotation == other._compressedRotation;
        }

        public void CopyStateData(ref FBuildableData other)
        {
            _stateData = other._stateData;
            _compressedRotation = other._compressedRotation;
        }

        public void CopyPosition(ref FBuildableData other)
        {
            _position = other._position;
        }

        public void CopyRotation(ref FBuildableData other)
        {
            _compressedRotation = other._compressedRotation;
        }

        private static ushort EncodeOctahedral(Vector3 normal)
        {
            // Normalize input
            normal = normal.normalized;

            // Project to octahedron
            normal /= Mathf.Abs(normal.x) + Mathf.Abs(normal.y) + Mathf.Abs(normal.z);
            float u, v;
            if (normal.z >= 0f)
            {
                u = normal.x;
                v = normal.y;
            }
            else
            {
                u = (1f - Mathf.Abs(normal.y)) * Mathf.Sign(normal.x);
                v = (1f - Mathf.Abs(normal.x)) * Mathf.Sign(normal.y);
            }

            // Map u, v from [-1, 1] to [0, 255]
            byte uByte = (byte)Mathf.RoundToInt((u * 0.5f + 0.5f) * 255f);
            byte vByte = (byte)Mathf.RoundToInt((v * 0.5f + 0.5f) * 255f);

            // Pack into 16 bits
            return (ushort)((vByte << 8) | uByte);
        }

        private static Vector3 DecodeOctahedral(ushort encoded)
        {
            // Unpack u, v
            byte uByte = (byte)(encoded & 0xFF);
            byte vByte = (byte)(encoded >> 8);
            float u = (uByte / 255f) * 2f - 1f;
            float v = (vByte / 255f) * 2f - 1f;

            // Reconstruct normal
            float z = 1f - Mathf.Abs(u) - Mathf.Abs(v);
            Vector3 normal;
            if (z >= 0f)
            {
                normal = new Vector3(u, v, z);
            }
            else
            {
                normal = new Vector3(
                    (1f - Mathf.Abs(v)) * Mathf.Sign(u),
                    (1f - Mathf.Abs(u)) * Mathf.Sign(v),
                    z
                );
            }

            return normal.normalized;
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
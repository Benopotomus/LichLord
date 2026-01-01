using Fusion;
using LichLord.NonPlayerCharacters;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 5)]
    public struct FNetObjectID : INetworkStruct
    {
        [FieldOffset(0)]
        public ushort _data;

        private const int TYPE_BITS = 4;                // 0–15
        private const int TYPE_SHIFT = 0;
        private const ushort TYPE_MASK = (1 << TYPE_BITS) - 1;

        private const int INDEX_BITS = 10;                // 0–1023
        private const int INDEX_SHIFT = TYPE_SHIFT + TYPE_BITS;
        private const ushort INDEX_MASK = (1 << INDEX_BITS) - 1;

        // Type
        public EObjectType GetObjectType()
        {
            return (EObjectType)((_data >> TYPE_SHIFT) & TYPE_MASK);
        }

        public void SetObjectType(EObjectType objectType)
        {
            int config = _data;
            int newValue = Mathf.Clamp((int)objectType, 0, TYPE_MASK);
            config = ((config & ~(TYPE_MASK << TYPE_SHIFT)) | (newValue << TYPE_SHIFT));
            _data = (ushort)config;
        }

        // Index
        public int GetIndex()
        {
            return (_data >> INDEX_SHIFT) & INDEX_MASK;
        }

        public void SetIndex(int index)
        {
            int config = _data;
            int newValue = Mathf.Clamp((int)index, 0, INDEX_MASK);
            config = ((config & ~(INDEX_MASK << INDEX_SHIFT)) | (newValue << INDEX_SHIFT));
            _data = (ushort)config;
        }

        public bool IsValid()
        {
            if (_data == 0)
                return false;

            return true;
        }

        public void Copy(FNetObjectID otherObjectID)
        {
            this._data = otherObjectID._data;
        }

        public bool IsEqual(FNetObjectID otherObjectID)
        {
            if (this._data != otherObjectID._data)
                return false;

            return true;
        }

        public Component GetSceneContextObject(SceneContext context)
        {
            switch (GetObjectType())
            {
                case EObjectType.Player:
                    return context.NetworkGame;
                case EObjectType.NonPlayerCharacter:
                    return context.NonPlayerCharacterManager;
                case EObjectType.Buildable_Stronghold_1:
                    return context.StrongholdManager;
            }

            return null;
        }

        public IHitInstigator GetHitInstigator(SceneContext context)
        {
            Component component = GetSceneContextObject(context);
            if (component == null) 
                return null;

            switch (GetObjectType())
            {
                case EObjectType.Player:
                    var networkGame = context.NetworkGame;
                    return networkGame.GetPlayerByIndex(GetIndex());
                case EObjectType.NonPlayerCharacter:
                    var npcManager = context.NonPlayerCharacterManager;
                    return npcManager.GetNpcAtIndex(GetIndex());
            }

            return null;

        }

        public void Clear()
        {
            _data = 0;
        }
    }

    public enum EObjectType : byte
    {
        None,
        Player,
        NonPlayerCharacter,
        Buildable_Stronghold_0,
        Buildable_Stronghold_1,
        Buildable_Stronghold_3,
        Buildable_Stronghold_4,
    }
}

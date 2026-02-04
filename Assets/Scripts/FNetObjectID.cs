using Fusion;
using LichLord.Buildables;
using LichLord.NonPlayerCharacters;
using LichLord.World;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LichLord
{
    [StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct FNetObjectID : INetworkStruct
    {
        [FieldOffset(0)]
        public ushort _data;

        private const int TYPE_BITS = 5;                // 0–31
        private const int TYPE_SHIFT = 0;
        private const ushort TYPE_MASK = (1 << TYPE_BITS) - 1;

        private const int INDEX_BITS = 11;                // 0–2047
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
                case EObjectType.Lair:
                case EObjectType.Buildable_Lair_0:
                case EObjectType.Buildable_Lair_1:
                case EObjectType.Buildable_Lair_2:
                case EObjectType.Buildable_Lair_3:
                    return context.LairManager;
            }

            return null;
        }


        public IHitTarget GetHitTarget(SceneContext context)
        {
            Component component = GetSceneContextObject(context);
            if (component == null)
                return null;

            LairManager lairManager = null;
            Lair lair = null;
            FBuildableLoadState loadState;

            switch (GetObjectType())
            {
                case EObjectType.Player:
                    var networkGame = context.NetworkGame;
                    return networkGame.GetPlayerByIndex(GetIndex());
                case EObjectType.NonPlayerCharacter:
                    var npcManager = context.NonPlayerCharacterManager;
                    return npcManager.GetNpcAtIndex(GetIndex());
                case EObjectType.Lair:
                    lairManager = context.LairManager;
                    return lairManager.GetLair(GetIndex());
                case EObjectType.Buildable_Lair_0:
                    lairManager = context.LairManager;

                    lair = lairManager.GetLair(0);
                    if (lair == null)
                        return null;

                    loadState = lair.BuildableZone.LoadStates[GetIndex()];
                    if (loadState.LoadState != ELoadState.Loaded)
                        return null;

                    return loadState.Buildable;
                case EObjectType.Buildable_Lair_1:
                    lairManager = context.LairManager;

                    lair = lairManager.GetLair(1);
                    if (lair == null)
                        return null;

                    loadState = lair.BuildableZone.LoadStates[GetIndex()];
                    if (loadState.LoadState != ELoadState.Loaded)
                        return null;

                    return loadState.Buildable;
                case EObjectType.Buildable_Lair_2:
                    lairManager = context.LairManager;

                    lair = lairManager.GetLair(2);
                    if (lair == null)
                        return null;

                    loadState = lair.BuildableZone.LoadStates[GetIndex()];
                    if (loadState.LoadState != ELoadState.Loaded)
                        return null;

                    return loadState.Buildable;
                case EObjectType.Buildable_Lair_3:
                    lairManager = context.LairManager;

                    lair = lairManager.GetLair(3);
                    if (lair == null)
                        return null;

                    loadState = lair.BuildableZone.LoadStates[GetIndex()];
                    if (loadState.LoadState != ELoadState.Loaded)
                        return null;

                    return loadState.Buildable;

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
                case EObjectType.Lair:
                    var lairManager = context.LairManager;
                    return lairManager.GetLair(GetIndex());
            }

            return null;

        }

        public void SetHitInstigator(IHitInstigator hitInstigator)
        {
            if (hitInstigator is PlayerCharacter pc)
            { 
                SetObjectType(EObjectType.Player);
                SetIndex(pc.PlayerIndex);
            }

            if (hitInstigator is NonPlayerCharacter npc)
            {
                SetObjectType(EObjectType.NonPlayerCharacter);
                SetIndex(npc.FullIndex);
            }
        }

        public void SetHitTarget(IHitTarget hitTarget)
        {
            if (hitTarget is PlayerCharacter pc)
            {
                SetObjectType(EObjectType.Player);
                SetIndex(pc.PlayerIndex);
            }
            else if (hitTarget is NonPlayerCharacter npc)
            {
                SetObjectType(EObjectType.NonPlayerCharacter);
                SetIndex(npc.FullIndex);
            }
            else if (hitTarget is Lair lair)
            {
                Copy(lair.NetObjectID);
            }
            else if (hitTarget is Buildable buildable)
            {
                Copy(buildable.NetObjectID);
            }
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
        Lair,
        Buildable_Lair_0,
        Buildable_Lair_1,
        Buildable_Lair_2,
        Buildable_Lair_3,
    }
}

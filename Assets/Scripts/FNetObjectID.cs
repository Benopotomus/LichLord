using Fusion;
using UnityEngine;

namespace LichLord
{
    public struct FNetObjectID : INetworkStruct
    {
        public NetworkId guid;
        public byte index;

        public bool IsValid()
        {
            if (guid.Raw <= 0)
                return false;

            return true;
        }

        public void Copy(FNetObjectID otherObjectID)
        {
            guid = otherObjectID.guid;
            index = otherObjectID.index;
        }

        public bool IsEqual(FNetObjectID otherObjectID)
        {
            if (guid.Raw != otherObjectID.guid.Raw)
                return false;

            if (index != otherObjectID.index)
                return false;

            return true;
        }

        public NetworkObject GetNetObject(NetworkRunner runner)
        {
            return IsValid() ? runner.FindObject(guid) : null;
        }

        public T GetComponent<T>(NetworkRunner runner) where T : class
        {
            var netObject = GetNetObject(runner);
            return netObject != null ? netObject.GetComponent<T>() : null;
        }

        public IHitInstigator GetHitInstigator(NetworkRunner runner)
        {
            return GetComponent<IHitInstigator>(runner);
        }

        public IHitTarget GetHitTarget(NetworkRunner runner)
        {
            return GetComponent<IHitTarget>(runner);
        }

        // Override Equals
        public override bool Equals(object obj)
        {
            if (obj is FNetObjectID other)
            {
                return IsEqual(other);
            }
            return false;
        }

        // Override GetHashCode
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 31 + guid.Raw.GetHashCode();
                hash = hash * 31 + index.GetHashCode();
                return hash;
            }
        }

        public void Clear()
        {
            guid.Raw = 0;
            index = 0;
        }
    }
}

using Fusion;
using LichLord.NonPlayerCharacters;

namespace LichLord
{
    public struct FNetObjectID : INetworkStruct
    {
        public NetworkId networkId;
        public byte index;

        public bool IsValid()
        {
            if (networkId.Raw <= 0)
                return false;

            return true;
        }

        public void Copy(FNetObjectID otherObjectID)
        {
            networkId = otherObjectID.networkId;
            index = otherObjectID.index;
        }

        public bool IsEqual(FNetObjectID otherObjectID)
        {
            if (networkId.Raw != otherObjectID.networkId.Raw)
                return false;

            if (index != otherObjectID.index)
                return false;

            return true;
        }

        public NetworkObject GetNetObject(NetworkRunner runner)
        {
            return IsValid() ? runner.FindObject(networkId) : null;
        }

        public T GetComponent<T>(NetworkRunner runner) where T : class
        {
            var netObject = GetNetObject(runner);
            return netObject != null ? netObject.GetComponent<T>() : null;
        }

        public IHitInstigator GetHitInstigator(NetworkRunner runner)
        {
            var netObject = GetNetObject(runner);
            if (netObject == null)
                return null;

            // Use TryGetComponent if using Unity 2020.3+ for performance
            if (netObject.TryGetComponent<NonPlayerCharacterReplicator>(out var npcReplicator))
            {
                if (npcReplicator.LoadStates[index].LoadState != ELoadState.Loaded)
                    return null;
             
                return npcReplicator.LoadStates[index].NPC;
            }

            // Fallback: direct component lookup
            return netObject.GetComponent<IHitInstigator>();
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
                hash = hash * 31 + networkId.Raw.GetHashCode();
                hash = hash * 31 + index.GetHashCode();
                return hash;
            }
        }

        public void Clear()
        {
            networkId.Raw = 0;
            index = 0;
        }
    }
}

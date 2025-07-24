
namespace LichLord
{
    using Fusion;

    public struct FInteractorState : INetworkStruct
    {
        public bool IsInteracting { get { return _state.IsBitSet(0); } set { _state.SetBit(0, value); } }
        public bool IsExecuting { get { return _state.IsBitSet(1); } set { _state.SetBit(1, value); } }
        public bool IsReviving { get { return _state.IsBitSet(2); } set { _state.SetBit(2, value); } }
        public bool IsLooting { get { return _state.IsBitSet(3); } set { _state.SetBit(3, value); } }

        private byte _state;

        public FNetObjectID NetObject;

        public int StartTick;

        public bool IsValid()
        {
            return NetObject.IsValid();
        }

        public bool IsEqual(FInteractorState other)
        {
            if (other.NetObject.networkId == NetObject.networkId &&
                other.NetObject.index == NetObject.index)
                return true;

            return false;
        }
    }
}

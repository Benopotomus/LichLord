// Assets/Scripts/LichLord/SenseResult.cs
namespace LichLord
{
    public struct SenseResult
    {
        public int BrainIndex;
        public int AttackGlobalIndex;     // index in AllTrackables
        public int HarvestGlobalIndex;
        public int DepositGlobalIndex;
        public float AttackDistSqr;
        public float HarvestDistSqr;
        public float DepositDistSqr;
    }
}
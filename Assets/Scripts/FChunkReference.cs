using LichLord.World;

namespace LichLord
{
    public struct FChunkReference
    {
        private Chunk _chunk;
        public bool IsValid { get; private set; }

        public Chunk Chunk
        {
            get => _chunk;
            set
            {
                _chunk = value;
                IsValid = _chunk != null;
            }
        }
    }
}
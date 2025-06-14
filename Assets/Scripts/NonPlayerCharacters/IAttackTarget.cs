using LichLord.World;

namespace LichLord.NonPlayerCharacters
{
    public interface IAttackTarget
    {
        public IChunkTrackable ChunkTrackable { get; }

        public bool IsAttackable { get; }
    }
}

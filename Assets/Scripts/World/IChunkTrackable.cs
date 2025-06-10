
namespace LichLord.World
{
    public interface IChunkTrackable
    {
        void UpdateChunk(ChunkManager chunkManager);
        Chunk CurrentChunk { get; set; }
    }
}
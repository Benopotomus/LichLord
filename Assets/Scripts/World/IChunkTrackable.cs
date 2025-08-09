
using UnityEngine;

namespace LichLord.World
{
    public interface IChunkTrackable
    {
        Chunk CurrentChunk { get; set; }
        
        Vector3 Position { get; }

        bool IsAttackable { get; }

        // Extra radius added for npc maneuvers to determine if they're in range.
        public float BonusRadius { get; }
    }
}
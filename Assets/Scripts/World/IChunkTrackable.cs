
using UnityEngine;

namespace LichLord.World
{
    public interface IChunkTrackable
    {
        FChunkReference CurrentChunk { get; set; }
        
        Vector3 Position { get; }

        Collider HurtBoxCollider { get; }

        bool IsAttackable { get; }
    }
}
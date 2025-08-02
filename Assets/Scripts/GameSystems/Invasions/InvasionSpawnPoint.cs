
using UnityEngine;

namespace LichLord.World
{
    public class InvasionSpawnPoint
    {
        // Not replicated
        public Vector3 position; // World position
        public Chunk chunk; // Owning chunk

        public InvasionSpawnPoint(Vector3 position, Chunk chunk)
        { 
            this.position = position;
            this.chunk = chunk;
        }
    }
}


using UnityEngine;

namespace LichLord
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        public float Radius = 1f;

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}

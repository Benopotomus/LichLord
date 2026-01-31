using Fusion;
using UnityEngine;

namespace LichLord
{

    [CreateAssetMenu(fileName = "Maneuver", menuName = "LichLord/Maneuvers/SquadCommandManeuverDefinition", order = 1)]
    public class SquadCommandManeuverDefinition : ManeuverDefinition
    {
        [SerializeField]
        private int _squadId = -1;
        public int SquadId => _squadId;

    }
}

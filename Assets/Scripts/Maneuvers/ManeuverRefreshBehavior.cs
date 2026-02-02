using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "ManeuverRefreshBehavior", menuName = "LichLord/Maneuvers/Behaviors/ManeuverRefreshBehavior")]
    public class ManeuverRefreshBehavior : ScriptableObject
    {
        [SerializeField]
        private ERefreshType _refreshType;
        public ERefreshType RefreshType => _refreshType;

        [SerializeField]
        private ManeuverDefinition _newBehavior;
        public ManeuverDefinition NewBehavior => _newBehavior;

        public bool ShouldManeuverSwap(PlayerCharacter pc)
        {
            switch (RefreshType)
            {
                case ERefreshType.CommandSquadsSummoned:
                    // swaps when the command squads are active
                    if (pc.Commander.HasCommandSquadsSummoned)
                        return true;
                    break;
                case ERefreshType.CommandSquadsRecalled:
                    // swaps when the command squads are not active
                    if (!pc.Commander.HasCommandSquadsSummoned)
                        return true;
                    break;
            }

            return false;
        }

        public enum ERefreshType
        {
            None,
            CommandSquadsSummoned,
            CommandSquadsRecalled,
        }
    }


}

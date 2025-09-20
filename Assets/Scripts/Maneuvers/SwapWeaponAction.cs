
using Fusion;
using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "SwapWeaponAction", menuName = "LichLord/Maneuvers/SwapWeaponAction", order = 1)]
    public class SwapWeaponAction : ManeuverActionDefinition
    {
        public override void Execute(PlayerCharacter playerCharacter, NetworkRunner runner)
        { 
        }
    }
}

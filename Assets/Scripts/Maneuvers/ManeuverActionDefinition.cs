
using Fusion;
using UnityEngine;

namespace LichLord
{

    public class ManeuverActionDefinition : ScriptableObject
    {
        public virtual void Execute(PlayerCharacter playerCharacter, NetworkRunner runner)
        { 
        }

        public virtual void Sustain(PlayerCharacter playerCharacter, NetworkRunner runner)
        {
        }

        public virtual void EndExecute(PlayerCharacter playerCharacter, NetworkRunner runner)
        {
        }
    }
}

using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public struct FNonPlayerCharacterSpawnParams
    {
        public int index; // Unique identifier
        public int definitionId; // NonPlayerCharacterDefinition.TableID
        public Vector3 position; // World position
        public Quaternion rotation; // World rotation
        public ETeamID teamId; // Team identifier

        public void Copy(FNonPlayerCharacterSpawnParams other)
        {
            index = other.index;
            definitionId = other.definitionId;
            position = other.position;
            rotation = other.rotation;
            teamId = other.teamId;
        }
    }
}
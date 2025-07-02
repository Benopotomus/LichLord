using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public struct FNonPlayerCharacterSpawnParams
    {
        public int Index; // Unique identifier
        public int DefinitionId; // NonPlayerCharacterDefinition.TableID
        public Vector3 Position; // World position
        public Quaternion Rotation; // World rotation
        public ETeamID TeamId; // Team identifier

        public void Copy(FNonPlayerCharacterSpawnParams other)
        {
            Index = other.Index;
            DefinitionId = other.DefinitionId;
            Position = other.Position;
            Rotation = other.Rotation;
            TeamId = other.TeamId;
        }
    }
}
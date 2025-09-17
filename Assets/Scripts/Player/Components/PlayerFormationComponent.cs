using System;
using UnityEngine;
using LichLord.NonPlayerCharacters;

namespace LichLord
{
    public class PlayerFormationComponent : MonoBehaviour
    {
        [SerializeField]
        private Formation[] formations = new Formation[4];

        [SerializeField]
        private float backLineZOffset = -2f; // Offset to place the back line behind the front line

        [SerializeField]
        private float xSpacing = 2f; // Fixed 2-unit spacing between characters along x-axis

        public void AddCharacter(NonPlayerCharacter npc, int formationID, int formationIndex)
        {
            if (!IsValidFormation(formationID, formationIndex))
            {
                Debug.LogWarning($"Invalid formationID {formationID} or formationIndex {formationIndex}");
                return;
            }
            formations[formationID].Characters[formationIndex].Character = npc;
            formations[formationID].Characters[formationIndex].IsFilled = true;
        }

        public void RemoveCharacter(NonPlayerCharacter npc, int formationID, int formationIndex)
        {
            if (!IsValidFormation(formationID, formationIndex))
            {
                Debug.LogWarning($"Invalid formationID {formationID} or formationIndex {formationIndex}");
                return;
            }
            formations[formationID].Characters[formationIndex].Character = null;
            formations[formationID].Characters[formationIndex].IsFilled = false;
        }

        public Vector3 GetFormationPosition(int formationID, int formationIndex)
        {
            if (!IsValidFormation(formationID, formationIndex))
            {
               // Debug.LogWarning($"Invalid formationID {formationID} or formationIndex {formationIndex}, returning Vector3.zero");
                return Vector3.zero;
            }

            Formation formation = formations[formationID];
            bool isFrontLine = formationIndex < 8; // Indices 0-7 are front line, 8-15 are back line
            int lineStartIndex = isFrontLine ? 0 : 8;
            int lineEndIndex = isFrontLine ? 7 : 15;

            // Count non-null characters in the line and find position of formationIndex
            int nonNullCount = 0;
            int positionInLine = -1;
            for (int i = lineStartIndex; i <= lineEndIndex; i++)
            {
                if (formation.Characters[i].IsFilled)
                {
                    if (i == formationIndex)
                    {
                        positionInLine = nonNullCount;
                    }
                    nonNullCount++;
                }
            }

            if (nonNullCount == 0 || positionInLine == -1)
            {
                return Vector3.zero;
            }

            // Calculate x-position with 2-unit spacing, centered around transform.position
            float totalWidth = (nonNullCount - 1) * xSpacing;
            float xOffset = -(totalWidth / 2f) + (positionInLine * xSpacing);
            float zOffset = isFrontLine ? 0f : backLineZOffset;

            Vector3 localOffset = new Vector3(xOffset, 0f, zOffset);

            Vector3 worldPosition = transform.position + transform.TransformDirection(localOffset + formation.FormationOffset);

            return worldPosition;
        }

        public (int formationId, int formationIndex) GetFreeFrontlineIndex()
        {
            for (int formationId = 0; formationId < formations.Length; formationId++)
            {
                for (int i = 0; i < 8; i++) // Only check frontline indices 0-7
                {
                    if (!formations[formationId].Characters[i].IsFilled)
                    {
                        return (formationId, i);
                    }
                }
            }

            return (-1, -1); // Return (-1, -1) if no free frontline slot is found
        }

        public (int formationId, int formationIndex) GetFreeBacklineIndex()
        {
            for (int formationId = 0; formationId < formations.Length; formationId++)
            {
                for (int i = 8; i < 16; i++) // Only check backline indices 8-15
                {
                    if (!formations[formationId].Characters[i].IsFilled)
                    {
                        return (formationId, i);
                    }
                }
            }

            return (-1, -1); // Return (-1, -1) if no free backline slot is found
        }

        // Used to block slots before the NPC spawns
        public void SetFormationIndexFilled(int formationId, int formationIndex)
        {
            formations[formationId].Characters[formationIndex].IsFilled = true;
        }

        private bool IsValidFormation(int formationID, int formationIndex)
        {
            return formationID >= 0 && formationID < formations.Length &&
                   formationIndex >= 0 && formationIndex < formations[formationID].Characters.Length &&
                   formations[formationID] != null;
        }
    }

    [Serializable]
    public class Formation
    {
        public FFormationCharacter[] Characters = new FFormationCharacter[16];
        [SerializeField]
        public Vector3 FormationOffset; // Per-formation offset applied to all characters
    }

    [Serializable]
    public struct FFormationCharacter
    {
        public NonPlayerCharacter Character;
        public bool IsFilled;
    }
}
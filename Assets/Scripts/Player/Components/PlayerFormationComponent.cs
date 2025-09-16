using System;
using UnityEngine;
using LichLord.NonPlayerCharacters;

namespace LichLord
{
    public class PlayerFormationComponent : MonoBehaviour
    {
        [SerializeField]
        private Vector3[] formationOffsets = new Vector3[16]; // Kept for compatibility but unused

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
            formations[formationID].Characters[formationIndex] = npc;
        }

        public void RemoveCharacter(NonPlayerCharacter npc, int formationID, int formationIndex)
        {
            if (!IsValidFormation(formationID, formationIndex))
            {
                Debug.LogWarning($"Invalid formationID {formationID} or formationIndex {formationIndex}");
                return;
            }
            formations[formationID].Characters[formationIndex] = null;
        }

        public Vector3 GetFormationPosition(int formationID, int formationIndex)
        {
            if (!IsValidFormation(formationID, formationIndex))
            {
                Debug.LogWarning($"Invalid formationID {formationID} or formationIndex {formationIndex}, returning Vector3.zero");
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
                if (formation.Characters[i] != null)
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

            // Create local offset (x: spaced position, y: 0, z: back line offset if applicable)
            Vector3 localOffset = new Vector3(xOffset, 0f, zOffset);

            // Transform to world space and add formation-specific offset
            Vector3 worldPosition = transform.position + transform.TransformDirection(localOffset + formation.FormationOffset);

            Debug.Log($"GetFormationPosition: formationID={formationID}, formationIndex={formationIndex}, {(isFrontLine ? "front" : "back")} line, positionInLine={positionInLine}, nonNullCount={nonNullCount}, xOffset={xOffset}, zOffset={zOffset}, formationOffset={formation.FormationOffset}, worldPosition={worldPosition}");

            return worldPosition;
        }

        public int GetFreeFormationID()
        {
            for (int formationID = 0; formationID < formations.Length; formationID++)
            {
                if (formations[formationID] == null)
                {
                    continue; // Skip null formations
                }

                bool isFree = true;
                for (int i = 0; i < formations[formationID].Characters.Length; i++)
                {
                    if (formations[formationID].Characters[i] != null)
                    {
                        isFree = false;
                        break;
                    }
                }

                if (isFree)
                {
                    Debug.Log($"Found free formation at formationID={formationID}");
                    return formationID;
                }
            }

            Debug.LogWarning("No free formations found");
            return -1; // Return -1 if no free formation is found
        }

        public (int formationID, int formationIndex) GetFreeFrontlineIndex()
        {
            for (int formationID = 0; formationID < formations.Length; formationID++)
            {
                if (formations[formationID] == null)
                {
                    continue; // Skip null formations
                }

                for (int i = 0; i < 8; i++) // Only check frontline indices 0-7
                {
                    if (formations[formationID].Characters[i] == null)
                    {
                        Debug.Log($"Found free frontline slot at formationID={formationID}, formationIndex={i}");
                        return (formationID, i);
                    }
                }
            }

            Debug.LogWarning("No free frontline slots found");
            return (-1, -1); // Return (-1, -1) if no free frontline slot is found
        }

        public (int formationID, int formationIndex) GetFreeBacklineIndex()
        {
            for (int formationID = 0; formationID < formations.Length; formationID++)
            {
                if (formations[formationID] == null)
                {
                    continue; // Skip null formations
                }

                for (int i = 8; i < 16; i++) // Only check backline indices 8-15
                {
                    if (formations[formationID].Characters[i] == null)
                    {
                        Debug.Log($"Found free backline slot at formationID={formationID}, formationIndex={i}");
                        return (formationID, i);
                    }
                }
            }

            Debug.LogWarning("No free backline slots found");
            return (-1, -1); // Return (-1, -1) if no free backline slot is found
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
        public NonPlayerCharacter[] Characters = new NonPlayerCharacter[16];
        [SerializeField]
        public Vector3 FormationOffset; // Per-formation offset applied to all characters
    }
}
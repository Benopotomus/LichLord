using System;
using UnityEngine;
using LichLord.NonPlayerCharacters;
using Fusion;
using LichLord.Items;

namespace LichLord
{
    public class CommanderComponent : ContextBehaviour
    {
        FItemData[] _squadItems_0;
        FItemData[] _squadItems_1;
        FItemData[] _squadItems_2;

        [SerializeField]
        private CommandSquad[] squads = new CommandSquad[3];
        public CommandSquad[] Squads => squads;

        [SerializeField]
        private float backLineZOffset = -2f; // Offset to place the back line behind the front line

        [SerializeField]
        private float xSpacing = 2f; // Fixed 2-unit spacing between characters along x-axis

        [Networked]
        public ref FWorldTransform SquadTargetTransform_0 => ref MakeRef<FWorldTransform>();

        [Networked]
        public ref FWorldTransform SquadTargetTransform_1 => ref MakeRef<FWorldTransform>();

        [Networked]
        public ref FWorldTransform SquadTargetTransform_2 => ref MakeRef<FWorldTransform>();

        public Action<int, CommandSquad, int> OnCommandSquadUnitChanged;

        public void AddCharacter(NonPlayerCharacter npc, int squadId, int formationIndex)
        {
            if (!IsValidFormation(squadId, formationIndex))
            {
                Debug.LogWarning($"Invalid SquadID {squadId} or formationIndex {formationIndex}");
                return;
            }

            CommandSquad squad = squads[squadId];
            squad.CommandUnits[formationIndex].NPC = npc;
            squad.CommandUnits[formationIndex].IsFilled = true;

            OnCommandSquadUnitChanged?.Invoke(squadId, squad, formationIndex);
        }

        public void RemoveCharacter(NonPlayerCharacter npc, int squadId, int formationIndex)
        {
            if (!IsValidFormation(squadId, formationIndex))
            {
                Debug.LogWarning($"Invalid SquadID {squadId} or formationIndex {formationIndex}");
                return;
            }

            CommandSquad squad = squads[squadId];
            squads[squadId].CommandUnits[formationIndex].NPC = null;
            squads[squadId].CommandUnits[formationIndex].IsFilled = false;

            OnCommandSquadUnitChanged?.Invoke(squadId, squad, formationIndex);
        }

        public Vector3 GetFormationPosition(int squadId, int formationIndex)
        {
            if (!IsValidFormation(squadId, formationIndex))
            {
                //Debug.LogWarning($"Invalid squadId {squadId} or formationIndex {formationIndex}");
                return Vector3.zero;
            }

            CommandSquad squad = squads[squadId];
            int maxColumns = squad.MaxColumns;           // 6
            int slotsPerLine = maxColumns;               // 6

            // Determine which row (0 = front, 1 = back)
            int row = formationIndex / slotsPerLine;     // 0 or 1
            int col = formationIndex % slotsPerLine;     // 0 to 5

            // Count how many units are actually present in **this row**
            int occupiedInRow = 0;
            int positionInRow = -1;

            int rowStart = row * slotsPerLine;           // 0 or 6
            int rowEnd = rowStart + slotsPerLine - 1;  // 5 or 11

            for (int i = rowStart; i <= rowEnd; i++)
            {
                if (squad.CommandUnits[i].IsFilled)
                {
                    if (i == formationIndex)
                    {
                        positionInRow = occupiedInRow;
                    }
                    occupiedInRow++;
                }
            }

            if (occupiedInRow == 0 || positionInRow == -1)
            {
                return Vector3.zero;
            }

            // Center the occupied units
            float totalWidth = (occupiedInRow - 1) * xSpacing;
            float xOffset = -(totalWidth / 2f) + (positionInRow * xSpacing);

            float zOffset = (row == 0) ? 0f : backLineZOffset;

            Vector3 localOffset = new Vector3(xOffset, 0f, zOffset);

            // Apply squad-specific offset + centering around commander's position
            Vector3 worldPosition = transform.position
                                  + transform.TransformDirection(localOffset + squad.DefaultSquadPositionOffset);

            return worldPosition;
        }

        public (int squadId, int formationIndex) GetFreeFrontlineIndex()
        {
            for (int squadId = 0; squadId < squads.Length; squadId++)
            {
                CommandSquad squad = squads[squadId];

                for (int i = 0; i < squad.MaxColumns; i++) // Only check frontline indices 0-7
                {
                    if (!squads[squadId].CommandUnits[i].IsFilled)
                    {
                        return (squadId, i);
                    }
                }
            }
            
            return (-1, -1); // Return (-1, -1) if no free frontline slot is found
        }

        public (int squadId, int formationIndex) GetFreeBacklineIndex()
        {
            for (int squadId = 0; squadId < squads.Length; squadId++)
            {
                CommandSquad squad = squads[squadId];

                for (int i = squad.MaxColumns; i < (squad.MaxColumns * 2); i++) // Only check backline indices 8-15
                {
                    if (!squads[squadId].CommandUnits[i].IsFilled)
                    {
                        return (squadId, i);
                    }
                }
            }

            return (-1, -1); // Return (-1, -1) if no free backline slot is found
        }

        // Used to block slots before the NPC spawns
        public void SetFormationIndexFilled(int formationId, int formationIndex)
        {
            squads[formationId].CommandUnits[formationIndex].IsFilled = true;
        }

        private bool IsValidFormation(int squadId, int formationIndex)
        {
            return squadId >= 0 && squadId < squads.Length &&
                   formationIndex >= 0 && formationIndex < squads[squadId].CommandUnits.Length &&
                   squads[squadId] != null;
        }
    }

    [Serializable]
    public class CommandSquad
    {
        [SerializeField]
        public int MaxColumns = 6;

        [SerializeField]
        public int MaxRows = 2;

        [SerializeField]
        public Vector3 DefaultSquadPositionOffset; // Per-formation offset applied to all characters

        public FCommandUnit[] CommandUnits = new FCommandUnit[16];
    }

    [Serializable]
    public struct FCommandUnit
    {
        public NonPlayerCharacter NPC;
        public bool IsFilled;
    }
}
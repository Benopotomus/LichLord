using System;
using UnityEngine;
using LichLord.NonPlayerCharacters;
using Fusion;
using System.Collections.Generic;

namespace LichLord
{
    public class CommanderComponent : ContextBehaviour
    {
        [SerializeField]
        private CommandSquad[] _squads = new CommandSquad[3];
        public CommandSquad[] Squads => _squads;

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

            CommandSquad squad = _squads[squadId];
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

            CommandSquad squad = _squads[squadId];
            _squads[squadId].CommandUnits[formationIndex].NPC = null;
            _squads[squadId].CommandUnits[formationIndex].IsFilled = false;

            OnCommandSquadUnitChanged?.Invoke(squadId, squad, formationIndex);
        }

        public Vector3 GetFormationPosition(int squadId, int formationIndex)
        {
            if (!IsValidFormation(squadId, formationIndex))
            {
                //Debug.LogWarning($"Invalid squadId {squadId} or formationIndex {formationIndex}");
                return Vector3.zero;
            }

            CommandSquad squad = _squads[squadId];
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
            Vector3 worldPosition = GetCommandPositionForSquad(squadId)
                                  + transform.TransformDirection(localOffset );

            return worldPosition;
        }

        public (int squadId, int formationIndex) GetFreeFrontlineIndex()
        {
            for (int squadId = 0; squadId < _squads.Length; squadId++)
            {
                CommandSquad squad = _squads[squadId];

                for (int i = 0; i < squad.MaxColumns; i++) // Only check frontline indices 0-7
                {
                    if (!_squads[squadId].CommandUnits[i].IsFilled)
                    {
                        return (squadId, i);
                    }
                }
            }
            
            return (-1, -1); // Return (-1, -1) if no free frontline slot is found
        }

        public (int squadId, int formationIndex) GetFreeBacklineIndex()
        {
            for (int squadId = 0; squadId < _squads.Length; squadId++)
            {
                CommandSquad squad = _squads[squadId];

                for (int i = squad.MaxColumns; i < (squad.MaxColumns * 2); i++) // Only check backline indices 8-15
                {
                    if (!_squads[squadId].CommandUnits[i].IsFilled)
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
            _squads[formationId].CommandUnits[formationIndex].IsFilled = true;
        }

        private bool IsValidFormation(int squadId, int formationIndex)
        {
            return squadId >= 0 && squadId < _squads.Length &&
                   formationIndex >= 0 && formationIndex < _squads[squadId].CommandUnits.Length &&
                   _squads[squadId] != null;
        }

        private Vector3 GetCommandPositionForSquad(int squadId)
        {
            switch (squadId)
            { 
                case 0:
                    if (SquadTargetTransform_0.Position != Vector3.zero)
                        return SquadTargetTransform_0.Position;
                    break;
                 case 1:
                    if (SquadTargetTransform_1.Position != Vector3.zero)
                        return SquadTargetTransform_1.Position;
                    break;
                case 2:
                    if (SquadTargetTransform_2.Position != Vector3.zero)
                        return SquadTargetTransform_2.Position;
                    break;
            }

            return _squads[squadId].DefaultSquadPositionOffset;
        }

        public void SetCommandPosition(int squadId, Vector3 position)
        {
            switch (squadId)
            {
                case 0:
                    SquadTargetTransform_0.Position = position;
                    break;
                case 1:
                    SquadTargetTransform_1.Position = position;
                    break;
                case 2:
                    SquadTargetTransform_2.Position = position;
                    break;
            }
        }

        public void ProcessInput(ref FGameplayInput input)
        { 
        
        }

        public void OnFixedUpdate()
        {

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

        public bool HasAnyUnitsActive()
        { 
            for(int i = 0; i < CommandUnits.Length; i++)
                if (CommandUnits[i].IsFilled)
                    return true;

            return false;
        }
    }

    [Serializable]
    public struct FCommandUnit
    {
        public NonPlayerCharacter NPC;
        public bool IsFilled;
    }
}
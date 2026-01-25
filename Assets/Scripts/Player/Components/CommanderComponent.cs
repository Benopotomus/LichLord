using System;
using UnityEngine;
using LichLord.NonPlayerCharacters;
using Fusion;

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
        public ref FCommandTransform SquadTargetTransform_0 => ref MakeRef<FCommandTransform>();

        [Networked]
        public ref FCommandTransform SquadTargetTransform_1 => ref MakeRef<FCommandTransform>();

        [Networked]
        public ref FCommandTransform SquadTargetTransform_2 => ref MakeRef<FCommandTransform>();

        [SerializeField]
        private GameObject _commandVisualPrefab;
        private GameObject _commandVisualInstance;

        public Action<int, CommandSquad, int> OnCommandSquadUnitChanged;

        public override void Spawned()
        {
            _commandVisualInstance = GameObject.Instantiate(_commandVisualPrefab) as GameObject;
        }

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

            var squadTransform = GetCommandTransformForSquad(squadId);

            // Apply squad-specific offset + centering around commander's position
            Vector3 worldPosition = squadTransform.Item1
                                  + (squadTransform.Item2 * localOffset);

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

        private (Vector3, Quaternion) GetCommandTransformForSquad(int squadId)
        {
            switch (squadId)
            { 
                case 0:
                    if (SquadTargetTransform_0.IsValid)
                        return (SquadTargetTransform_0.Position, SquadTargetTransform_0.Rotation);
                    break;
                 case 1:
                    if (SquadTargetTransform_1.IsValid)
                        return (SquadTargetTransform_1.Position, SquadTargetTransform_1.Rotation);
                    break;
                case 2:
                    if (SquadTargetTransform_2.IsValid)
                        return (SquadTargetTransform_2.Position, SquadTargetTransform_2.Rotation);
                    break;
            }

            return (transform.position + transform.TransformDirection(_squads[squadId].DefaultSquadPositionOffset), transform.rotation);
        }

        public void SetCommandPosition(int squadId, Vector3 position)
        {
            switch (squadId)
            {
                case 0:
                    SquadTargetTransform_0.Position = position;
                    SquadTargetTransform_0.IsValid = true;
                    break;
                case 1:
                    SquadTargetTransform_1.Position = position;
                    SquadTargetTransform_1.IsValid = true;
                    break;
                case 2:
                    SquadTargetTransform_2.Position = position;
                    SquadTargetTransform_2.IsValid = true;
                    break;
            }
        }

        public void SetCommandRotation(int squadId, Vector3 direction)
        {
            switch (squadId)
            {
                case 0:
                    SquadTargetTransform_0.Rotation = Quaternion.LookRotation(direction);
                    SquadTargetTransform_0.IsValid = true;
                    break;
                case 1:
                    SquadTargetTransform_1.Rotation = Quaternion.LookRotation(direction);
                    SquadTargetTransform_1.IsValid = true;
                    break;
                case 2:
                    SquadTargetTransform_2.Rotation = Quaternion.LookRotation(direction);
                    SquadTargetTransform_2.IsValid = true;
                    break;
            }
        }

        public void ModifyCommandRotation(int squadId, float inputY)
        {
            switch (squadId)
            {
                case 0:
                    Vector3 euler0 = SquadTargetTransform_0.Rotation.eulerAngles;
                    euler0.y += inputY;
                    SquadTargetTransform_0.Rotation = Quaternion.Euler(euler0);
                    break;
                case 1:
                    Vector3 euler1 = SquadTargetTransform_1.Rotation.eulerAngles;
                    euler1.y += inputY;
                    SquadTargetTransform_1.Rotation = Quaternion.Euler(euler1);
                    break;
                case 2:
                    Vector3 euler2 = SquadTargetTransform_2.Rotation.eulerAngles;
                    euler2.y += inputY;
                    SquadTargetTransform_2.Rotation = Quaternion.Euler(euler2);
                    break;
            }
        }

        public void ToggleVisuals(int squadId, bool isDisplayed)
        {
            var commandTransform = GetCommandTransformForSquad(squadId);

            _commandVisualInstance.transform.position = commandTransform.Item1;
            _commandVisualInstance.transform.rotation = commandTransform.Item2;
        }

        public void ProcessInput(ref FGameplayInput input)
        { 
        
        }

        public void OnFixedUpdate()
        {
            if(SquadTargetTransform_0.IsValid) 
                ToggleVisuals(0, true);
            else
                ToggleVisuals(0, false);
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
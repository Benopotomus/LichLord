using System;
using UnityEngine;
using LichLord.NonPlayerCharacters;
using Fusion;

namespace LichLord
{
    public class CommanderComponent : ContextBehaviour
    {
        [SerializeField]
        private PlayerCharacter _pc;

        [SerializeField]
        private CommandSquad[] _squads = new CommandSquad[3];
        public CommandSquad[] Squads => _squads;

        private float backLineZOffset = -1f; // Offset to place the back line behind the front line
        private float frontLineZOffset = 1f;

        private float xSpacing = 2f; // Fixed 2-unit spacing between characters along x-axis

        [Networked]
        public ref FCommandTransform SquadTargetTransform_0 => ref MakeRef<FCommandTransform>();
        private float _desiredY_0 = 0;
        private ESquadStance _desiredStance_0;
        public ESquadStance DesiredStance_0 => _desiredStance_0;

        [Networked]
        public ref FCommandTransform SquadTargetTransform_1 => ref MakeRef<FCommandTransform>();
        private float _desiredY_1 = 0;
        private ESquadStance _desiredStance_1;
        public ESquadStance DesiredStance_1 => _desiredStance_1;

        [Networked]
        public ref FCommandTransform SquadTargetTransform_2 => ref MakeRef<FCommandTransform>();
        private float _desiredY_2 = 0;
        private ESquadStance _desiredStance_2;
        public ESquadStance DesiredStance_2 => _desiredStance_2;

        [SerializeField]
        private SquadCommandVisual _squadCommmandVisualPrefab;
        private SquadCommandVisual[] _squadCommandVisuals = new SquadCommandVisual[3];

        public Action<int, CommandSquad, int> OnCommandSquadUnitChanged;

        public Action<int, ESquadStance> OnDesiredSquadStanceChanged;

        public Action<int, bool> OnIsModifyingStanceChanged;

        public override void Spawned()
        {
            for (int i = 0; i < 3; i++)
            {
                _squadCommandVisuals[i] = Instantiate(_squadCommmandVisualPrefab) as SquadCommandVisual;
            }
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

            float zOffset = (row == 0) ? frontLineZOffset : backLineZOffset;

            Vector3 localOffset = new Vector3(xOffset, 0f, zOffset);

            var squadTransform = GetCommandTransformForSquad(squadId);

            // Apply squad-specific offset + centering around commander's position
            Vector3 worldPosition = squadTransform.Item1
                                  + (squadTransform.Item2 * localOffset);

            return worldPosition;
        }

        public (int squadId, int formationIndex) GetFreeSquadAndFrontlineIndex()
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

        public (int squadId, int formationIndex) GetFreeSquadAndBacklineIndex()
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

        public int GetFreeFrontlineIndex(int squadId)
        {
            CommandSquad squad = _squads[squadId];

            for (int i = 0; i < squad.MaxColumns; i++) // Only check frontline indices 0-7
            {
                if (!_squads[squadId].CommandUnits[i].IsFilled)
                {
                    return i;
                }
            }

            return -1; // Return (-1, -1) if no free frontline slot is found
        }

        public int GetFreeBacklineIndex(int squadId)
        {
            CommandSquad squad = _squads[squadId];

            for (int i = squad.MaxColumns; i < (squad.MaxColumns * 2); i++) // Only check backline indices 8-15
            {
                if (!_squads[squadId].CommandUnits[i].IsFilled)
                {
                    return i;
                }
            }

            return -1; // Return (-1, -1) if no free backline slot is found
        }

        // Used to block slots before the NPC spawns
        public void SetFormationIndexFilled(int squadId, int formationIndex)
        {
            _squads[squadId].CommandUnits[formationIndex].IsFilled = true;
        }

        public bool IsFormationIndexFilled(int squadId, int formationIndex)
        {
            return _squads[squadId].CommandUnits[formationIndex].IsFilled;
        }

        private bool IsValidFormation(int squadId, int formationIndex)
        {
            return squadId >= 0 && squadId < _squads.Length &&
                   formationIndex >= 0 && formationIndex < _squads[squadId].CommandUnits.Length &&
                   _squads[squadId] != null;
        }

        public bool IsCommandTargetValid(int squadId)
        {
            switch (squadId)
            {
                case 0:
                    return SquadTargetTransform_0.Stance != ESquadStance.Follow;
                case 1:
                    return SquadTargetTransform_1.Stance != ESquadStance.Follow;
                case 2:
                    return SquadTargetTransform_2.Stance != ESquadStance.Follow;
                    
            }

            return false;
        }

        public Vector3 GetSquadAveragePosition(int squadId)
        {
            CommandSquad squad = _squads[squadId];

            Vector3 sum = Vector3.zero;
            int count = 0;

            for (int i = 0; i < squad.CommandUnits.Length; i++)
            {
                var unit = squad.CommandUnits[i];
                if (unit.IsFilled && unit.NPC != null)
                {
                    sum += unit.NPC.Position;
                    count++;
                }
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        public (Vector3, Quaternion) GetCommandTransformForSquad(int squadId)
        {
            switch (squadId)
            { 
                case 0:
                    if (SquadTargetTransform_0.Stance != ESquadStance.Follow)
                        return (SquadTargetTransform_0.Position, SquadTargetTransform_0.Rotation);
                    break;
                 case 1:
                    if (SquadTargetTransform_1.Stance != ESquadStance.Follow)
                        return (SquadTargetTransform_1.Position, SquadTargetTransform_1.Rotation);
                    break;
                case 2:
                    if (SquadTargetTransform_2.Stance != ESquadStance.Follow)
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
                    if (SquadTargetTransform_0.Stance == ESquadStance.Follow)
                        SquadTargetTransform_0.Stance = ESquadStance.Attack;
                    break;
                case 1:
                    SquadTargetTransform_1.Position = position;
                    if (SquadTargetTransform_1.Stance == ESquadStance.Follow)
                        SquadTargetTransform_1.Stance = ESquadStance.Attack;
                    break;
                case 2:
                    SquadTargetTransform_2.Position = position;
                    if (SquadTargetTransform_2.Stance == ESquadStance.Follow)
                        SquadTargetTransform_2.Stance = ESquadStance.Attack;
                    break;
            }
        }

        public void SetCommandRotation(int squadId, Vector3 direction)
        {
            switch (squadId)
            {
                case 0:
                    SquadTargetTransform_0.Rotation = Quaternion.LookRotation(direction);
                    break;
                case 1:
                    SquadTargetTransform_1.Rotation = Quaternion.LookRotation(direction);
                    break;
                case 2:
                    SquadTargetTransform_2.Rotation = Quaternion.LookRotation(direction);
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

        public void SetModifyingStance(int squadId, bool isModifying)
        {
            OnIsModifyingStanceChanged?.Invoke(squadId, isModifying);
        }

        public void ModifyDesiredCommandStance(int squadId, float inputY)
        {
            {
                switch (squadId)
                {
                    case 0:
                        _desiredY_0 += inputY; 
                        _desiredStance_0 = GetNextStance(_desiredStance_0, ref _desiredY_0);
                        OnDesiredSquadStanceChanged?.Invoke(0, _desiredStance_0);
                        break;
                    case 1:
                        _desiredY_1 += inputY;
                        _desiredStance_1 = GetNextStance(_desiredStance_1, ref _desiredY_1);
                        OnDesiredSquadStanceChanged?.Invoke(1, _desiredStance_1);
                        break;
                    case 2:
                        _desiredY_2 += inputY;
                        _desiredStance_2 = GetNextStance(_desiredStance_2, ref _desiredY_2);
                        OnDesiredSquadStanceChanged?.Invoke(2, _desiredStance_2);
                        break;
                }
            }
        }

        public void ConfirmDesiredStance(int squadId)
        {
            switch (squadId)
            {
                case 0:
                    SquadTargetTransform_0.Stance = _desiredStance_0;
                    _desiredY_0 = 0;
                    break;
                case 1:
                    SquadTargetTransform_1.Stance = _desiredStance_1;
                    _desiredY_1 = 0;
                    break;
                case 2:
                    SquadTargetTransform_2.Stance = _desiredStance_2;
                    _desiredY_2 = 0;
                    break;
            }
        }

        private ESquadStance GetNextStance(ESquadStance oldStance, ref float inputY)
        {
            if (inputY > 1)
            {
                switch (oldStance)
                {
                    case ESquadStance.Attack:
                        inputY = 0;
                        return ESquadStance.Defend;
                    case ESquadStance.Defend:
                        inputY = 0;
                        return ESquadStance.Follow;
                }
            }
            else if (inputY < -1)
            {
                switch (oldStance)
                {
                    case ESquadStance.Follow:
                        inputY = 0;
                        return ESquadStance.Defend;
                    case ESquadStance.Defend:
                        inputY = 0;
                        return ESquadStance.Attack;
                }
            }

            return oldStance;
        }

        public ESquadStance GetDesiredStance(int squadId)
        {
            switch (squadId)
            {
                case 0: return _desiredStance_0;
                case 1: return _desiredStance_1;
                case 2: return _desiredStance_2;
            }

            return ESquadStance.Attack;
        }

        public ESquadStance GetStance(int squadId)
        {
            switch (squadId)
            {
                case 0: return SquadTargetTransform_0.Stance;
                case 1: return SquadTargetTransform_1.Stance;
                case 2: return SquadTargetTransform_2.Stance;
            }

            return ESquadStance.Attack;
        }

        public void ToggleVisuals(bool isDisplayed)
        {
            for (int i = 0; i < _squadCommandVisuals.Length; i++)
            {
                _squadCommandVisuals[i].SetActive(isDisplayed);
            }
        }

        public void OnFixedUpdate()
        {
            var selectedManeuver = _pc.Maneuvers.GetSelectedManeuver();
            if (selectedManeuver == null)
            {
                ToggleVisuals(false);
                return;
            }

            if (selectedManeuver.SquadId < 0)
            {
                ToggleVisuals(false);
                return;
            }

            for (int i = 0; i < _squadCommandVisuals.Length; i++)
            {
                if (i == selectedManeuver.SquadId)
                {
                    _squadCommandVisuals[i].SetActive(true);
                    _squadCommandVisuals[i].UpdateVisuals(this, i);
                }
                else
                {
                    _squadCommandVisuals[i].SetActive(false);
                }
            }
        }

        public void OnEnterState()
        {
        }

        public void OnExitState()
        {
            ToggleVisuals(false);
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
using UnityEngine;
using Fusion;
using System.Collections.Generic;
using LichLord.Items;

namespace LichLord
{
    public class SummonerComponent : NetworkBehaviour
    {
        [SerializeField] private PlayerCharacter _pc;

        [Networked] 
        private sbyte _selectedIndex { get; set; }
        public ELoadoutSlot SelectedSlot
        {
            get
            {
                switch (_selectedIndex)
                {
                    case 0:
                        return ELoadoutSlot.Summon_00;
                    case 1:
                        return ELoadoutSlot.Summon_01;
                    case 2:
                        return ELoadoutSlot.Summon_02;
                    case 3:
                        return ELoadoutSlot.Summon_03;
                    case 4:
                        return ELoadoutSlot.Summon_04;
                    default:
                        return ELoadoutSlot.None;
                }
            }
        }

        [Networked] 
        private ushort _activeSummoningManeuverId { get; set; }

        private TickTimer _activeManeuverTimer { get; set; }
        private int _activeManeuverTick;
        private ELoadoutSlot _activeManeuverSlot;

        // Current upper body blend amount
        private float _moveSpeedMultiplier = 1f;

        public float GetMoveSpeedMultiplier()
        { 
            if(_activeSummoningManeuverId < 0)
                return 1f;

            return _moveSpeedMultiplier;
        }

        public override void Spawned()
        {
            base.Spawned();
            ReplicateToAll(false);
        }

        public void ProcessInput(ref FGameplayInput input)
        {
            ProcessSummonSelection(ref input);
            ProcessManeuverActivation(ref input);
            ProcessActiveManeuver(ref input);
        }

        public void OnFixedUpdate()
        {
            ProcessManeuverExpiration();
        }

        private List<int> GetValidSummonIndexes()
        {
            List<int> actions = new List<int>();

            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_00).IsValid())
                actions.Add(0);
            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_01).IsValid())
                actions.Add(1);
            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_02).IsValid())
                actions.Add(2);
            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_03).IsValid())
                actions.Add(3);
            if (_pc.Inventory.GetItemAtLoadoutSlot(ELoadoutSlot.Summon_04).IsValid())
                actions.Add(4);

            return actions;
        }

        private void ProcessSummonSelection(ref FGameplayInput input)
        {
            List<int> validActions = GetValidSummonIndexes();

            // If no valid actions, reset _selectedIndex and exit
            if (validActions.Count == 0)
            {
                _selectedIndex = -1;
                return;
            }

            // Check if current _selectedIndex is invalid
            if (!validActions.Contains(_selectedIndex))
            {
                // Find the next valid index
                int currentPos = 0;
                if (_selectedIndex >= 0)
                {
                    // Try to find the next valid index after the current _selectedIndex
                    for (int i = 0; i < validActions.Count; i++)
                    {
                        if (validActions[i] > _selectedIndex)
                        {
                            currentPos = i;
                            break;
                        }
                    }
                }
                // Update to the next valid index
                _selectedIndex = (sbyte)validActions[currentPos];
            }

            int newIndex = -1;
            if (input.ScrollDelta != 0)
            {
                // Find the current position in validActions
                int currentPos = validActions.IndexOf(_selectedIndex);
                // Move to next/previous valid index based on scroll direction
                int delta = input.ScrollDelta > 0 ? 1 : -1;
                currentPos = (currentPos + delta + validActions.Count) % validActions.Count;
                newIndex = validActions[currentPos];
            }
            else if (input.ActionSelection > 0)
            {
                // Check if the selected index is valid
                int selectedIndex = input.ActionSelection - 1;
                if (validActions.Contains(selectedIndex))
                {
                    newIndex = selectedIndex;
                }
                else
                {
                    return;
                }
            }

            if (newIndex < 0)
                return;

            if (newIndex == _selectedIndex)
                return;

            _selectedIndex = (sbyte)newIndex;
        }

        private void ProcessManeuverActivation(ref FGameplayInput input)
        {
            if (!input.Fire)
                return;

            if (_selectedIndex == -1)
                return;

            FItemData itemData = _pc.Inventory.GetItemAtLoadoutSlot(SelectedSlot);

            if (!itemData.IsValid())
                return;

            ItemDefinition itemDefinition = Global.Tables.ItemTable.TryGetDefinition(itemData.DefinitionID);

            if (itemDefinition == null)
                return;

            if (itemDefinition is not SummonableDefinition summonable)
                return;

            ManeuverDefinition maneuverDefinition = summonable.ManeuverDefinition;

            if (maneuverDefinition == null)
                return;

            _activeManeuverTick = Runner.Tick;
            _activeManeuverTimer = TickTimer.CreateFromSeconds(Runner, maneuverDefinition.Duration);
            _activeManeuverSlot = SelectedSlot;

            _activeSummoningManeuverId = (ushort)maneuverDefinition.TableID;

            maneuverDefinition.StartExecute(_pc, this, Runner);
        }

        private void ProcessActiveManeuver(ref FGameplayInput input)
        {
            SummonableManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            if (activeManeuver.InputType == EInputType.Held)
            {
                if (input.FireHeld)
                {
                    _activeManeuverTimer = TickTimer.CreateFromSeconds(Runner, activeManeuver.Duration);
                }
            }

            if (_activeManeuverTimer.ExpiredOrNotRunning(Runner))
                return;

            int ticksSinceStart = Runner.Tick - _activeManeuverTick;
            SustainManeuver(activeManeuver, ticksSinceStart);
            activeManeuver.CheckExpiredItem(_pc, _activeManeuverSlot, Runner, ticksSinceStart);
        }

        private int _lastProcessedTick = -1;

        public void SustainManeuver(ManeuverDefinition definition, int ticksSinceStart)
        {
            for (int t = _lastProcessedTick + 1; t <= ticksSinceStart; t++)
            {
                // Timed projectiles
                for (int i = 0; i < definition.TimedProjectiles.Count; i++)
                {
                    var projectile = definition.TimedProjectiles[i];   // ref to the actual list element
                    if (projectile.SpawnTick == t)
                    {
                        definition.SpawnProjectile(_pc, ref projectile, Runner.Tick);
                    }
                }

                // Cyclic projectiles
                if (t >= definition.ProjectileCycleDelayTicks && definition.ProjectileTicksPerCycle > 0)
                {
                    int cycleTicksElapsed = t - definition.ProjectileCycleDelayTicks;
                    int currentCycleTick = cycleTicksElapsed % definition.ProjectileTicksPerCycle;

                    for (int i = 0; i < definition.CycleProjectiles.Count; i++)
                    {
                        var projectile = definition.CycleProjectiles[i];
                        if (projectile.SpawnTick == currentCycleTick)
                        {
                            definition.SpawnProjectile(_pc, ref projectile, Runner.Tick);
                        }
                    }
                }

                // Timed actions
                for (int i = 0; i < definition.TimedActions.Count; i++)
                {
                    var action = definition.TimedActions[i];   // ref to the actual list element
                    if (action.SpawnTick == t)
                    {
                        action.Definition.Execute(_pc, Runner);
                    }
                }
            }

            _lastProcessedTick = ticksSinceStart;

            // Optional: keep for overrides / custom per-tick logic
            definition.SustainExecute(_pc, Runner, ticksSinceStart);
        }

        private SummonableManeuverDefinition GetActiveManeuver()
        {
            if (_activeSummoningManeuverId == 0)
                return null;

            return Global.Tables.ManeuverTable.TryGetDefinition(_activeSummoningManeuverId) as SummonableManeuverDefinition;
        }

        private void ProcessManeuverExpiration()
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            if (_activeManeuverTimer.ExpiredOrNotRunning(Runner))
            {
                activeManeuver.EndExecute(_pc, this, Runner);
                return;
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyStartExecute(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(_activeSummoningManeuverId);

            int weaponId = _pc.Weapons.GetWeaponID();
            var animationState = maneuver.UpperBodyAnimationStates[weaponId];

            if (!maneuver.Fullbody)
            {
                _pc.AnimationController.SetAnimationForUpperBodyTrigger(animationState);
            }

            _pc.Aim.TargetPitchOffset = animationState.PitchOffset;
            _pc.Aim.TargetYawOffset = animationState.YawOffset;
            _pc.Aim.TargetRollOffset = animationState.RollOffset;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyEndExecute(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(maneuverDefinitionID);

            FUpperBodyAnimationTrigger upperBodyAnimationTrigger = new FUpperBodyAnimationTrigger();
            _pc.AnimationController.SetAnimationForUpperBodyTrigger(upperBodyAnimationTrigger);
            
            _pc.Aim.TargetPitchOffset = 0;
            _pc.Aim.TargetYawOffset = 0;

            _activeSummoningManeuverId = 0;
        }

    }
}
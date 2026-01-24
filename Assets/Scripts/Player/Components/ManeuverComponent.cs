using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace LichLord
{
    public class ManeuverComponent : NetworkBehaviour
    {
        [SerializeField] private PlayerCharacter _pc;

        [SerializeField]
        private EManeuverList _maneuverList;

        [Header("Maneuvers")]
        //Spells
        [SerializeField] private List<ManeuverDefinition> _spellManeuvers = new List<ManeuverDefinition>();
        public IReadOnlyList<ManeuverDefinition> SpellManeuvers => _spellManeuvers;

        [Networked, Capacity(5)]
        private NetworkDictionary<sbyte, TickTimer> _spellCooldownTimers { get; }

        //Commands
        [SerializeField] private List<ManeuverDefinition> _commandManeuvers = new List<ManeuverDefinition>();
        public IReadOnlyList<ManeuverDefinition> CommandManeuvers => _commandManeuvers;

        [Networked, Capacity(3)]
        private NetworkDictionary<sbyte, TickTimer> _commandCooldownTimers { get; }

        [SerializeField]
        private ManeuverDefinition _swapWeaponManeuver;

        [Networked]
        private TickTimer _swapWeaponCooldownTimer { get; set; }

        [SerializeField]
        private ManeuverDefinition _weaponAttackManeuver;

        [Networked]
        private TickTimer _weaponAttackCooldownTimer { get; set; }

        [Networked] private sbyte _selectedIndex { get; set; }

        [Networked] private sbyte _activeManeuverId { get; set; }

        [Networked]
        private TickTimer _activeManeuverTimer { get; set; }
        private int _activeManeuverTick;

        private float _moveSpeedMultiplier = 1f;

        private int _lastProcessedTick = -1;

        public float GetMoveSpeedMultiplier()
        { 
            if(_activeManeuverId < 0)
                return 1f;

            return _moveSpeedMultiplier;
        }

        public override void Spawned()
        {
            base.Spawned();
            ReplicateToAll(false);

            if (HasStateAuthority)
            {
                _activeManeuverTimer = TickTimer.None;

                for (int i = 0; i < _spellManeuvers.Count; i++)
                    _spellCooldownTimers.Set((sbyte)i, TickTimer.None);

                for (int i = 0; i < _commandManeuvers.Count; i++)
                    _commandCooldownTimers.Set((sbyte)i, TickTimer.None);
            }
        }

        public void ProcessInput(ref FGameplayInput input)
        {
            ProcessManeuverSelection(ref input);
            ProcessManeuverActivation(ref input);
            ProcessWeaponAttackActivation(ref input);
            ProcessWeaponSwapActivation(ref input);
            ProcessActiveManeuver(ref input);
        }

        public void OnFixedUpdate()
        {
            ProcessManeuverExpiration();
        }

        private void ProcessActiveManeuver(ref FGameplayInput input)
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            ManeuverDefinition selectedManeuver = GetSelectedManeuver();

            if (activeManeuver != null &&
                selectedManeuver != activeManeuver)
                return;

            if (activeManeuver.InputType == EInputType.Held)
            {
                if (input.FireHeld)
                {
                    _activeManeuverTimer = TickTimer.CreateFromSeconds(Runner, activeManeuver.Duration);
                }
            }

            if (!_activeManeuverTimer.ExpiredOrNotRunning(Runner))
            {
                int ticksSinceStart = Runner.Tick - _activeManeuverTick;
                SustainManeuver(activeManeuver, ticksSinceStart);
            }
        }

        public void SustainManeuver(ManeuverDefinition definition, int ticksSinceStart)
        {
            for (int t = _lastProcessedTick + 1; t <= ticksSinceStart; t++)
            {
                // Timed Projectiles
                for (int i = 0; i < definition.TimedProjectiles.Count; i++)
                {
                    var projectile = definition.TimedProjectiles[i];   // ref to the actual list element
                    if (projectile.SpawnTick == t)
                    {
                        definition.SpawnProjectile(_pc, ref projectile, Runner.Tick);
                    }
                }

                // Cycle Projectiles
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

                // Timed Actions
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

            definition.SustainExecute(_pc, Runner, ticksSinceStart);
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

        public void OnExitState()
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            activeManeuver.EndExecute(_pc, this, Runner);
        }

        private void ProcessManeuverActivation(ref FGameplayInput input)
        {
            var maneuverList = GetManeuverList();
            if (maneuverList == null)
                return;

            // if the selected index is out of range, early out
            if (_selectedIndex < 0 || _selectedIndex >= maneuverList.Count)
                return;
 
            // Cache current selected maneuver
            ManeuverDefinition selectedManeuver = GetSelectedManeuver();

            // if the cooldown timer doesn't exist for this selected index, early out
            if(!TryGetCooldownTimer(_selectedIndex, out TickTimer cooldownTimer))
            {
                Debug.Log("Maneuver cooldown timer doesn't exist for index " + _selectedIndex);
                return;
            }

            // If the event is on cooldown, early out
            if (!cooldownTimer.ExpiredOrNotRunning(Runner))
            {
                //Debug.Log("Maneuver cooldown timer is running for " + _selectedIndex);
                return;
            }

            ManeuverDefinition activeManeuver = GetActiveManeuver();

            if (activeManeuver != null)
                return;

            if (input.FireHeld)
            {
                Debug.Log($"[ActionManager] Executing action: {GetSelectedManeuver().ManeuverName} (Index: {_selectedIndex})");

                _activeManeuverTick = Runner.Tick;
                _activeManeuverId = (sbyte)maneuverList[_selectedIndex].TableID;
                _activeManeuverTimer = TickTimer.CreateFromSeconds(Runner, selectedManeuver.Duration);

                activeManeuver = GetActiveManeuver();
                activeManeuver.StartExecute(_pc, this, Runner);
                _lastProcessedTick = Runner.Tick;
            }
        }

        private void ProcessWeaponAttackActivation(ref FGameplayInput input)
        {
            // If the event is on cooldown, early out
            if (!_weaponAttackCooldownTimer.ExpiredOrNotRunning(Runner))
            {
                //Debug.Log("Maneuver cooldown timer is running for " + _selectedIndex);
                return;
            }

            // Cache current selected maneuver
            ManeuverDefinition activeManeuver = GetActiveManeuver();

            if (activeManeuver != null)
                return;

            if (input.AltFire)
            {
                _activeManeuverTick = Runner.Tick;
                _activeManeuverId = (sbyte)_weaponAttackManeuver.TableID;
                _activeManeuverTimer = TickTimer.CreateFromSeconds(Runner, _weaponAttackManeuver.Duration);

                activeManeuver = GetActiveManeuver();
                activeManeuver.StartExecute(_pc, this, Runner);
                _lastProcessedTick = Runner.Tick;
            }
        }

        private void ProcessWeaponSwapActivation(ref FGameplayInput input)
        {
            // If the event is on cooldown, early out
            if (!_swapWeaponCooldownTimer.ExpiredOrNotRunning(Runner))
            {
                //Debug.Log("Maneuver cooldown timer is running for " + _selectedIndex);
                return;
            }

            // Cache current selected maneuver
            ManeuverDefinition activeManeuver = GetActiveManeuver();

            if (activeManeuver != null)
                return;

            if (input.SwapWeapon)
            {
                _activeManeuverTick = Runner.Tick;
                _activeManeuverId = (sbyte)_swapWeaponManeuver.TableID;
                _activeManeuverTimer = TickTimer.CreateFromSeconds(Runner, _swapWeaponManeuver.Duration);

                activeManeuver = GetActiveManeuver();
                activeManeuver.StartExecute(_pc, this, Runner);
                _lastProcessedTick = Runner.Tick;
            }
        }

        public float GetCooldownPercent(int slot)
        {
            var maneuverList = GetManeuverList();
            if (maneuverList == null)
                return 0f;

            // if the cooldown timer doesn't exist for this selected index, early out
            if (!TryGetCooldownTimer((sbyte)slot, out TickTimer cooldownTimer))
                return 0f;

            ManeuverDefinition definition = maneuverList[slot];
            if (definition.Cooldown == 0)
                return 0;

            float? remainingTime = cooldownTimer.RemainingTime(Runner); // Use float? to accept nullable float

            // Handle the nullable case
            if (!remainingTime.HasValue)
            {
                return 0f; // Or another default value, depending on your requirements
            }

            return (remainingTime.Value / definition.Cooldown);
        }

        public void UpdateMoveSpeed(float deltaTime)
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();

            if (activeManeuver != null)
            {
                _moveSpeedMultiplier = Mathf.Lerp(_moveSpeedMultiplier, activeManeuver.MovementSpeedMultiplier, deltaTime * 4f);
            }
            else
            {
                _moveSpeedMultiplier = Mathf.Lerp(_moveSpeedMultiplier, 1, deltaTime * 4f);
            }
        }

        private void ProcessManeuverSelection(ref FGameplayInput input)
        {
            var maneuverList = GetManeuverList();

            if (maneuverList == null)
                return;

            int newIndex = -1;
            if (input.ScrollDelta != 0 && maneuverList.Count > 1)
            {
                int delta = input.ScrollDelta > 0 ? 1 : -1;
                newIndex = (_selectedIndex + delta + maneuverList.Count) % maneuverList.Count;
                //Debug.Log($"[ActionManager] ScrollDelta={input.ScrollDelta}, Delta={delta}, NewIndex={newIndex}");
            }

            if (input.ActionSelection > 0)
                newIndex = input.ActionSelection - 1;

            if (newIndex >= maneuverList.Count)
                return;

            if (newIndex < 0)
                return;

            if (newIndex == _selectedIndex)
                return;

            UpdateActionSelection(newIndex);
        }

        private void UpdateActionSelection(int newIndex)
        {
            if (!HasStateAuthority)
                return;

            var maneuverList = GetManeuverList();

            if (_selectedIndex >= 0 && _selectedIndex < maneuverList.Count)
            {
                maneuverList[_selectedIndex].DeselectManeuver(_pc, Runner);
            }

            _selectedIndex = (sbyte)newIndex;

            if (newIndex >= 0 && newIndex < maneuverList.Count)
            {
                maneuverList[newIndex].SelectManeuver(_pc, Runner);
            }

            if (newIndex >= 0 && newIndex < maneuverList.Count)
            {
                Debug.Log($"[ActionManager] Selected action: {maneuverList[newIndex].ManeuverName} (Index: {newIndex})");
            }
            else
            {
                Debug.Log("[ActionManager] Action selection cleared");
            }            
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyStartExecute(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(maneuverDefinitionID);

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

            if (!maneuver.Fullbody)
            {
                FUpperBodyAnimationTrigger upperBodyAnimationTrigger = new FUpperBodyAnimationTrigger();
                _pc.AnimationController.SetAnimationForUpperBodyTrigger(upperBodyAnimationTrigger);
            }

            if (maneuver == _swapWeaponManeuver)
            {
                _swapWeaponCooldownTimer = TickTimer.CreateFromSeconds(Runner, maneuver.Cooldown);
            }
            else if (maneuver == _weaponAttackManeuver)
            {
                _weaponAttackCooldownTimer = TickTimer.CreateFromSeconds(Runner, maneuver.Cooldown);
            }
            else
            {
                if (_spellCooldownTimers.TryGet(_activeManeuverId, out TickTimer cooldownTimer))
                {
                    cooldownTimer = TickTimer.CreateFromSeconds(Runner, maneuver.Cooldown);
                    _spellCooldownTimers.Set(_selectedIndex, cooldownTimer);
                }
            }
            
            _pc.Aim.TargetPitchOffset = 0;
            _pc.Aim.TargetYawOffset = 0;

            _activeManeuverId = -1;
        }

        public int GetAvailableActionsCount()
        {
            return _spellManeuvers.Count;
        }

        int _lastSpellIndex = -1;
        int _lastCommandIndex = -1;

        public void OnEnterState(EManeuverList maneuverList)
        {
            if (!HasStateAuthority)
                return;

            _maneuverList = maneuverList;

            int selectedIndex = 0;

            switch (maneuverList)
            {
                case EManeuverList.Spells:
                    if (_spellManeuvers.Count == 0)
                        return;

                    if(_lastSpellIndex >= 0)
                        selectedIndex = _lastSpellIndex;
                    break;
                case EManeuverList.Commands:
                    if (_commandManeuvers.Count == 0)
                        return;

                    if (_lastCommandIndex >= 0)
                        selectedIndex = _lastCommandIndex;
                    break;
            }

            UpdateActionSelection(selectedIndex);
        }

        private List<ManeuverDefinition> GetManeuverList()
        {
            switch (_maneuverList)
            {
                case EManeuverList.Spells:
                    return _spellManeuvers;
                case EManeuverList.Commands:
                    return _commandManeuvers;
            }

            return null;
        }

        public ManeuverDefinition GetActiveManeuver()
        {
            if (_activeManeuverId < 0)
                return null;

            return Global.Tables.ManeuverTable.TryGetDefinition(_activeManeuverId);
        }

        public ManeuverDefinition GetSelectedManeuver()
        {
            var maneuvers = GetManeuverList();

            if( maneuvers == null ) 
                return null;

            return maneuvers[_selectedIndex];
        }

        private bool TryGetCooldownTimer(sbyte selectedIndex, out TickTimer cooldownTimer)
        {
            switch (_maneuverList)
            {
                case EManeuverList.Spells:
                    if (_spellCooldownTimers.TryGet(selectedIndex, out TickTimer spellTimer))
                    {
                        cooldownTimer = spellTimer;
                        return true;
                    }
                        break;

                case EManeuverList.Commands:
                    if (_commandCooldownTimers.TryGet(selectedIndex, out TickTimer commandTimer))
                    {
                        cooldownTimer = commandTimer;
                        return true;
                    }
                        break;
            }

            cooldownTimer = new TickTimer();
            return false;
        }
    }

    public struct FManeuverProjectileTracker
    {
        public List<int> ProjectileTicks;
        public List<int> CycleProjectileTicks;
    }

    public enum EManeuverList
    {
        None,
        Spells,
        Commands,
    }
}
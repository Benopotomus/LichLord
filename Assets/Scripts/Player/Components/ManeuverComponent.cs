using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System;
using DWD.Pooling;

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

        [Networked]
        private int _activeManeuverTick { get; set; }

        private VisualEffectSpawner _visualEffectSpawner = new VisualEffectSpawner();
        private StandaloneVisualEffect _maneuverTargetVisualInstance;

        private Vector3 _maneuverTargetPosition;
        public Vector3 ManeuverTargetPosition => _maneuverTargetPosition;

        private float _moveSpeedMultiplier = 1f;

        private int _lastProcessedFixedUpdateTick = -1;
        private int _lastProcessedRenderTick = -1;

        public Action<ManeuverDefinition> OnSelectedManeuverChanged;
        public Action<ManeuverDefinition> OnActiveManeuverChanged;
        public Action<ManeuverDefinition, int> OnActiveManeuverUpdated;

        public float GetMoveSpeedMultiplier()
        {
            if (_activeManeuverId < 0)
                return 1f;

            return _moveSpeedMultiplier;
        }

        public override void Spawned()
        {
            base.Spawned();

            _visualEffectSpawner.OnLoaded += OnVisualEffectSpawned;

            if (HasStateAuthority)
            {
                _activeManeuverTimer = TickTimer.None;

                for (int i = 0; i < _spellManeuvers.Count; i++)
                    _spellCooldownTimers.Set((sbyte)i, TickTimer.None);

                for (int i = 0; i < _commandManeuvers.Count; i++)
                    _commandCooldownTimers.Set((sbyte)i, TickTimer.None);
            }
        }

        private void OnVisualEffectSpawned(GameObject loadedGameObject, Vector3 position, Quaternion rotation)
        {
            var poolObject = loadedGameObject.GetComponent<DWDObjectPoolObject>();

            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn Visuals Prefab for Impact");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAt(poolObject, position, rotation);
            if (instance is StandaloneVisualEffect standaloneEffect)
            {
                standaloneEffect.Initialize();
                _maneuverTargetVisualInstance = standaloneEffect;
            }
        }

        public void ProcessInput(ref FGameplayInput input)
        {
            ProcessManeuverSelection(ref input);
            ProcessManeuverTargeting();
            ProcessManeuverActivation(ref input);
            //ProcessWeaponAttackActivation(ref input);
            ProcessWeaponSwapActivation(ref input);
            ProcessActiveManeuver(ref input);
        }

        public void OnFixedUpdate(int tick)
        {
            ProcessManeuverExpiration();
            RefreshManeuvers();
        }

        private void ProcessManeuverTargeting()
        {
             var selectedManeuver = GetSelectedManeuver();
            if (selectedManeuver == null)
                return;

            _maneuverTargetPosition = selectedManeuver.GetTargetPosition(_pc, Runner);

            if (_maneuverTargetVisualInstance != null)
            { 
                _maneuverTargetVisualInstance.CachedTransform.position = _maneuverTargetPosition;
            }
        }

        private void ProcessActiveManeuver(ref FGameplayInput input)
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            ManeuverDefinition selectedManeuver = GetSelectedManeuver();

            if (activeManeuver != null &&
                activeManeuver != selectedManeuver &&
                activeManeuver != selectedManeuver.AltFireManeuver)
                return;

            int ticksSinceStart = Runner.Tick - _activeManeuverTick;

            if (activeManeuver.InputType == EInputType.Held)
            {
                if (activeManeuver.MaxHeldTicks > 0 &&
                    ticksSinceStart > activeManeuver.MaxHeldTicks)
                    return;

                switch (activeManeuver.FireButton)
                {
                    case EFireButton.Fire:
                        if (input.FireHeld)
                        {
                            _activeManeuverTimer = TickTimer.CreateFromTicks(Runner, activeManeuver.DurationTicks);
                        }
                        break;
                    case EFireButton.AltFire:
                        if (input.AltFireHeld)
                        {
                            _activeManeuverTimer = TickTimer.CreateFromTicks(Runner, activeManeuver.DurationTicks);
                        }
                        break;
                }

            }

            if (!_activeManeuverTimer.ExpiredOrNotRunning(Runner))
            {
                SustainManeuver(activeManeuver, ticksSinceStart);
            }
        }

        public void SustainManeuver(ManeuverDefinition definition, int ticksSinceStart)
        {
            for (int t = _lastProcessedFixedUpdateTick + 1; t <= ticksSinceStart; t++)
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

                /*
                // Timed Muzzle VFX
                for (int i = 0; i < definition.TimedMuzzleEffects.Length; i++)
                {
                    var muzzleVisual = definition.TimedMuzzleEffects[i];   // ref to the actual list element
                    if (muzzleVisual.SpawnTick == t)
                    {
                        Transform attachment = MuzzleUtility.GetMuzzleTransform(_pc, muzzleVisual.Muzzle);
                        Quaternion rotation = _pc.IK.CameraPivot.rotation;
                        _pc.Context.VFXManager.SpawnVisualEffectAttached(attachment, rotation, muzzleVisual.MuzzleEffectPrefab);
                    }
                }

                // Cycle Muzzle VFX
                if (t >= definition.MuzzleCycleDelayTicks && definition.MuzzleTicksPerCycle > 0)
                {
                    int cycleTicksElapsed = t - definition.MuzzleCycleDelayTicks;
                    int currentCycleTick = cycleTicksElapsed % definition.MuzzleTicksPerCycle;

                    for (int i = 0; i < definition.CycleMuzzleEffects.Length; i++)
                    {
                        var muzzleVisual = definition.CycleMuzzleEffects[i];
                        if (muzzleVisual.SpawnTick == currentCycleTick)
                        {
                            Transform attachment = MuzzleUtility.GetMuzzleTransform(_pc, muzzleVisual.Muzzle);
                            Quaternion rotation = _pc.IK.CameraPivot.rotation;
                            _pc.Context.VFXManager.SpawnVisualEffectAttached(attachment, rotation, muzzleVisual.MuzzleEffectPrefab);
                        }
                    }
                }
                */
                // Timed Actions
                for (int i = 0; i < definition.TimedActions.Count; i++)
                {
                    var action = definition.TimedActions[i];   // ref to the actual list element
                    if (action.SpawnTick == t)
                    {
                        action.Definition.Execute(_pc, Runner);
                    }

                    if (ticksSinceStart > action.SpawnTick)
                        action.Definition.Sustain(_pc, Runner);

                }
            }

            _lastProcessedFixedUpdateTick = ticksSinceStart;

            definition.SustainExecute(_pc, Runner, ticksSinceStart);

            OnActiveManeuverUpdated?.Invoke(definition, ticksSinceStart);
        }

        private void ProcessManeuverExpiration()
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            if (!_activeManeuverTimer.ExpiredOrNotRunning(Runner))
                return;

            activeManeuver.EndExecute(_pc, this, Runner);
            OnActiveManeuverChanged?.Invoke(null);
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
            if (!TryGetCooldownTimer(_selectedIndex, out TickTimer cooldownTimer))
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

            if (input.Fire)
            {
                StartManeuver(selectedManeuver);
            }
            else if (input.AltFire)
            {
                if (selectedManeuver.AltFireManeuver != null)
                    StartManeuver(selectedManeuver.AltFireManeuver);
            }
        }

        private void StartManeuver(ManeuverDefinition selectedManeuver)
        {
            Debug.Log($"[ActionManager] Executing action: {selectedManeuver.ManeuverName} (Index: {_selectedIndex})");

            _activeManeuverTick = Runner.Tick;
            _activeManeuverId = (sbyte)selectedManeuver.TableID;
            _activeManeuverTimer = TickTimer.CreateFromTicks(Runner, selectedManeuver.DurationTicks);

            selectedManeuver.StartExecute(_pc, this, Runner);
            _lastProcessedFixedUpdateTick = Runner.Tick;
            OnActiveManeuverChanged?.Invoke(selectedManeuver);
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
                _activeManeuverTimer = TickTimer.CreateFromTicks(Runner, _swapWeaponManeuver.DurationTicks);

                activeManeuver = GetActiveManeuver();
                activeManeuver.StartExecute(_pc, this, Runner);
                _lastProcessedFixedUpdateTick = Runner.Tick;
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
            if (definition.CooldownTicks == 0)
                return 0;

            float? remainingTime = cooldownTimer.RemainingTicks(Runner); // Use float? to accept nullable float

            // Handle the nullable case
            if (!remainingTime.HasValue)
            {
                return 0f; // Or another default value, depending on your requirements
            }

            return ((float)remainingTime.Value / (float)definition.CooldownTicks);
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

            UpdateManeuverSelection(newIndex);
        }

        private void UpdateManeuverSelection(int newIndex)
        {
            if (!HasStateAuthority)
                return;

            var maneuverList = GetManeuverList();

            if (maneuverList.Count == 0)
                return;

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

            var newSelectedManeuver = GetSelectedManeuver();

            if (_maneuverTargetVisualInstance != null)
                _maneuverTargetVisualInstance.StartRecycle();

            if (newSelectedManeuver != null &&
                newSelectedManeuver.Targeting != null &&
                newSelectedManeuver.Targeting.VisualsPrefab.Name != "")
            {
                
                _visualEffectSpawner.SpawnVisualEffect(ManeuverTargetPosition, Quaternion.identity, newSelectedManeuver.Targeting.VisualsPrefab);
            }

            OnSelectedManeuverChanged?.Invoke(newSelectedManeuver);
        }

        public void OnRender(int tick)
        {
            ManeuverDefinition activeManeuver = GetActiveManeuver();
            if (activeManeuver == null)
                return;

            int ticksSinceStart = tick - _activeManeuverTick;

            UpdateRenderManeuver(activeManeuver, ticksSinceStart);
        }

        public void UpdateRenderManeuver(ManeuverDefinition definition, int ticksSinceStart)
        {
            for (int t = _lastProcessedRenderTick + 1; t <= ticksSinceStart; t++)
            {
                // Timed Muzzle VFX
                for (int i = 0; i < definition.TimedMuzzleEffects.Length; i++)
                {
                    var muzzleVisual = definition.TimedMuzzleEffects[i];   // ref to the actual list element
                    if (muzzleVisual.SpawnTick == t)
                    {
                        Transform attachment = MuzzleUtility.GetMuzzleTransform(_pc, muzzleVisual.Muzzle);
                        Quaternion rotation = _pc.IK.CameraPivot.rotation;
                        _pc.Context.VFXManager.SpawnVisualEffectAttached(attachment, rotation, muzzleVisual.MuzzleEffectPrefab);
                    }
                }

                // Cycle Muzzle VFX
                if (t >= definition.MuzzleCycleDelayTicks && definition.MuzzleTicksPerCycle > 0)
                {
                    int cycleTicksElapsed = t - definition.MuzzleCycleDelayTicks;
                    int currentCycleTick = cycleTicksElapsed % definition.MuzzleTicksPerCycle;

                    for (int i = 0; i < definition.CycleMuzzleEffects.Length; i++)
                    {
                        var muzzleVisual = definition.CycleMuzzleEffects[i];
                        if (muzzleVisual.SpawnTick == currentCycleTick)
                        {
                            Transform attachment = MuzzleUtility.GetMuzzleTransform(_pc, muzzleVisual.Muzzle);
                            Quaternion rotation = _pc.IK.CameraPivot.rotation;
                            _pc.Context.VFXManager.SpawnVisualEffectAttached(attachment, rotation, muzzleVisual.MuzzleEffectPrefab);
                        }
                    }
                }

                if (!HasStateAuthority)
                    continue;

                // Timed CameraShakes
                for (int i = 0; i < definition.TimedCameraShakes.Length; i++)
                {
                    var cameraShake = definition.TimedCameraShakes[i];   // ref to the actual list element
                    if (cameraShake.SpawnTick == t)
                    {
                        _pc.Context.Camera.Shake(cameraShake.ShakeType,
                            overrideAmplitude: cameraShake.Amplitude,
                            overrideDuration: cameraShake.Duration);
                    }
                }

                // Cycle Camera Shakes
                if (t >= definition.CameraShakeCycleDelayTicks && definition.CameraShakeTicksPerCycle > 0)
                {
                    int cycleTicksElapsed = t - definition.CameraShakeCycleDelayTicks;
                    int currentCycleTick = cycleTicksElapsed % definition.CameraShakeTicksPerCycle;

                    for (int i = 0; i < definition.CycleCameraShakes.Length; i++)
                    {
                        var cameraShake = definition.CycleCameraShakes[i];
                        if (cameraShake.SpawnTick == currentCycleTick)
                        {
                            _pc.Context.Camera.Shake(cameraShake.ShakeType, 
                                overrideAmplitude: cameraShake.Amplitude,
                                overrideDuration: cameraShake.Duration);
                        }
                    }
                }
            }

            _lastProcessedRenderTick = ticksSinceStart;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_NotifyStartExecute(ushort maneuverDefinitionID)
        {
            ManeuverDefinition maneuver = Global.Tables.ManeuverTable.TryGetDefinition(maneuverDefinitionID);

            int weaponId = _pc.Weapons.GetWeaponID();
            var animationState = maneuver.UpperBodyAnimationStates[weaponId];
            _lastProcessedRenderTick = _activeManeuverTick;

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
                _swapWeaponCooldownTimer = TickTimer.CreateFromTicks(Runner, maneuver.CooldownTicks);
            }
            else if (maneuver == _weaponAttackManeuver)
            {
                _weaponAttackCooldownTimer = TickTimer.CreateFromTicks(Runner, maneuver.CooldownTicks);
            }
            else
            {
                if (_spellCooldownTimers.TryGet(_activeManeuverId, out TickTimer cooldownTimer))
                {
                    cooldownTimer = TickTimer.CreateFromTicks(Runner, maneuver.CooldownTicks);
                    _spellCooldownTimers.Set(_selectedIndex, cooldownTimer);
                }
            }

            _pc.Aim.TargetPitchOffset = 0;
            _pc.Aim.TargetYawOffset = 0;
            _pc.Aim.TargetRollOffset = 0;

            _activeManeuverId = -1;
        }

        private void RefreshManeuvers()
        {
            for (int i = 0; i < _spellManeuvers.Count; i++)
            {
                ManeuverDefinition maneuver = _spellManeuvers[i];
                if (maneuver.RefreshBehavior == null)
                    continue;

                if (maneuver.RefreshBehavior.ShouldManeuverSwap(_pc))
                {
                    _spellManeuvers[i] = maneuver.RefreshBehavior.NewBehavior;
                    UpdateManeuverSelection(_selectedIndex);
                }

            }
        
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

                    if (_lastSpellIndex >= 0)
                        selectedIndex = _lastSpellIndex;
                    break;
                case EManeuverList.Commands:
                    if (_commandManeuvers.Count == 0)
                        return;

                    if (_lastCommandIndex >= 0)
                        selectedIndex = _lastCommandIndex;
                    break;
            }

            UpdateManeuverSelection(selectedIndex);
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

            if (maneuvers == null)
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

    public enum EManeuverList
    {
        None,
        Spells,
        Commands,
    }
}
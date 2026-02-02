using AYellowpaper.SerializedCollections;
using DWD.Utility.Loading;
using Fusion;
using LichLord.Projectiles;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord
{

    [CreateAssetMenu(fileName = "Maneuver", menuName = "LichLord/Maneuvers/ManeuverDefinition", order = 1)]
    public class ManeuverDefinition : TableObject
    {
        [SerializeField]
        private string _maneuverName;
        public string ManeuverName => _maneuverName;

        [SerializeField]
        private string _activationTooltipText;
        public string ActivationTooltipText => _activationTooltipText;

        [SerializeField]
        private string _progressTooltipText;
        public string ProgressTooltipText => _progressTooltipText;

        [SerializeField] 
        private int _durationTicks = 32;
        public int DurationTicks => _durationTicks;

        [SerializeField] 
        private int _cooldownTicks = 32;
        public int CooldownTicks => _cooldownTicks;

        [SerializeField] // When the held button is down, this will automatically 
        private int _maxHeldTime = -1;
        public int MaxHeldTicks => _maxHeldTime;

        [SerializeField] 
        private EFireButton _fireButton;
        public EFireButton FireButton => _fireButton; 

        [SerializeField] 
        private EInputType _inputType;
        public EInputType InputType => _inputType;

        [SerializeField]
        private ManeuverTargetingDefinition _targeting;
        public ManeuverTargetingDefinition Targeting => _targeting;

        //Visuals
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _muzzleEffectPrefab;
        public BundleObject MuzzleEffectPrefab => _muzzleEffectPrefab;

        //UI
        [BundleObject(typeof(Sprite))]
        [SerializeField]
        protected BundleObject _icon;
        public BundleObject Icon => _icon;

        [Header("Animation")]
        private FAnimationTrigger _animationTrigger;
        public FAnimationTrigger AnimationTrigger => _animationTrigger;

        public bool Fullbody;

        [SerializeField]
        [SerializedDictionary("WeaponID", "AnimationState")]
        private SerializedDictionary<int, FUpperBodyAnimationTrigger> _upperBodyAnimationStates;
        public SerializedDictionary<int, FUpperBodyAnimationTrigger> UpperBodyAnimationStates => _upperBodyAnimationStates;

        [SerializeField]
        private float _animationSpeed = 1f;
        public float AnimationSpeed => _animationSpeed;

        [SerializeField]
        private float _movementSpeedMultiplier = 1f;
        public float MovementSpeedMultiplier => _movementSpeedMultiplier;

        [SerializeField]
        private List<FManeuverProjectile> _timedProjectiles = new List<FManeuverProjectile>();
        public List<FManeuverProjectile> TimedProjectiles => _timedProjectiles;

        [SerializeField]
        private List<FManeuverProjectile> _cycleProjectiles = new List<FManeuverProjectile>();
        public List<FManeuverProjectile> CycleProjectiles => _cycleProjectiles;

        [SerializeField]
        private int _projectileCycleDelayTicks;
        public int ProjectileCycleDelayTicks => _projectileCycleDelayTicks;

        [SerializeField]
        private int _projectileTicksPerCycle;
        public int ProjectileTicksPerCycle => _projectileTicksPerCycle;

        [SerializeField]
        private List<FManeuverAction> _timedActions;
        public List<FManeuverAction> TimedActions => _timedActions;

        [SerializeField]
        private ManeuverDefinition _altFireManeuver;
        public ManeuverDefinition AltFireManeuver => _altFireManeuver;

        [SerializeField]
        private ManeuverRefreshBehavior _refreshBehavior;
        public ManeuverRefreshBehavior RefreshBehavior => _refreshBehavior;

        [SerializeField]
        private int _squadId = -1;
        public int SquadId => _squadId;

        public virtual void SelectManeuver(PlayerCharacter pc, NetworkRunner runner) { }

        public virtual void DeselectManeuver(PlayerCharacter pc, NetworkRunner runner) { }

        public virtual Vector3 GetTargetPosition(PlayerCharacter pc, NetworkRunner runner)
        {
            if (Targeting == null)
                return pc.Context.Camera.CachedRaycastHit.position;

            return Targeting.GetTargetPosition(this, pc, runner);
        }

        public virtual void StartExecute(PlayerCharacter playerCharacter, Component component, NetworkRunner runner) 
        {
            if (component == null)
                return;

            if(component is ManeuverComponent maneuverComponent)
                maneuverComponent.RPC_NotifyStartExecute((ushort)TableID);

            if (component is SummonerComponent summonerComponent)
                summonerComponent.RPC_NotifyStartExecute((ushort)TableID);
        }

        public virtual void SustainExecute(PlayerCharacter playerCharacter, NetworkRunner runner, int ticksSinceStart)
        {
        }

        public virtual void EndExecute(PlayerCharacter playerCharacter, Component component, NetworkRunner runner) 
        {
            for (int i = 0; i < TimedActions.Count; i++)
                TimedActions[i].Definition.EndExecute(playerCharacter, runner);

            if (component == null)
                return;

            if (component is ManeuverComponent maneuverComponent)
                maneuverComponent.RPC_NotifyEndExecute((ushort)TableID);

            if (component is SummonerComponent summonerComponent)
                summonerComponent.RPC_NotifyEndExecute((ushort)TableID);
        }

        public void SpawnProjectile(PlayerCharacter pc, ref FManeuverProjectile projectileData, int tick)
        {
            ProjectileManager projectileManager = pc.Context.ProjectileManager;
            if (projectileManager == null)
                return;

            Vector3 targetPos = pc.Context.Camera.CachedRaycastHit.position;

            FProjectileFireEvent fireEvent = new FProjectileFireEvent();
            FProjectilePayload payload = new FProjectilePayload();
            FProjectilePayload payload_spawnedProjectile = new FProjectilePayload();
            payload.damagePotential.DamageValue = projectileData.Damage.DamageValue;
            payload.damagePotential.DamageType = projectileData.Damage.DamageType;

            payload_spawnedProjectile.damagePotential.DamageValue = projectileData.Damage.DamageValue;
            payload_spawnedProjectile.damagePotential.DamageType = projectileData.Damage.DamageType;

            ProjectileManager.CreateProjectileFireEvent(
                ref fireEvent,
                projectileData.Definition,
                pc,
                null,
                MuzzleUtility.GetMuzzlePosition(pc, projectileData.Muzzle),
                targetPos,
                tick,
                ref payload,
                ref payload_spawnedProjectile
            );

            var projectile = projectileManager.SpawnProjectile(fireEvent);
            //Debug.Log($"[GunActionData] Fired projectile with {ActionName} using ProjectileManager");
        }
    }

    public enum EFireButton
    { 
        None,
        Fire,
        AltFire,
    }

    public enum EInputType
    { 
        Pressed,
        Held,
    }
}
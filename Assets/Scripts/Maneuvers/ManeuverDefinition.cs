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
        public string ManeuverName;

        [SerializeField] private float _duration = 1f;
        public float Duration => _duration;

        public float Cooldown = 1f;

        public AudioClip ActionSound; // Sound played when performing action (e.g., FireSound for gun)

        [SerializeField] private EInputType _inputType;
        public EInputType InputType => _inputType;

        [SerializeField] private EMuzzle _muzzle;
        public EMuzzle Muzzle => _muzzle;

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

        public bool Fullbody; // Animator trigger (e.g., "Shoot" for gun)

        [SerializeField]
        [SerializedDictionary("WeaponID", "AnimationState")]
        private SerializedDictionary<int, FUpperBodyAnimationTrigger> _upperBodyAnimationStates;
        public SerializedDictionary<int, FUpperBodyAnimationTrigger> UpperBodyAnimationStates => _upperBodyAnimationStates;

        public float AnimationSpeed = 1f;

        public float MovementSpeedMultiplier = 1f; // Scales movement speed during action

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
        private List<ManeuverActionDefinition> _maneuverActions;
        public List<ManeuverActionDefinition> ManeuverActions => _maneuverActions;

        public virtual void SelectManeuver(PlayerCharacter playerCreature, NetworkRunner runner) { }

        public virtual void DeselectManeuver(PlayerCharacter playerCreature, NetworkRunner runner) { }

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

    public enum EInputType
    { 
        Pressed,
        Held,
    }
}
using AYellowpaper.SerializedCollections;
using DWD.Utility.Loading;
using Fusion;
using LichLord.Projectiles;
using System;
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

        [SerializeField]
        private List<FManeuverProjectile> _cycleProjectiles = new List<FManeuverProjectile>();

        [SerializeField]
        private int _projectileCycleDelayTicks;

        [SerializeField]
        private int _projectileTicksPerCycle;

        [SerializeField]
        private List<ManeuverActionDefinition> _maneuverActions;
        public List<ManeuverActionDefinition> ManeuverActions => _maneuverActions;

        public virtual void SelectAction(PlayerCharacter playerCreature, NetworkRunner runner) { }

        public virtual void DeselectAction(PlayerCharacter playerCreature, NetworkRunner runner) { }

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
            for (int i = 0; i < _timedProjectiles.Count; i++)
            { 
                var projectile  = _timedProjectiles[i];
                if (projectile.SpawnTick == ticksSinceStart)
                {
                    SpawnProjectile(playerCharacter, ref projectile, runner.Tick);
                }
            }

            // Handle cyclic projectiles after delay
            if (ticksSinceStart < _projectileCycleDelayTicks || _projectileTicksPerCycle <= 0)
                return;

            // Calculate tick within the current cycle
            int cycleTicksElapsed = ticksSinceStart - _projectileCycleDelayTicks;
            int currentCycleTick = cycleTicksElapsed % _projectileTicksPerCycle;

            for (int i = 0; i < _cycleProjectiles.Count; i++)
            {
                var projectile = _cycleProjectiles[i];
                if (projectile.SpawnTick == currentCycleTick)
                {
                    SpawnProjectile(playerCharacter, ref projectile, runner.Tick);
                }
            }
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

        private void SpawnProjectile(PlayerCharacter pc, ref FManeuverProjectile projectileData, int tick)
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
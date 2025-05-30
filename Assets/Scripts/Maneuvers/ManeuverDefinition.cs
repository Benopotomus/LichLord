using Fusion;
using LichLord.Projectiles;
using UnityEngine;

namespace LichLord
{

    [CreateAssetMenu(fileName = "ActionData", menuName = "LichLord/ActionData", order = 1)]
    public class ManeuverDefinition : TableObject
    {
        public string ActionName;
        public int Damage = 10;
        public float Cooldown = 1f;
        public GameObject ProjectilePrefab; // For spell/gun projectiles (null for melee or raycast gun)
        public float ProjectileSpeed = 20f; // For spell/gun projectiles
        public float Range = 100f; // Range for raycast (spells, melee, gun)
        public AudioClip ActionSound; // Sound played when performing action (e.g., FireSound for gun)
        public ParticleSystem ActionEffect; // VFX played when performing action (e.g., MuzzleParticle for gun)
        public GameObject ImpactPrefab; // Impact effect for gun hits
        public LayerMask HitMask; // Layers to hit for gun/spell raycasts
        public string AnimationTrigger; // Animator trigger (e.g., "Shoot" for gun)
        public float AnimationDuration = 1f; // Duration of the action animation
        [Range(0.01f, 1f)]
        public float MovementSpeedMultiplier = 1f; // Scales movement speed during action

        public virtual void SelectAction(PlayerCreature playerCreature, NetworkRunner runner)
        {        
        }

        public virtual void DeselectAction(PlayerCreature playerCreature, NetworkRunner runner)
        { 
        }

        public virtual void Execute(PlayerCreature playerCreature, NetworkRunner runner)
        {
        }
    }


}
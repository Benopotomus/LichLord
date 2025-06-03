using UnityEngine;

namespace LichLord
{
    [CreateAssetMenu(fileName = "SpellData", menuName = "LichLord/SpellData", order = 1)]
    public class SpellData : ScriptableObject
    {
        public string SpellName;
        public float Damage = 10f;
        public float Cooldown = 1f;
        public GameObject ProjectilePrefab; // Optional: For projectile-based spells
        public float ProjectileSpeed = 20f; // Speed for projectile-based spells
        public float CastRange = 100f; // Range for raycast-based spells
        public AudioClip CastSound; // Sound played when casting
        public ParticleSystem CastEffect; // VFX played when casting
        public string AnimationTrigger; // Animator trigger name for the spell
        public float AnimationDuration = 1f; // Duration of the casting animation
        [Range(0f, 1f)]
        public float MovementSpeedMultiplier = 1f; // Scales movement speed during casting (0 = stopped, 1 = full speed)
    }
}
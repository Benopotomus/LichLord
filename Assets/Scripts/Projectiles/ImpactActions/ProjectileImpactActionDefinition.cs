using LichLord.NonPlayerCharacters;
using UnityEngine;
using Fusion;
using System.Collections.Generic;
using DWD.Utility.Loading;

namespace LichLord.Projectiles
{
    [CreateAssetMenu(menuName = "LichLord/Projectiles/ProjectileImpactActionDefinition")]
    public class ProjectileImpactActionDefinition : ScriptableObject
    {
        //Visuals
        [BundleObject(typeof(GameObject))]
        [SerializeField]
        protected BundleObject _visualsPrefab;
        public BundleObject VisualsPrefab => _visualsPrefab;

        public virtual void Trigger(ref FProjectileData data, Projectile projectile)
        {
            Quaternion rotation = Quaternion.LookRotation((data.Position.Position - data.TargetPosition.Position).normalized);
            projectile.Context.VFXManager.SpawnVisualEffect(data.TargetPosition.Position, rotation, VisualsPrefab);
        }
    }
}

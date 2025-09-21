using AYellowpaper.SerializedCollections;
using LichLord.Projectiles;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.Player
{
    [CreateAssetMenu(fileName = "InteractorActionDefinition", menuName = "LichLord/Interactor/InteractorActionDefinition", order = 1)]
    public class InteractorActionDefinition : ScriptableObject
    {
        [SerializeField]
        private VisualEffectBeam _beamPrefab;
        public VisualEffectBeam BeamPrefab => _beamPrefab;

        [SerializeField]
        [SerializedDictionary("WeaponID", "Muzzle")]
        private SerializedDictionary<int, EMuzzle> _beamMuzzles;
        public SerializedDictionary<int, EMuzzle> BeamMuzzle => _beamMuzzles;

        [SerializeField]
        [SerializedDictionary("WeaponID", "AnimationTrigger")]
        private SerializedDictionary<int, FUpperBodyAnimationTrigger> _animationUpperBodyTrigger;
        public SerializedDictionary<int, FUpperBodyAnimationTrigger> AnimationUpperBodyTrigger => _animationUpperBodyTrigger;

        public void OnEnterStateRender(InteractorComponent interactor)
        {
            PlayerCharacter pc = interactor.PC;

            if(pc == null)
                return;

            int weaponId = pc.Weapons.GetWeaponID();

            if (_animationUpperBodyTrigger.TryGetValue(weaponId, out var trigger))
            {
                pc.AnimationController.SetAnimationForUpperBodyTrigger(trigger);
                pc.Aim.TargetPitchOffset = trigger.PitchOffset;
                pc.Aim.TargetYawOffset = trigger.YawOffset;
                pc.Aim.TargetRollOffset = trigger.RollOffset;
            }

            if (_beamPrefab != null)
            {
                if (_beamMuzzles.TryGetValue(weaponId, out var muzzle))
                {
                    interactor.SpawnBeamEffect(_beamPrefab, muzzle);
                }
            }
        }

    }
}

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
        private int _animationUpperBodyTriggerNumber;
        public int AnimationUpperBodyTriggerNumber => _animationUpperBodyTriggerNumber;

        [SerializeField]
        private float _pitchOffset = 0f;

        [SerializeField]
        private float _yawOffset = 0f;

        [SerializeField]
        private float _rollOffset = 0f;

        public void OnEnterStateRender(InteractorComponent interactor)
        {
            PlayerCharacter pc = interactor.PC;
            FUpperBodyAnimationTrigger upperBodyAnimationTrigger = new FUpperBodyAnimationTrigger();
            upperBodyAnimationTrigger.UpperbodyTriggerNumber = _animationUpperBodyTriggerNumber;

            pc.AnimationController.SetAnimationForUpperBodyTrigger(upperBodyAnimationTrigger);
            pc.Aim.TargetPitchOffset = _pitchOffset;
            pc.Aim.TargetYawOffset = _yawOffset;
            pc.Aim.TargetRollOffset = _rollOffset;

            if (_beamPrefab != null)
            {
                interactor.SpawnBeamEffect(_beamPrefab);
            }
        }

    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIReticle : UIWidget
    {
        [SerializeField] 
        private Image _reticleImage; // Reference to the reticle UI element

        [SerializeField]
        private UIManeuverReticleContainer _maneuverReticleContainer;

        [SerializeField]
        private UICommandReticleContainer _commandReticleContainer;

        private PlayerCharacter _pc;
        private SceneCamera _sceneCamera;

        protected override void OnVisible()
        {
            base.OnVisible();
            StartCoroutine(BindPlayerCharacter());
            StartCoroutine(BindSceneCamera());
        }

        protected override void OnHidden()
        {
            if (_pc != null)
            {
                _pc.Maneuvers.OnSelectedManeuverChanged += OnSelectedManeuverChanged;
                _pc.Maneuvers.OnActiveManeuverChanged += OnActiveManeuverChanged;
                _pc.Maneuvers.OnActiveManeuverUpdated += OnActiveManeuverUpdated;
                _pc.Commander.OnIsModifyingStanceChanged -= OnIsModifyingStanceChanged;
                _pc.Commander.OnDesiredSquadStanceChanged -= OnDesiredSquadStanceChanged;
                _pc = null;
            }

            base.OnHidden();
        }

        private IEnumerator BindSceneCamera()
        {
            if (Context.Camera == null)
                yield return null;

            _sceneCamera = Context.Camera;

            OnReticlePositionChanged(_sceneCamera.ReticlePosition);
            _sceneCamera.OnReticlePositionChanged += OnReticlePositionChanged;
        }

        private void OnReticlePositionChanged(Vector3 newPosition)
        {
            RectTransform.position = newPosition;
        }

        private IEnumerator BindPlayerCharacter()
        {
            while (Context.LocalPlayerCharacter == null)
            {
                yield return null;  // Wait one frame and check again
            }

            _pc = Context.LocalPlayerCharacter;

            OnSelectedManeuverChanged(_pc.Maneuvers.GetSelectedManeuver());
            OnActiveManeuverChanged(_pc.Maneuvers.GetActiveManeuver());
            _pc.Maneuvers.OnSelectedManeuverChanged += OnSelectedManeuverChanged;
            _pc.Maneuvers.OnActiveManeuverChanged += OnActiveManeuverChanged;
            _pc.Maneuvers.OnActiveManeuverUpdated += OnActiveManeuverUpdated;

            OnIsModifyingStanceChanged(0, false);
            _pc.Commander.OnIsModifyingStanceChanged += OnIsModifyingStanceChanged;
            _pc.Commander.OnDesiredSquadStanceChanged += OnDesiredSquadStanceChanged;
        }

        private void OnActiveManeuverUpdated(ManeuverDefinition definition, int ticksSinceStart)
        {
            _maneuverReticleContainer.OnActiveManeuverUpdated(definition, ticksSinceStart);
        }

        private void OnActiveManeuverChanged(ManeuverDefinition definition)
        {
            _maneuverReticleContainer.OnActiveManeuverChanged(definition);
            OnSelectedManeuverChanged(_pc.Maneuvers.GetSelectedManeuver());
        }

        private void OnSelectedManeuverChanged(ManeuverDefinition definition)
        {
            _maneuverReticleContainer.OnSelectedManeuverChanged(definition);
        }

        private void OnIsModifyingStanceChanged(int squadId, bool isModifying)
        {
            _maneuverReticleContainer.gameObject.SetActive(!isModifying);
            _commandReticleContainer.gameObject.SetActive(isModifying);
            _commandReticleContainer.OnIsModifyingStanceChanged(_pc, squadId, isModifying);
        }

        private void OnDesiredSquadStanceChanged(int squadId, ESquadStance stance)
        {
            _commandReticleContainer.OnDesiredSquadStanceChanged(_pc, squadId, stance);
        }
    }
}

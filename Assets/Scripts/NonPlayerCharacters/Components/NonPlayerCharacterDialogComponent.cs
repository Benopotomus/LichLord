using LichLord.Dialog;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterDialogComponent : MonoBehaviour
    {
        [SerializeField] private int _dialogIndex;
        [SerializeField] private DialogDefinition _dialogDefinition;
        public DialogDefinition CurrentDialog => _dialogDefinition;

        [SerializeField] private GameObject _indicator;

        private bool _shouldShowIndicator;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            _dialogIndex = runtimeState.GetDialogIndex();
            _shouldShowIndicator = GetShouldShowIndictor(runtimeState);
            _indicator.SetActive(_shouldShowIndicator);

            _dialogDefinition = runtimeState.GetDialogDefinition();
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState)
        {
            UpdateDialogChange(runtimeState);
            UpdateShouldShowIndicator(runtimeState);
        }

        private void UpdateDialogChange(NonPlayerCharacterRuntimeState runtimeState)
        {
            int oldDialogIndex = _dialogIndex;
            int newDialogIndex = runtimeState.GetDialogIndex();

            if (oldDialogIndex == newDialogIndex)
                return;

            _dialogIndex = newDialogIndex;
            GetShouldShowIndictor(runtimeState);

            DialogDefinition oldDialog = _dialogDefinition;
            DialogDefinition newDialog = runtimeState.GetDialogDefinition();

            if (oldDialog == newDialog)
                return;

            _dialogDefinition = newDialog;
        }

        public void UpdateShouldShowIndicator(NonPlayerCharacterRuntimeState runtimeState)
        {
            bool newShouldShow = GetShouldShowIndictor(runtimeState);

            if (_shouldShowIndicator != newShouldShow)
            {
                _shouldShowIndicator = newShouldShow;
                _indicator.SetActive(_shouldShowIndicator);
            }
        }

        public bool GetShouldShowIndictor(NonPlayerCharacterRuntimeState runtimeState)
        {
            if (!runtimeState.HasDialog())
                return false;

            if (runtimeState.GetHealth() > 0)
            {
                if (runtimeState.GetAttitude() == EAttitude.Hostile)
                    return false;

                if (runtimeState.IsInvader())
                {
                    if (runtimeState.Context.InvasionManager.InvasionID == 0)
                        return false;

                    if (runtimeState.Context.InvasionManager.InvasionState == EInvasionState.Retreating)
                        return false;
                }

                return true;
            }

            return false;
        }
    }
}

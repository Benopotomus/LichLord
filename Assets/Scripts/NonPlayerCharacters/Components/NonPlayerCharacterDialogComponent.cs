using LichLord.Dialog;
using UnityEngine;

namespace LichLord.NonPlayerCharacters
{
    public class NonPlayerCharacterDialogComponent : MonoBehaviour
    {
        [SerializeField] private bool _hasDialog;
        [SerializeField] private DialogDefinition _dialog;

        [SerializeField] private GameObject _indicator;  

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            _hasDialog = runtimeState.HasDialog();
            _indicator.SetActive(_hasDialog);
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState)
        {
            UpdateDialogChange(runtimeState);
        }

        private void UpdateDialogChange(NonPlayerCharacterRuntimeState runtimeState)
        {
            bool oldHasDialog = _hasDialog;
            bool newHasDialog = runtimeState.HasDialog();

            if (oldHasDialog == newHasDialog)
                return;

            _indicator.SetActive(newHasDialog);

            DialogDefinition oldDialog = _dialog;
            DialogDefinition newDialog = runtimeState.GetDialog();

            if (oldDialog == newDialog)
                return;

            _dialog = newDialog;
        }
    }
}

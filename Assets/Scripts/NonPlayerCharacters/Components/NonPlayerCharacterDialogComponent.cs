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

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            _dialogIndex = runtimeState.GetDialogIndex();
            _indicator.SetActive(_dialogIndex >= 0);

            _dialogDefinition = runtimeState.GetDialogDefinition();
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState)
        {
            UpdateDialogChange(runtimeState);
        }

        private void UpdateDialogChange(NonPlayerCharacterRuntimeState runtimeState)
        {
            int oldDialogIndex = _dialogIndex;
            int newDialogIndex = runtimeState.GetDialogIndex();

            if (oldDialogIndex == newDialogIndex)
                return;

            _dialogIndex = newDialogIndex;
            _indicator.SetActive(_dialogIndex >= 0);

            DialogDefinition oldDialog = _dialogDefinition;
            DialogDefinition newDialog = runtimeState.GetDialogDefinition();

            if (oldDialog == newDialog)
                return;

            _dialogDefinition = newDialog;
        }
    }
}

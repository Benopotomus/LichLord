using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using LichLord.Dialog;

namespace LichLord.UI
{
    public class UIDialogResponseButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private Button _button;
        private DialogResponse _dialogResponse;
        public DialogResponse DialogResponse => _dialogResponse;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void SetDialogResponse(DialogResponse dialogResponse)
        {
            _dialogResponse = dialogResponse;
            _buttonText.text = dialogResponse.Text;
        }

        public void AddClickListener(UnityAction action)
        {
            _button.onClick.AddListener(action);
        }
    }
}
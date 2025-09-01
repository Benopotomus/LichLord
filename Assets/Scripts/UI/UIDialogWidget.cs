using LichLord.Dialog;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIDialogWidget : UIWidget
    {
        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _statementText;
        [SerializeField] private VerticalLayoutGroup _responseLayoutGroup;

        [SerializeField] private UIDialogResponseButton _responseButtonPrefab;

        public void SetDialogNode(DialogNode dialogNode)
        {
            _characterNameText.text = dialogNode.name;
            _statementText.text = dialogNode.Statement.Text;

            // Clear existing response buttons
            foreach (Transform child in _responseLayoutGroup.transform)
            {
                Destroy(child.gameObject);
            }

            // Instantiate new response buttons and bind their selection
            foreach (var response in dialogNode.Responses)
            {
                var button = Instantiate(_responseButtonPrefab, _responseLayoutGroup.transform);
                button.SetDialogResponse(response.Key);
                button.AddClickListener(() => OnResponseSelected(response.Value));
            }
        }

        private void OnResponseSelected(DialogNode nextNode)
        {
            Context.DialogManager.SetActiveDialogNode(nextNode); // Transition to the next dialog node
        }
    }
}
using LichLord.Dialog;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIDialogWidget : UIWidget
    {
        [SerializeField] private RectTransform _backgroundRect;
        [SerializeField] private float _largeSizeY = 300;
        [SerializeField] private float _smallSizeY = 200;

        [SerializeField] private TextMeshProUGUI _characterNameText;
        [SerializeField] private TextMeshProUGUI _statementText;
        [SerializeField] private VerticalLayoutGroup _responseLayoutGroup;

        [SerializeField] private UIDialogResponseButton _responseButtonPrefab;

        private DialogNode _currentNode;

        public void SetDialogNode(DialogNode dialogNode)
        {
            _currentNode = dialogNode;
            _characterNameText.text = dialogNode.name;
            _statementText.text = dialogNode.Statement.Text;

            // Clear existing response buttons
            foreach (Transform child in _responseLayoutGroup.transform)
            {
                Destroy(child.gameObject);
            }

            if (_currentNode.RequiresResponse)
            {
                _backgroundRect.sizeDelta = new Vector2(_backgroundRect.sizeDelta.x, _largeSizeY);
                // Instantiate new response buttons and bind their selection
                foreach (var response in dialogNode.Responses)
                {
                    var button = Instantiate(_responseButtonPrefab, _responseLayoutGroup.transform);
                    var responseKey = response.Key;
                    var nextNode = response.Value;

                    button.SetDialogResponse(responseKey);

                    // Pass both responseKey and nextNode
                    button.AddClickListener(() => OnResponseSelected(responseKey, nextNode));
                }
            }
            else
            {
                _backgroundRect.sizeDelta = new Vector2(_backgroundRect.sizeDelta.x, _smallSizeY);
            }
        } 

        private void OnResponseSelected(DialogResponse response, DialogNode nextNode)
        {
            _currentNode.InvokeResponse(response, Context);
        }
    }
}
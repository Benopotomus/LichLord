using TMPro;
using UnityEngine;

namespace LichLord.UI
{
    public class UIWarningText : UIWidget
    {
        [SerializeField]
        private TextMeshProUGUI _warningText;

        [SerializeField]
        private TextMeshProUGUI _subText;

        private int _timeoutTick;
        public int TimeoutTick => _timeoutTick;

        public void ShowWarningText(string warningText, string subtext)
        {
            _timeoutTick = Context.Runner.Tick + 96;
            _warningText.text = warningText;
            _subText.text = subtext;

        }
    }
}

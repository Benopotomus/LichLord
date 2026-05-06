using System.Text;
using TMPro;
using UnityEngine;

namespace LichLord.CardGame.UI
{
    /// <summary>
    /// Displays the current dice values for one combatant's pool.
    /// Uses a single TMP label: "[3] [5] [1] [4] [6]"
    /// </summary>
    public class DiceRowView : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI diceLabel;

        private static readonly StringBuilder _sb = new StringBuilder();

        public void Refresh(int[] roll)
        {
            _sb.Clear();
            if (roll == null || roll.Length == 0)
            {
                diceLabel.text = "—";
                return;
            }

            for (int i = 0; i < roll.Length; i++)
            {
                if (i > 0) _sb.Append("  ");
                _sb.Append('[');
                _sb.Append(roll[i]);
                _sb.Append(']');
            }

            diceLabel.text = _sb.ToString();
        }
    }
}

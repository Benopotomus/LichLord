using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.CardGame.UI
{
    /// <summary>
    /// Simple scrollable text log for combat events.
    /// Attach to a ScrollRect GameObject; assign the content TMP text.
    /// </summary>
    public class CombatLogView : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI logText;
        [SerializeField] public ScrollRect scrollRect;

        private const int MaxLines = 100;
        private readonly StringBuilder _buffer = new StringBuilder();
        private int _lineCount;

        public void AddLine(string message)
        {
            if (_lineCount >= MaxLines)
            {
                // Drop oldest line — search directly in the builder to avoid allocation
                int firstNewline = -1;
                for (int i = 0; i < _buffer.Length; i++)
                {
                    if (_buffer[i] == '\n') { firstNewline = i; break; }
                }
                if (firstNewline >= 0)
                    _buffer.Remove(0, firstNewline + 1);
                else
                    _buffer.Clear();
                _lineCount--;
            }

            if (_buffer.Length > 0) _buffer.Append('\n');
            _buffer.Append(message);
            _lineCount++;

            logText.text = _buffer.ToString();

            // Scroll to bottom next frame
            if (scrollRect != null)
                Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.normalizedPosition = new Vector2(0f, 0f);
        }

        public void Clear()
        {
            _buffer.Clear();
            _lineCount = 0;
            if (logText != null) logText.text = "";
        }
    }
}

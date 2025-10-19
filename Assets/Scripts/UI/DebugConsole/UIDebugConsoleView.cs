using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIDebugConsoleView : UIGameplayView
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI _consoleText;
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Console Settings")]
        [SerializeField] private int _maxLines = 100;
        [SerializeField] private float _fadeTime = 5f;

        private Queue<ConsoleLine> _lines = new Queue<ConsoleLine>();
        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;

        private void Awake()
        {
            SetupInputField();
            SetupScrollRect();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _inputField.gameObject.SetActive(false);
            ClearConsole();
        }

        protected override void OnOpen()
        {
            ShowConsole();
        }

        protected override void OnClose()
        {
            HideConsole();
        }

        public void ShowConsole()
        {
            gameObject.SetActive(true);
            _inputField.gameObject.SetActive(true);
            _inputField.Select();
            _inputField.ActivateInputField();
            _scrollRect.verticalNormalizedPosition = 0f; // Scroll to bottom
        }

        public void HideConsole()
        {
            _inputField.gameObject.SetActive(false);
            _inputField.DeactivateInputField();
        }

        public void OnSubmit(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            // Add to history
            AddToHistory(command);
            _historyIndex = _commandHistory.Count;

            // Execute command
            Context.DebugConsole.ExecuteCommand(command);

            // Clear input
            _inputField.text = "";
            _inputField.ActivateInputField();
        }

        public void OnInputChanged(string input)
        {
            // Auto-complete or other input handling
        }

        public void OnUpArrow()
        {
            if (_commandHistory.Count == 0) return;
            _historyIndex = Mathf.Clamp(_historyIndex - 1, 0, _commandHistory.Count - 1);
            _inputField.text = _commandHistory[_historyIndex];
            _inputField.caretPosition = _inputField.text.Length;
        }

        public void OnDownArrow()
        {
            if (_commandHistory.Count == 0) return;
            _historyIndex = Mathf.Clamp(_historyIndex + 1, -1, _commandHistory.Count - 1);
            _inputField.text = _historyIndex == -1 ? "" : _commandHistory[_historyIndex];
            _inputField.caretPosition = _inputField.text.Length;
        }

        public void AddLog(EConsoleColor color, string message)
        {
            var line = new ConsoleLine
            {
                text = $"[{System.DateTime.Now:HH:mm:ss}] {message}",
                color = GetColor(color)
            };

            _lines.Enqueue(line);
            if (_lines.Count > _maxLines)
                _lines.Dequeue();

            UpdateConsoleText();
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void SetupInputField()
        {
            _inputField.onEndEdit.AddListener(OnSubmit);
            _inputField.onValueChanged.AddListener(OnInputChanged);

            // Custom navigation for history
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            // Note: You may need to handle arrow keys via Input System or custom InputField
        }

        private void SetupScrollRect()
        {
            var content = _scrollRect.content.GetComponent<RectTransform>();

            // ✅ FIX 1: Use UpperLeft alignment (NOT LowerLeft)
            var layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.UpperLeft; // ← CHANGED FROM LowerLeft
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = false;
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.padding = new RectOffset(5, 5, 5, 5); // Small padding
            }

            // ✅ FIX 2: ContentSizeFitter for dynamic height
            var contentFitter = content.GetComponent<ContentSizeFitter>();
            if (contentFitter == null)
            {
                contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Dynamic height
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // ✅ FIX 3: Anchor content to stretch full width
            content.anchorMin = new Vector2(0, 1); // Top-left
            content.anchorMax = new Vector2(1, 1); // Top-right
            content.pivot = new Vector2(0, 1);     // Pivot top-left
            content.anchoredPosition = Vector2.zero;
        }

        private void UpdateConsoleText()
        {
            _consoleText.text = "";
            foreach (var line in _lines)
            {
                _consoleText.text += $"<color=#{ColorUtility.ToHtmlStringRGBA(line.color)}>{line.text}</color>\n";
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_consoleText.rectTransform);

            // ✅ ENSURE SCROLL TO BOTTOM AFTER LAYOUT
            _scrollRect.verticalNormalizedPosition = 0f;
        }

        private void AddToHistory(string command)
        {
            if (_commandHistory.Count > 0 && _commandHistory[^1] == command)
                return;

            _commandHistory.Add(command);
            if (_commandHistory.Count > 50) // Max history
                _commandHistory.RemoveAt(0);
        }

        private Color GetColor(EConsoleColor color)
        {
            return color switch
            {
                EConsoleColor.Default => Color.white,
                EConsoleColor.Info => Color.cyan,
                EConsoleColor.Warning => Color.yellow,
                EConsoleColor.Error => Color.red,
                EConsoleColor.Success => Color.green,
                _ => Color.white
            };
        }

        private void Update()
        {
            if (!_inputField.isFocused)
                return;

            // Navigate command history
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                OnUpArrow();
                _inputField.caretPosition = _inputField.text.Length;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                OnDownArrow();
                _inputField.caretPosition = _inputField.text.Length;
            }
        }

        private void ClearConsole()
        {
            _lines.Clear();
            UpdateConsoleText();
        }

        // Inner class for console lines
        private class ConsoleLine
        {
            public string text;
            public Color color;
        }
    }

    public enum EConsoleColor
    {
        Default,
        Info,
        Warning,
        Error,
        Success
    }
}
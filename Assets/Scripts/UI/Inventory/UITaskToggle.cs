
using DWD.Utility.Loading;
using LichLord.NonPlayerCharacters;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UITaskToggle : UIWidget
    {
        [SerializeField]
        protected Image _iconImage;
        public Image IconImage => _iconImage;

        [SerializeField]
        protected Image _activeImage;

        [SerializeField]
        protected UIButton _uiButton;

        private IconLoader _iconLoader = new IconLoader();

        private CommandTaskDefinition _commandTask;

        private WorkerComponent _workerComponent;
        private int _workerIndex;
        private int _taskIndex;

        public void Awake()
        {
            _uiButton.onClick.AddListener(OnButtonPressed);
        }

        public void SetCommandTask(WorkerComponent workerComponent,
            int workerIndex,
            int taskIndex,
            CommandTaskDefinition commandTask, 
            bool isActive)
        {
            _workerComponent = workerComponent;
            _workerIndex = workerIndex;
            _taskIndex = taskIndex;

            UpdateIcon(commandTask);
            _activeImage.enabled = isActive;

        }

        private void OnButtonPressed()
        {
            if(_workerComponent == null) 
                return;

            _workerComponent.RPC_ToggleTask((byte)_workerIndex, (byte)_taskIndex);
        }

        public void UpdateToggle()
        { 
            
        }

        private void UpdateIcon(CommandTaskDefinition commandTask)
        {
            if (commandTask == _commandTask)
                return;

            _commandTask = commandTask;

            if (_commandTask == null)
                return;

            Debug.Log("load Icon");

            LoadIcon(_commandTask.Icon);
        }

        protected void LoadIcon(BundleObject prefabBundle)
        {
            _iconLoader.OnLoaded += OnIconLoaded;
            _iconLoader.LoadIcon(prefabBundle);
        }

        protected void OnIconLoaded(IconLoader iconLoader, Sprite sprite)
        {
            _iconLoader.OnLoaded -= OnIconLoaded;
            _iconImage.sprite = sprite;
            _iconImage.enabled = true;
        }
    }
}

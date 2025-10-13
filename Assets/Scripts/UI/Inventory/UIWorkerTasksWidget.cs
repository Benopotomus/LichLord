using DWD.Utility.Loading;
using LichLord.NonPlayerCharacters;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UIWorkerTasksWidget : UIWidget
    {
        [SerializeField]
        private Image _workerIcon;

        [SerializeField]
        private UITaskToggle[] _taskToggles;

        [SerializeField]
        private WorkerComponent _workerComponent;

        [SerializeField]
        private int _workerIndex;

        [SerializeField]
        private NonPlayerCharacter _worker;

        private IconLoader _iconLoader = new IconLoader();

        public void SetWorkerData(WorkerComponent workerComponent, int workerIndex)
        {
            _workerComponent = workerComponent;
            _workerIndex = workerIndex;
        }

        protected override void OnTick()
        {
            UpdateWorkerIcon();
            UpdateTasks();
        }

        private void UpdateWorkerIcon()
        {
            if (_workerComponent == null)
                return;

            var newWorker = _workerComponent.WorkerCharacters[_workerIndex];

            if (newWorker == _worker)
                return;

            _worker = newWorker;

            if (_worker == null)
                return;

            LoadIcon(_worker.RuntimeState.Definition.Icon);
        }

        private void UpdateTasks()
        {
            if (_workerComponent == null)
                return;

            var workerData = _workerComponent.GetWorkerData(_workerIndex);

            var workerDefinition = _worker.RuntimeState.Definition;

            for (int i = 0; i < 8; i++)
            {
                if (i >= workerDefinition.Tasks.Length)
                {
                    _taskToggles[i].SetActive(false);
                    continue;
                }

                var task = workerDefinition.Tasks[i];
                switch (task)
                {
                    case ECommandTask.Wood:

                        break;

                }
            }


        }

        protected void LoadIcon(BundleObject prefabBundle)
        {
            _iconLoader.OnLoaded += OnIconLoaded;
            _iconLoader.LoadIcon(prefabBundle);
        }

        protected void OnIconLoaded(IconLoader iconLoader, Sprite sprite)
        {
            _iconLoader.OnLoaded -= OnIconLoaded;
            _workerIcon.sprite = sprite;
            _workerIcon.enabled = true;
        }
    }
}

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
            UpdateWorkerIcon();
            UpdateTasks();
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

            if (_worker == null)
                return;

            var workerData = _workerComponent.GetWorkerData(_workerIndex);
            var workerDefinition = _worker.RuntimeState.Definition;
            var tasks = _worker.RuntimeState.GetCommandTasks();

            //Debug.Log(tasks.Length);

            for (int i = 0; i < 8; i++)
            {
                if (_taskToggles[i] == null)
                    continue;

                if (i >= tasks.Length)

                {
                    _taskToggles[i].SetActive(false);
                    continue;
                }
                
                var task = tasks[i];
                _taskToggles[i].SetCommandTask(_workerComponent, 
                    _workerIndex,
                    i,
                    task, 
                    workerData.TasksData.IsTaskActive(i));

                _taskToggles[i].SetActive(true);
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

using LichLord.Buildables;
using LichLord.NonPlayerCharacters;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LichLord.UI
{
    public class UITaskMasterWidget : UIInventoryContextWidget
    {
        [SerializeField]
        private TextMeshProUGUI _nameText;

        [SerializeField]
        private TaskMaster _taskMaster;

        [SerializeField]
        private WorkerComponent _workerComponent;

        [SerializeField]
        private RectTransform _contentTransform;

        [SerializeField]
        private List<UIWorkerTasksWidget> _workerTaskWidgets;

        [SerializeField]
        private UIWorkerTasksWidget _workerTaskWidgetPrefab;

        protected override void OnVisible()
        {
            base.OnVisible();

            PlayerCharacter pc = Context.LocalPlayerCharacter;
            if (pc == null)
                return;

            var interactor = pc.Interactor;
            if (interactor == null)
                return;

            var interactable = interactor.CurrentInteractable;
            if (interactable == null)
                return;

            if (interactable.Owner is not TaskMaster taskMaster)
                return;

            _taskMaster = taskMaster;

            BuildableRuntimeState runtimeState = taskMaster.RuntimeState;

            if (runtimeState.Definition is not TaskMasterDefinition definition)
                return;

            _nameText.text = definition.BuildableName;

            Lair stronghold = _taskMaster.Lair;
            _workerComponent = stronghold.WorkerComponent;
            _workerComponent.OnWorkersChanged += OnWorkersChanged;
            OnWorkersChanged(_workerComponent.WorkerCharacters);
        }

        protected override void OnHidden()
        {
            if(_workerComponent != null)
                _workerComponent.OnWorkersChanged -= OnWorkersChanged;

            base.OnHidden();
        }

        private void OnWorkersChanged(NonPlayerCharacter[] newWorkers)
        {
            if (_workerComponent == null)
                return;

            for (int i = 0; i < newWorkers.Length; i++)
            {
                var worker = newWorkers[i];

                if (i >= _workerTaskWidgets.Count)
                    continue;

                if (worker != null)
                {
                    UIWorkerTasksWidget widget = _workerTaskWidgets[i];

                    widget.SetWorkerData(_workerComponent, i);
                    widget.gameObject.SetActive(true);
                }
                else
                {
                    _workerTaskWidgets[i].gameObject.SetActive(false);
                }
            }
        }

    }
}
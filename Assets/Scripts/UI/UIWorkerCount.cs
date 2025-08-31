using TMPro;
using UnityEngine;

namespace LichLord.UI
{
    public class UIWorkerCount : UIWidget
    {
        [SerializeField]
        protected TextMeshProUGUI _text;

        protected override void OnTick()
        {
            base.OnTick();

            var workerCount = Context.WorkerManager.ActiveWorkerCount;
            var maxWorkers = Context.WorkerManager.MaxWorkerCount;
            _text.text = workerCount + " / " + maxWorkers;
        }
    }
}

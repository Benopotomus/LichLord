using LichLord.Buildables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.UI
{
    public class UICryptTooltip : UIWidget
    {
        [SerializeField] private Crypt _crypt;

        [SerializeField] private TextMeshProUGUI _stateText;

        [SerializeField] private UIFloatingHealthbar _healthbar;

        [SerializeField] private Image _skullIcon;
        [SerializeField] private Image _deadIcon;
        [SerializeField] private Image _circleFill;

        [SerializeField] private EWorkerState _workerState;

        public void SetCryptData(Crypt crypt)
        {
            _crypt = crypt;

            _healthbar.SetHealth(crypt.RuntimeState.GetHealth(), crypt.RuntimeState.GetMaxHealth());

            var newWorkerState = crypt.RuntimeState.GetWorkerState();

            UpdateWorkerStateChange(newWorkerState);

            _stateText.text = newWorkerState.ToString();

            int tick = Context.Runner.Tick;

            UpdateWorkerState(tick);
        }

        private void UpdateWorkerStateChange(EWorkerState newWorkerState)
        {
            if (newWorkerState == _workerState)
                return;

            _workerState = newWorkerState;

            switch (newWorkerState)
            {
                case EWorkerState.Spawning:
                    _skullIcon.SetActive(false);
                    _deadIcon.SetActive(false);
                    _circleFill.SetActive(true);
                    break;

                case EWorkerState.Cooldown:
                    _skullIcon.SetActive(true);
                    _deadIcon.SetActive(true);
                    _circleFill.SetActive(true);
                    break;

                case EWorkerState.WorkerActive:
                    _skullIcon.SetActive(true);
                    _deadIcon.SetActive(false);
                    _circleFill.SetActive(false);
                    break;
            }
        }

        private void UpdateWorkerState(int tick)
        {
            switch (_workerState)
            {
                case EWorkerState.Spawning:
                    int ticksRemaining = _crypt.SpawnEndTick - tick; // Ticks until spawning completes
                    int ticksToSpawnMax = _crypt.RuntimeState.GetWorkerSpawnTicks();
                    _circleFill.fillAmount = 1f - (float)ticksRemaining / (float)ticksToSpawnMax; // Normalize to 0-1
                    break;

                case EWorkerState.Cooldown:
                    _skullIcon.SetActive(true);
                    _deadIcon.SetActive(true);
                    _circleFill.SetActive(true);
                    break;

                case EWorkerState.WorkerActive:
                    _skullIcon.SetActive(true);
                    _deadIcon.SetActive(false);
                    _circleFill.SetActive(false);
                    break;
            }
        }
    }
}

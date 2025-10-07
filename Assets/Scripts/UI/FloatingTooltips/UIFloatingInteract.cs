using TMPro;
using UnityEngine;
using UnityEngine.UI; // Needed for Image

namespace LichLord.UI
{
    public class UIFloatingInteract : UIFloatingWidget
    {
        [Header("UI Elements")]
        [SerializeField] private Slider _progressSlider;  // Drag your progress bar fill image here

        [SerializeField] private TextMeshProUGUI _warningText;

        protected override void OnVisible()
        {
            base.OnVisible();
            _warningText.SetActive(false);
        }

        protected override void OnTick()
        {
            base.OnTick();
            float localRenderTime = Context.Runner.LocalRenderTime;

            PlayerCharacter pc = Context.LocalPlayerCharacter;

            if (pc == null)
            {
                SetTarget(null);
                return;
            }

            InteractableComponent bestInteractable = pc.Interactor.BestInteractable;

            if (bestInteractable == null)
            {
                SetTarget(null);
                return;
            }

            if (bestInteractable.IsInteractionValid(pc.Interactor))
            {
                SetTarget(bestInteractable.transform);
            }

            InteractableComponent currentInteractable = pc.Interactor.CurrentInteractable;

            if (currentInteractable == null)
            {
                SetProgressBarVisible(false);
                return;
            }

            SetProgressBarPercent(currentInteractable.GetPercentRemaining(localRenderTime));
            SetProgressBarVisible(true);
        }

        public override void SetTarget(Transform target)
        {
            if (target != _target)
                _warningText.SetActive(false);

            base.SetTarget(target);
        }

        public void SetProgressBarVisible(bool visible)
        {
            _progressSlider.SetActive(visible);
        }

        public void SetProgressBarPercent(float percent)
        {
            _progressSlider.value = Mathf.Clamp01(1 - percent);
        }

        public void ShowWarningMessage(string warningMessage)
        {
            _warningText.text = warningMessage;
            _warningText.SetActive(true);
            Invoke(nameof(HideWarningMessage), 3.0f);
        }

        private void HideWarningMessage()
        {
            _warningText.SetActive(false);
        }

    }
}
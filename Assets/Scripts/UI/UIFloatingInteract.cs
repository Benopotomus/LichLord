using LichLord;
using LichLord.UI;
using UnityEngine;
using UnityEngine.UI; // Needed for Image

public class UIFloatingInteract : UIFloatingWidget
{
    [Header("UI Elements")]
    [SerializeField] private Slider _progressSlider;  // Drag your progress bar fill image here

    public void SetProgressBarVisible(bool visible)
    {
        _progressSlider.SetActive(visible);
    }

    public void SetProgressBarPercent(float percent)
    {
        _progressSlider.value = Mathf.Clamp01(1 - percent);
    }
}

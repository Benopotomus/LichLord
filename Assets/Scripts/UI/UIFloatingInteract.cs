using LichLord;
using LichLord.UI;
using UnityEngine;
using UnityEngine.UI; // Needed for Image

public class UIFloatingInteract : UIWidget
{
    public Transform Target;
    public Vector3 Offset = new Vector3(0, 1f, 0);
    public Camera UICamera;

    private RectTransform _rectTransform;

    [Header("UI Elements")]
    [SerializeField] private Slider _progressSlider;  // Drag your progress bar fill image here

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    protected override void OnTick()
    {
        base.OnTick();
        UpdateScreenSpacePosition();
    }

    private void UpdateScreenSpacePosition()
    {
        if (Target != null)
        {
            Vector3 worldPos = Target.position + Offset;
            Vector3 screenPos = (UICamera != null ? UICamera : Camera.main).WorldToScreenPoint(worldPos);
            _rectTransform.position = screenPos;
        }
        else
        {
            _rectTransform.position = new Vector2(-200, -200);
        }
    }

    public void SetProgressBarVisible(bool visible)
    {
        _progressSlider.SetActive(visible);
    }

    public void SetProgressBarPercent(float percent)
    {
        _progressSlider.value = Mathf.Clamp01(1 - percent);
    }

    public void SetTarget(Transform target)
    {
        Target = target;
    }
}

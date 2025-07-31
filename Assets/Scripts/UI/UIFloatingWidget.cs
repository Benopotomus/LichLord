using LichLord;
using LichLord.UI;
using UnityEngine;
using UnityEngine.UI; // Needed for Image

public class UIFloatingWidget : UIWidget
{
    public Transform Target;
    public Vector3 Offset = new Vector3(0, 1f, 0);
    public Camera UICamera;

    protected RectTransform _rectTransform;

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

    public void SetTarget(Transform target)
    {
        Target = target;
    }
}

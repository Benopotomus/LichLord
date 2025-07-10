using LichLord.UI;
using UnityEngine;

public class UIFloatingInteract : UIWidget
{
    public Transform Target;  // The world object to follow
    public Vector3 Offset = new Vector3(0, 1f, 0); // Offset above the object
    public Camera UICamera; // Optional: only if using Screen Space - Camera

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    protected override void OnTick()
    {
        base.OnTick();

        if (Target != null)
        {
            Vector3 worldPos = Target.position + Offset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos); // Use UICamera if set
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

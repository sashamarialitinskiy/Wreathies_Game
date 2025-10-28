using UnityEngine;

public class UIToCanvasAnchor : MonoBehaviour
{
    public RectTransform uiTarget;   // e.g., the Request panel/label inside ClientBox (Full group)
    public Canvas canvas;            // your UI Canvas
    public Vector2 pixelOffset;      // small nudge, if needed

    RectTransform self;

    void Awake()
    {
        self = (RectTransform)transform;
        if (!canvas) canvas = GetComponentInParent<Canvas>();
    }

    void LateUpdate()
    {
        if (!uiTarget || !canvas) return;

        // For Overlay canvases, canvasCam must be null; otherwise use canvas.worldCamera
        Camera canvasCam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            ? null
            : canvas.worldCamera;

        // Take the center of the target rect (works even if nested under other UI)
        Vector3 worldCenter = uiTarget.TransformPoint(uiTarget.rect.center);

        // World -> Screen
        Vector2 sp = RectTransformUtility.WorldToScreenPoint(canvasCam, worldCenter);

        // Screen -> Canvas local
        RectTransform canvasRect = (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, canvasCam, out var lp);

        self.anchoredPosition = lp + pixelOffset;
    }
}

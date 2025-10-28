using UnityEngine;

public class WorldToCanvasAnchor : MonoBehaviour
{
    public Transform worldTarget;   // e.g., Arrow_Flowers (in world)
    public Canvas canvas;           // your UI Canvas
    public Camera worldCamera;      // Main Camera
    public Vector2 pixelOffset;

    RectTransform rect;

    void Awake()
    {
        rect = (RectTransform)transform;
        if (!worldCamera) worldCamera = Camera.main;
        if (!canvas) canvas = GetComponentInParent<Canvas>();
    }

    void LateUpdate()
    {
        if (!worldTarget || !canvas) return;

        // Overlay: canvasCam must be null; other modes use canvas.worldCamera
        Camera canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (canvas.worldCamera ? canvas.worldCamera : worldCamera);

        // World -> Screen
        Vector2 sp = RectTransformUtility.WorldToScreenPoint(worldCamera, worldTarget.position);

        // Screen -> Canvas local
        RectTransform canvasRect = (RectTransform)canvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, canvasCam, out var lp);

        rect.anchoredPosition = lp + pixelOffset;
    }
}

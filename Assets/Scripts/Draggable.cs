using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Draggable : MonoBehaviour
{
    private Camera mainCamera;
    private Vector3 offset;
    private bool isDragging;

    [Header("State")]
    public bool isInWreath = false;

    [Header("Visuals")]
    [SerializeField] private int dragSortingBoost = 50;   // bring to front while dragging
    private SpriteRenderer sr;
    private int originalOrder;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (!mainCamera)
            Debug.LogWarning("[Draggable] No Camera.main found.");
    }

    private void Update()
    {
        // ----- Mouse -----
        if (Input.GetMouseButtonDown(0))
        {
            TryStartDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            DragTo(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }

        // ----- Touch -----
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            switch (t.phase)
            {
                case TouchPhase.Began:
                    TryStartDrag(t.position);
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging) DragTo(t.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging) EndDrag();
                    break;
            }
        }
    }

    private void TryStartDrag(Vector3 screenPosition)
    {
        if (!mainCamera) return;

        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 point = (Vector2)world;

        // Check every collider under the pointer (important when overlapping the wreath/other flowers)
        Collider2D[] hits = Physics2D.OverlapPointAll(point);
        foreach (var h in hits)
        {
            if (h && h.gameObject == gameObject)
            {
                // If we’re picking it up from the wreath, free it first.
                if (isInWreath && WreathZone.Instance != null)
                {
                    isInWreath = false;
                    WreathZone.Instance.RemoveFlower(gameObject);
                }

                isDragging = true;
                offset = transform.position - world;

                // bring to front
                if (sr)
                {
                    originalOrder = sr.sortingOrder;
                    sr.sortingOrder = originalOrder + dragSortingBoost;
                }
                return;
            }
        }
    }

    private void DragTo(Vector3 screenPosition)
    {
        if (!mainCamera) return;

        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector3 target = world + offset;
        target.z = transform.position.z; // keep original Z in 2D
        transform.position = target;
    }

    private void EndDrag()
    {
        isDragging = false;

        // restore draw order
        if (sr) sr.sortingOrder = originalOrder;

        // Check if dropped inside wreath
        if (WreathZone.Instance != null)
        {
            bool inside = WreathZone.Instance.IsInsideWreath(transform.position);
            if (inside)
            {
                isInWreath = true;
                WreathZone.Instance.AddFlower(gameObject);

                // Notify tutorial (fires once per round via GameManager guard)
                GameManager.Instance?.NotifyFirstFlowerPlaced();
            }
            else
            {
                isInWreath = false;
            }
        }
    }
}

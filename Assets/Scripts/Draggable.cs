using UnityEngine;

// This script lets players pick up, drag, and drop flowers using mouse or touch.
// It detects when a flower is picked up, moves it with the pointer, and drops it
// — possibly onto the wreath area. It also handles sound effects and sorting order
// so dragged flowers appear on top visually.

[RequireComponent(typeof(Collider2D))] // Ensures the flower has a 2D collider so it can be clicked or touched
public class Draggable : MonoBehaviour
{
    private Camera mainCamera;   // Reference to the camera to convert screen position to world position
    private Vector3 offset;      // Offset between the flower and the pointer (so it doesn’t jump when picked up)
    private bool isDragging;     // Is this flower currently being dragged?

    [Header("State")]
    public bool isInWreath = false; // Tracks if the flower is currently placed in the wreath

    [Header("Visuals")]
    [SerializeField] private int dragSortingBoost = 50; // Makes the flower appear “in front” while dragging
    private SpriteRenderer sr;      // Handles drawing the flower sprite
    private int originalOrder;      // Remembers the original draw order to restore later

    private void Awake()
    {
        // Find the flower’s SpriteRenderer so we can change its draw order during drag.
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        // Store a reference to the main camera (used to translate mouse/touch to world positions)
        mainCamera = Camera.main;
        if (!mainCamera)
            Debug.LogWarning("[Draggable] No Camera.main found."); // Warn if camera not found
    }

    private void Update()
    {
        // =====================================================
        // SECTION 1: MOUSE INPUT (for PC or Web builds)
        // =====================================================
        if (Input.GetMouseButtonDown(0))                // Mouse button pressed down
        {
            TryStartDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && isDragging) // Mouse held while dragging
        {
            DragTo(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isDragging) // Mouse released
        {
            EndDrag();
        }

        // =====================================================
        // SECTION 2: TOUCH INPUT (for tablets or phones)
        // =====================================================
        if (Input.touchCount > 0) // If there’s at least one finger on the screen
        {
            Touch t = Input.GetTouch(0);
            switch (t.phase)
            {
                case TouchPhase.Began:
                    TryStartDrag(t.position);  // Start dragging on touch start
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging) DragTo(t.position); // Keep moving flower under finger
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (isDragging) EndDrag();  // Let go of flower
                    break;
            }
        }
    }

    // =====================================================
    // Try to start dragging if player clicked/tapped this flower
    // =====================================================
    private void TryStartDrag(Vector3 screenPosition)
    {
        if (!mainCamera) return;

        // Convert pointer position (in pixels) to world space
        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 point = (Vector2)world;

        // Check if the pointer is over this object’s collider
        Collider2D[] hits = Physics2D.OverlapPointAll(point);
        foreach (var h in hits)
        {
            if (h && h.gameObject == gameObject)
            {
                // If the flower is currently on the wreath, remove it before dragging away
                if (isInWreath && WreathZone.Instance != null)
                {
                    isInWreath = false;
                    WreathZone.Instance.RemoveFlower(gameObject);
                }

                // Mark it as currently being dragged
                isDragging = true;
                offset = transform.position - world; // Keep consistent offset between pointer and flower

                // Bring flower visually to the front
                if (sr)
                {
                    originalOrder = sr.sortingOrder;
                    sr.sortingOrder = originalOrder + dragSortingBoost;
                }

                // 🔊 Play grab sound effect (when picking up a flower)
                AudioManager.Instance?.PlayGrab();

                return; // Stop checking others; we’ve found our target
            }
        }
    }

    // =====================================================
    // Move the flower with the pointer or finger
    // =====================================================
    private void DragTo(Vector3 screenPosition)
    {
        if (!mainCamera) return;

        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition); // Convert pointer to world space
        Vector3 target = world + offset; // Apply the offset so it follows naturally
        target.z = transform.position.z; // Keep the same Z depth (important in 2D)
        transform.position = target;     // Move the flower to that position
    }

    // =====================================================
    // Stop dragging (drop the flower)
    // =====================================================
    private void EndDrag()
    {
        isDragging = false; // Stop movement updates

        // Restore the original draw order (so flower goes back behind others)
        if (sr) sr.sortingOrder = originalOrder;

        // Check if flower was dropped inside the wreath area
        if (WreathZone.Instance != null)
        {
            bool inside = WreathZone.Instance.IsInsideWreath(transform.position);
            if (inside)
            {
                isInWreath = true;
                WreathZone.Instance.AddFlower(gameObject);

                // Let GameManager know the player placed their first flower (tutorial tracking)
                GameManager.Instance?.NotifyFirstFlowerPlaced();
            }
            else
            {
                isInWreath = false; // It was dropped outside
            }
        }

        // 🔊 Play pop sound when released
        AudioManager.Instance?.PlayPop();
    }
}

using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Draggable : MonoBehaviour
{
    private Vector3 offset;
    private bool isDragging = false;
    private Camera mainCamera;

    public bool isInWreath = false;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        Vector3 pointerPosition = Vector3.zero;

        // Desktop input
        if (Input.GetMouseButtonDown(0))
        {
            pointerPosition = Input.mousePosition;
            TryStartDrag(pointerPosition);
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            DragTo(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }

        // Touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointerPosition = touch.position;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryStartDrag(pointerPosition);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (isDragging)
                        DragTo(pointerPosition);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    EndDrag();
                    break;
            }
        }
    }

    private void TryStartDrag(Vector3 screenPosition)
    {
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        Collider2D hit = Physics2D.OverlapPoint(worldPos);

        if (hit != null && hit.gameObject == gameObject)
        {
            isDragging = true;
            offset = transform.position - (Vector3)worldPos;
        }
    }

    private void DragTo(Vector3 screenPosition)
    {
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPosition);
        transform.position = worldPos + (Vector2)offset;
    }

    private void EndDrag()
    {
        isDragging = false;

        // Check if inside the wreath zone
        if (WreathZone.Instance != null)
        {
            bool wasInWreath = isInWreath;
            isInWreath = WreathZone.Instance.IsInsideWreath(transform.position);

            if (isInWreath && !wasInWreath)
            {
                WreathZone.Instance.AddFlower(gameObject);
            }
            else if (!isInWreath && wasInWreath)
            {
                WreathZone.Instance.RemoveFlower(gameObject);
            }
        }
    }
}


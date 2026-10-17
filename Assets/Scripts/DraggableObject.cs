using UnityEngine;
using UnityEngine.EventSystems; // Required for Pointer Events

[RequireComponent(typeof(Collider2D))]
public class DraggableObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Camera _mainCamera;
    private Vector3 _dragOffset;

    private void Awake()
    {
        _mainCamera = Camera.main;

        // Ensure we have an EventSystem in the scene, otherwise dragging won't trigger
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Debug.Log("Created a missing EventSystem automatically!");
        }
    }

    // 1. Triggers the moment you click down and start moving the mouse
    public void OnBeginDrag(PointerEventData eventData)
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition(eventData.position);
        _dragOffset = transform.position - mouseWorldPos;
    }

    // 2. Triggers continuously while you move the mouse
    public void OnDrag(PointerEventData eventData)
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition(eventData.position);
        transform.position = mouseWorldPos + _dragOffset;
    }

    // 3. Triggers when you let go of the mouse button
    public void OnEndDrag(PointerEventData eventData)
    {
        // Put any drop logic here (like snapping to a grid slot)
        Debug.Log("Object dropped!");
    }

    private Vector3 GetMouseWorldPosition(Vector2 screenPosition)
    {
        // Convert screen pixel position to Unity 2D World units
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0));
        
        // Keep the sprite's original Z depth so it doesn't disappear
        worldPos.z = transform.position.z; 
        
        return worldPos;
    }
}
using UnityEngine;
using UnityEngine.EventSystems;

// Adding IDragHandler tells Unity to track dragging movements over this UI element
public class FixedTouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Vector2 touchDist;
    public bool pressed;

    private bool dragUpdatedThisFrame = false;

    private void Start()
    {
        pressed = false;
        touchDist = Vector2.zero;
    }

    private void Update()
    {
        // If the user stops moving their finger/mouse but still holds it down, 
        // we must reset touchDist to zero so the camera stops spinning.
        if (!dragUpdatedThisFrame)
        {
            touchDist = Vector2.zero;
        }

        dragUpdatedThisFrame = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
        touchDist = Vector2.zero;
    }

    // This callback is triggered automatically by Unity's UI system when dragging happens
    public void OnDrag(PointerEventData eventData)
    {
        if (pressed)
        {
            // eventData.delta is calculated perfectly by Unity for both Touch and Mouse
            touchDist = eventData.delta;
            dragUpdatedThisFrame = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        touchDist = Vector2.zero;
    }
}
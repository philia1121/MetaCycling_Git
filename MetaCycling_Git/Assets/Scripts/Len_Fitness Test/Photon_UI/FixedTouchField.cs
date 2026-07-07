using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
public class FixedTouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector]
    public Vector2 touchDist;
    [HideInInspector]
    public Vector2 pointerOld;
    [HideInInspector]
    protected int pointerId;
    [HideInInspector]
    public bool pressed;

    private void Start()
    {
        pressed = false;
    }

    void Update()
    {
        if (pressed)
        {
            // 1. Handle Touchscreen Inputs cleanly
            if (Touchscreen.current != null && pointerId >= 0 && pointerId < Touchscreen.current.touches.Count)
            {
                var targetTouch = Touchscreen.current.touches[pointerId];

                // Read the position from the new Input System's TouchControl structure
                Vector2 currentTouchPos = targetTouch.position.ReadValue();

                touchDist = currentTouchPos - pointerOld;
                pointerOld = currentTouchPos;
            }
            // 2. Fallback to generic mouse/screen Pointers (PC mouse drag simulation)
            else if (Pointer.current != null)
            {
                // Pointer.current handles both Mouse positions and Trackpad inputs uniformly
                Vector2 currentPointerPos = Pointer.current.position.ReadValue();

                touchDist = currentPointerPos - pointerOld;
                pointerOld = currentPointerPos;
            }
        }
        else
        {
            touchDist = Vector2.zero;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
        pointerId = eventData.pointerId; // Capture the pointer ID dynamically from the UI Raycast event!

        // Initialize the tracking start anchor position on the exact frame the click happens
        if (Pointer.current != null)
        {
            pointerOld = Pointer.current.position.ReadValue();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
    }
}

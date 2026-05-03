using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIFollowLogic : MonoBehaviour
{
    private Transform camPos;

    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float maxDist = 2f;
    [SerializeField] private Vector3 offSet = new Vector3(0, -0.75f, 0);

    [Header("Constraints")]
    [SerializeField] private float minYValue = -0.8f;
    [SerializeField] private bool lockYRot = true;

    private void OnEnable()
    {
        if (Camera.main != null) camPos = Camera.main.transform;
    }

    private void Update()
    {
        if (camPos == null) return;

        HandlePosition();
        HandleRotation();
    }

    private void HandlePosition()
    {
        // 1. Calculate the target position based on player's forward direction
        // We project the forward vector onto a horizontal plane to prevent the UI 
        // from moving up/down when the player looks at the sky/floor.
        Vector3 forward = camPos.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 targetPos = camPos.position + (forward * offSet.z) + (Vector3.up * offSet.y);

        // 2. Clamp the Y value so it never goes too low
        if (targetPos.y < minYValue)
        {
            targetPos.y = minYValue;
        }

        // 3. Distance Check (Teleport if too far, otherwise Smooth Follow)
        float dist = Vector3.Distance(transform.position, targetPos);
        if (dist > maxDist)
        {
            transform.position = targetPos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        }
    }

    private void HandleRotation()
    {
        // Make the UI face the player (Billboard)
        Vector3 lookDir = transform.position - camPos.position;

        if (lockYRot)
            lookDir.y = 0; // Keeps the bar level with the horizon

        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}

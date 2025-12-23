using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class DroneCamManager : MonoBehaviour
{
    public float speed = 5f;
    ControlMap controlMap;
    public Transform mainCam;
    void Awake()
    {
        controlMap = new ControlMap();
    }
    void OnEnable()
    {
        controlMap.Prototype.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 input = controlMap.Prototype.RJoystick.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.01f) return;

        Vector3 cameraForward = mainCam.forward;
        Vector3 cameraRight = mainCam.right;

        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * input.y) + (cameraRight * input.x);
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }
}

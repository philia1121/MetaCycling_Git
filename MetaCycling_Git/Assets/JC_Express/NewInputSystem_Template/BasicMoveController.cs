using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class BasicMoveController : MonoBehaviour
{
    [SerializeField]private float moveSpeed = 5;
    [SerializeField]private float rotateSpeed = 5;
    Vector3 moveDir;
    [SerializeField]private bool reverseX, reverseZ;


    [Header("")]
    public UnityEvent StartMovingEvent, StopMovingEvent;
    bool wasMoving;

    ControlMap controlMap;
    void Awake()
    {
        controlMap = new ControlMap();   
    }
    void OnEnable()
    {
        controlMap.PlayerInput.Enable();

        controlMap.PlayerInput.Move.started += MoveControl;
        controlMap.PlayerInput.Move.performed += MoveControl;
        controlMap.PlayerInput.Move.canceled += MoveControl;
    }
    void OnDisable()
    {
        controlMap.PlayerInput.Move.started -= MoveControl;
        controlMap.PlayerInput.Move.performed -= MoveControl;
        controlMap.PlayerInput.Move.canceled -= MoveControl;

        controlMap.PlayerInput.Disable();
    }
    public void Start()
    {
        wasMoving = false;
    }
    void Update()
    {
        transform.Translate(moveDir* moveSpeed* Time.deltaTime, Space.World);
        HandleRotation();
    }

    void MoveControl(InputAction.CallbackContext ctx)
    {
        Vector2 value = ctx.ReadValue<Vector2>();

        var isMoving = (value != Vector2.zero);
        if(wasMoving != isMoving)
        {
            if(isMoving)
                StartMovingEvent.Invoke();
            else
                StopMovingEvent.Invoke();
        }
        wasMoving = isMoving;

        var reX = reverseX? -1 : 1; 
        var reZ = reverseZ? -1 : 1;
        moveDir = new Vector3(value.x* reX, 0, value.y* reZ).normalized;
    }

    void HandleRotation()
    {
        if(moveDir.magnitude <= 0)  return;
        
        var facing = moveDir;
        facing.y = 0;

        Quaternion current = this.transform.rotation;
        Quaternion target = Quaternion.LookRotation(facing);

        if(Vector3.Distance(current.eulerAngles, target.eulerAngles) > 1f )
        {
            var step = rotateSpeed* 100* Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(current, target, step);
        }
        else
        {
            transform.rotation = target;
        }
    }
}

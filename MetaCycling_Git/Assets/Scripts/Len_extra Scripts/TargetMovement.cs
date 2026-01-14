using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private float speed;
    private Rigidbody rb;
    private Vector3 move;
    private ControlMap controlMap;

    private void Awake()
    {
        controlMap = new ControlMap();
    }
    private void Start()
    {
        target = this.gameObject;

        rb = target.GetComponent<Rigidbody>();
    }
    private void OnEnable()
    {
        controlMap.PlayerInput.Enable();
    }

    private void Update()
    {
        Vector2 rawMove = controlMap.PlayerInput.Move.ReadValue<Vector2>();
        move = new Vector3(rawMove.y, 0, rawMove.x);
    }

    private void FixedUpdate()
    {
        rb.AddForce(move * speed, ForceMode.Impulse);
    }
}

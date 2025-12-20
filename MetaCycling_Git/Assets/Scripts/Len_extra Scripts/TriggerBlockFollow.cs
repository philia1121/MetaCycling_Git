using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerBlockFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject itemObj;    //item obj
    [SerializeField] private GameObject playerHead; //obj for player's head, block will follow this

    [Header("Follow Settings")]
    [SerializeField] private float itemDist;        //obj distance when player is moving obj around

    private ControlMap controlMap;
    private bool isHolding;
    void Awake()
    {
        controlMap = new ControlMap();
    }
    private void OnEnable()
    {
        controlMap.Prototype.Enable();
    }

    void Update()
    {
        if (controlMap.Prototype.Left_Grip.WasPressedThisFrame())
        {
            isHolding = true;
            MoveItemInFront();
        }
        if (controlMap.Prototype.Left_Grip.WasReleasedThisFrame())
        {
            isHolding = false;
        }
        if (isHolding)
        {
            MoveItemInFront();
        }
    }

    void MoveItemInFront()
    {
        itemObj.transform.position =
            playerHead.transform.position +
            playerHead.transform.forward * itemDist;
    }
}

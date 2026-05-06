using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallObjectsToPlayer : MonoBehaviour
{
    public Transform playerHead;
    public GameObject[] callObj;
    ControlMap controlMap;

    private void Awake()
    {
        controlMap = new ControlMap();
        controlMap.Prototype.Enable();

        controlMap.Prototype.ReCenter.started += ctx => CallObjects();
    }


    private void CallObjects()
    {
        foreach (GameObject obj in callObj)
        {
            obj.transform.position = playerHead.position;
        }
    }
}

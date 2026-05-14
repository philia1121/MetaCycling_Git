using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallObjectsToPlayer : MonoBehaviour
{
    public Transform playerHead;
    public GameObject[] callObj;
    public Vector3 offset;
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
            Vector3 spreadOffset = offset + (Vector3.left * 0.5f);

            obj.transform.position = playerHead.TransformPoint(spreadOffset);
            obj.transform.LookAt(playerHead);
        }
    }
}

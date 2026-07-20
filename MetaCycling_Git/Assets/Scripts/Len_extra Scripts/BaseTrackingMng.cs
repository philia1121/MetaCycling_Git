using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTrackingMng : MonoBehaviour
{
    public Transform HMD, L_Cont, R_Cont, L_Hand, R_Hand;
    Transform[] allTransform = new Transform[4];
    OVRInput.Controller rc = OVRInput.Controller.RTouch;
    OVRInput.Controller lc = OVRInput.Controller.LTouch;
    OVRInput.Controller rh = OVRInput.Controller.RHand;
    OVRInput.Controller lh = OVRInput.Controller.LHand;
    OVRInput.Controller[] allController = new OVRInput.Controller[4];

    private Vector3 userPos;
    void Awake()
    {
        allTransform[0] = L_Cont;
        allTransform[1] = R_Cont;
        allTransform[2] = L_Hand;
        allTransform[3] = R_Hand;

        allController[0] = lc;
        allController[1] = rc;
        allController[2] = lh;
        allController[3] = rh;
    }

    void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            allTransform[i].position = OVRInput.GetLocalControllerPosition(allController[i]);
            allTransform[i].rotation = OVRInput.GetLocalControllerRotation(allController[i]);
        }
    }
}

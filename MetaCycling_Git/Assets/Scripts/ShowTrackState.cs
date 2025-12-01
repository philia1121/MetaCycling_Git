using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowTrackState : MonoBehaviour
{
    public Material LHand, RHand;
    void Update()
    {
        bool R_IsTracked = (OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch) & OVRInput.GetControllerOrientationTracked(OVRInput.Controller.RTouch)) ? true : false;
        bool L_IsTracked = (OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch) & OVRInput.GetControllerOrientationTracked(OVRInput.Controller.LTouch)) ? true : false;

        RHand.color = R_IsTracked ? Color.white : Color.red;
        LHand.color = L_IsTracked ? Color.white : Color.red;
    }
}

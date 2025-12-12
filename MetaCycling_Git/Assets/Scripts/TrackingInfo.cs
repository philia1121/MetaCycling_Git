using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingInfo : MonoBehaviour
{
    public bool RHand_PosTracked;
    public bool RHand_RotTracked;
    public bool RController_PosTracked;
    public bool RController_RotTracked;
    public bool LHand_PosTracked;
    public bool LHand_RotTracked;
    public bool LController_PosTracked;
    public bool LController_RotTracked;
    OVRInput.Controller R_Controller = OVRInput.Controller.RTouch;
    OVRInput.Controller L_Controller = OVRInput.Controller.LTouch;
    OVRInput.Controller R_Hand = OVRInput.Controller.RHand;
    OVRInput.Controller L_Hand = OVRInput.Controller.LHand;

    void Update()
    {
        CheckForTracking();
    }
    void CheckForTracking()
    {
        // R Hand
        RHand_PosTracked = OVRInput.GetControllerPositionTracked(R_Hand);
        RHand_RotTracked = OVRInput.GetControllerOrientationTracked(R_Hand);
        // R Controller
        RController_PosTracked = OVRInput.GetControllerPositionTracked(R_Controller);
        RController_RotTracked = OVRInput.GetControllerOrientationTracked(R_Controller);
        // L Hand
        LHand_PosTracked = OVRInput.GetControllerPositionTracked(L_Hand);
        LHand_RotTracked = OVRInput.GetControllerOrientationTracked(L_Hand);
        // L Controller
        LController_PosTracked = OVRInput.GetControllerPositionTracked(L_Controller);
        LController_RotTracked = OVRInput.GetControllerOrientationTracked(L_Controller);
    }

    public bool Get_RHand_PosTracked() { return OVRInput.GetControllerPositionTracked(R_Hand); }
    public bool Get_RHand_RotTracked() { return OVRInput.GetControllerOrientationTracked(R_Hand); }
    public bool Get_RController_PosTracked() { return OVRInput.GetControllerPositionTracked(R_Controller); }
    public bool Get_RController_RotTracked() { return OVRInput.GetControllerOrientationTracked(R_Controller); }
    public bool Get_LHand_PosTracked() { return OVRInput.GetControllerPositionTracked(L_Hand); }
    public bool Get_LHand_RotTracked() { return OVRInput.GetControllerOrientationTracked(L_Hand); }
    public bool Get_LController_PosTracked() { return OVRInput.GetControllerPositionTracked(L_Controller); }
    public bool Get_LController_RotTracked() { return OVRInput.GetControllerOrientationTracked(L_Controller); }

}

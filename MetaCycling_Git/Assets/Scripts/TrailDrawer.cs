using System.Collections;
using System.Collections.Generic;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using UnityEngine;

public class TrailDrawer : MonoBehaviour
{
    public Transform[] drawers;
    public TrackingStatusIndicator[] indicators;
    OVRInput.Controller R_Controller = OVRInput.Controller.RTouch;
    OVRInput.Controller L_Contorller = OVRInput.Controller.LTouch;
    OVRInput.Controller R_Hand = OVRInput.Controller.RHand;
    OVRInput.Controller L_Hand = OVRInput.Controller.LHand;
    OVRInput.Controller[] allController = new OVRInput.Controller[4];
    Vector3[] allLastPos = new Vector3[4];
    Quaternion[] allLastRot = new Quaternion[4];
    ControlMap controlMap;
    void Awake()
    {
        controlMap = new ControlMap();

        allController[0] = R_Controller;
        allController[1] = L_Contorller;
        allController[2] = R_Hand;
        allController[3] = L_Hand;
    }
    void OnEnable()
    {
        controlMap.Prototype.Enable();
        controlMap.Prototype.RHandToggle.started += ctx => RHandToggle();
        controlMap.Prototype.RTouchToggle.started += ctx => RTouchToggle();
        controlMap.Prototype.LHandToggle.started += ctx => LHandToggle();
        controlMap.Prototype.LTouchToggle.started += ctx => LTouchToggle();
    }
    void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            if (OVRInput.GetControllerPositionTracked(allController[i]))
            {
                drawers[i].position = OVRInput.GetLocalControllerPosition(allController[i]);
                allLastPos[i] = drawers[i].position;
                indicators[i].pos_mat.color = indicators[i].TrackedColor;
            }
            else
            {
                drawers[i].position = allLastPos[i];
                indicators[i].pos_mat.color = indicators[i].LostTrackedColor;
            }

            if (OVRInput.GetControllerOrientationTracked(allController[i]))
            {
                drawers[i].rotation = OVRInput.GetLocalControllerRotation(allController[i]);
                allLastRot[i] = drawers[i].rotation;
                indicators[i].rot_mat.color = indicators[i].TrackedColor;
            }
            else
            {
                drawers[i].rotation = allLastRot[i];
                indicators[i].rot_mat.color = indicators[i].LostTrackedColor;
            }
        }
    }
    void RTouchToggle()
    {
        drawers[0].gameObject.SetActive(!drawers[0].gameObject.activeSelf);
    }
    void LTouchToggle()
    {
        drawers[1].gameObject.SetActive(!drawers[1].gameObject.activeSelf);
    }
    void RHandToggle()
    {
        drawers[2].gameObject.SetActive(!drawers[2].gameObject.activeSelf);
    }
    void LHandToggle()
    {
        drawers[3].gameObject.SetActive(!drawers[3].gameObject.activeSelf);
    }
}

[System.Serializable]
public class TrackingStatusIndicator
{
    public Material pos_mat;
    public Material rot_mat;
    public Color TrackedColor, LostTrackedColor;
}

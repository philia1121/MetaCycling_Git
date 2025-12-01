using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
public class TrackingDetection : MonoBehaviour
{
    MyInputMap inputMap;
    public Image LH_stateIconB, RH_stateIconB, LH_stateIconC, RH_stateIconC;

    void Awake()
    {
        inputMap = new MyInputMap();
    }
    void OnEnable()
    {
        inputMap.VRBasic.Enable();
        inputMap.VRBasic.LH_TrackState.started += GetTrackState_LH;
        inputMap.VRBasic.RH_TrackState.started += GetTrackState_RH;
        inputMap.VRBasic.LH_TrackState.canceled += GetTrackState_LH;
        inputMap.VRBasic.RH_TrackState.canceled += GetTrackState_RH;
    }
    void OnDisable()
    {
        inputMap.VRBasic.Disable();
        inputMap.VRBasic.LH_TrackState.started -= GetTrackState_LH;
        inputMap.VRBasic.RH_TrackState.started -= GetTrackState_RH;
        inputMap.VRBasic.LH_TrackState.canceled -= GetTrackState_LH;
        inputMap.VRBasic.RH_TrackState.canceled -= GetTrackState_RH;
    }
    void GetTrackState_LH(InputAction.CallbackContext ctx)
    {
        bool isTracked = (ctx.ReadValue<float>() > 0.5f)? true : false; 
        if(isTracked)
        {
            Debug.Log("LH XR Controller isTracked: TRUE");
        }
        else
        {
            Debug.Log("LH XR Controller isTracked: FALSE");
        }
        LH_stateIconC.color = isTracked? Color.green : Color.red;
    }
    void GetTrackState_RH(InputAction.CallbackContext ctx)
    {
        bool isTracked = (ctx.ReadValue<float>() > 0.5f)? true : false; 
        if(isTracked)
        {
            Debug.Log("RH XR Controller isTracked: TRUE");
        }
        else
        {
            Debug.Log("RH XR Controller isTracked: FALSE");
        }
        RH_stateIconC.color = isTracked? Color.green : Color.red;
    }


    // Update is called once per frame
    void Update()
    {
        MethodB();
        
    }

    void MethodB()//checked
    {
        bool leftTracked = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);
        bool rightTracked = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);
        // Debug.Log($"Left Controller Tracked: {leftTracked}, Right Controller Tracked: {rightTracked}");

        LH_stateIconB.color = leftTracked? Color.green : Color.red;
        RH_stateIconB.color = rightTracked? Color.green : Color.red;
    }
}

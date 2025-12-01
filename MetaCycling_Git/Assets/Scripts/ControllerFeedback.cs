using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerFeedback : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.V))
        {
            OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
        }
        if(Input.GetKeyDown(KeyCode.B))
        {
            OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
        }
    }
}

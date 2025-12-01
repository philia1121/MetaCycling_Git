using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerFollower : MonoBehaviour
{
    [SerializeField]private OVRInput.Controller targetController = OVRInput.Controller.RTouch; 
    [SerializeField]private bool autoInitializeModel;
    [SerializeField]private Transform modelTransform;
    [SerializeField]private Vector3 model_initialPos, model_initialRot;
    private Vector3 lastKnownPosition;
    private Quaternion lastKnownRotation;

    void Start()
    {
        if (modelTransform == null)
            modelTransform = this.transform.GetChild(0).transform;
        if(autoInitializeModel)
            modelTransform.position = model_initialPos;
            modelTransform.rotation = Quaternion.Euler(model_initialRot);
    }
    void Update()
    {
        if (OVRInput.GetControllerPositionTracked(targetController))
        {
            transform.position = OVRInput.GetLocalControllerPosition(targetController);
            transform.rotation = OVRInput.GetLocalControllerRotation(targetController);

            lastKnownPosition = transform.position;
            lastKnownRotation = transform.rotation;
        }
        else
        {
            transform.position = lastKnownPosition;
            transform.rotation = lastKnownRotation;
        }
    }
}

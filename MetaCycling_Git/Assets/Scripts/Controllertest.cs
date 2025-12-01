using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controllertest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
         /* if(OVRInput.Get(OVRInput.Button.One, m_controller)==true){
            Debug.Log("press Button 1");
        }
        if(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, m_controller).x>0||OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, m_controller).y>0){
            Debug.Log(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, m_controller).x);
            Debug.Log(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, m_controller).y);
        }
        if(OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger)>0){
            Debug.Log(OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger));
        }  */

        //Trigger Press
        if (OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger) > 0) {
            Debug.Log("R Trigger :"+OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger));
        }
        if (OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger) > 0) {
            Debug.Log("L Trigger :"+OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger));
        }
        //Grip Press
        
        if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) > 0) {
            Debug.Log("L Grip :"+OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger));
        }
        if (OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger) > 0) {
            Debug.Log("R Grip :"+OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger));
        }
        
        

        
        
        //BUTTONS
        //'A' Button Press
        if (OVRInput.GetDown(OVRInput.Button.One)) {
            Debug.Log("A");
        //Do something on A button press down
        }
        //'B' Button Press
        if (OVRInput.GetDown(OVRInput.Button.Two)) {
        //Do something on B button press down
            Debug.Log("B");
        }
        //'Y' Button Press
        if (OVRInput.GetDown(OVRInput.Button.Four)) {
            //Do something on Y button press down
            Debug.Log("Y");
        }
        //'X' Button Press
        if (OVRInput.GetDown(OVRInput.Button.Three)) { 
            Debug.Log("X");
        //Do something on X button press down
        }

        //Button Touch
         if (OVRInput.Get(OVRInput.Touch.One)) {
            Debug.Log("Touch A");
         }
         if (OVRInput.Get(OVRInput.Touch.Two)) {
            Debug.Log("Touch B");
         }
         if (OVRInput.Get(OVRInput.Touch.Three)) {
            Debug.Log("Touch X");
         }
         if (OVRInput.Get(OVRInput.Touch.Four)) {
            Debug.Log("Touch Y");
         }
        if (OVRInput.Get(OVRInput.Touch.SecondaryThumbstick)) {
            Debug.Log("Thumbstick Touch");        
        }
         //'X' Button Press
        if (OVRInput.GetDown(OVRInput.Touch.Three)) { 
            Debug.Log("X");
        //Do something on X button press down
        }
 
        //Right THUMBSTICKS
        if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y > 0) {
            Debug.Log("Thumbstick Up :"+OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y);        
        }
        if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y < 0) {
            Debug.Log("Thumbstick Down :"+OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y); 
        }
        if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x > 0) {
            Debug.Log("Thumbstick Right :"+OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x); 
        }
        if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x < 0) {
            Debug.Log("Thumbstick Left :"+OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x); 
        }
        //Right THUMBSTICKS Pressed
        if (OVRInput.Get(OVRInput.Button.SecondaryThumbstick)){
            Debug.Log("Press Right Thumbstick.");
        }

        //Left THUMBSTICKS
        if (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y > 0) {
            Debug.Log("Thumbstick Up :"+OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y);        
        }
        if (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y < 0) {
            Debug.Log("Thumbstick Down :"+OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y); 
        }
        if (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x > 0) {
            Debug.Log("Thumbstick Left :"+OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x); 
        }
        if (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x < 0) {
            Debug.Log("Thumbstick Right :"+OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).x); 
        }
        //Left THUMBSTICKS Pressed
        if (OVRInput.Get(OVRInput.Button.PrimaryThumbstick)){
            Debug.Log("Press Left Thumbstick.");
        }

        //GRABBING OBJECTS
        //On Controller Grab Down
        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger)) {
        //Change gameobject parent to right controller     
        gameObject.transform.parent = GameObject.Find("RightHandAnchor").transform;
        }
        //On Controller Grab Up
        if (OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger)) {
        //Put back gameobject parent to Root
        gameObject.transform.parent = null;
        }
        
    }
}

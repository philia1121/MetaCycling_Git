using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextMointor : MonoBehaviour
{
    public GameObject Text;
    private float ThumbstickY;
    // Start is called before the first frame update
    void Start()
    {
        ThumbstickY = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
    }

    // Update is called once per frame
    void Update()
    {
        if (ThumbstickY > 0) {
            //Debug.Log("Thumbstick Up :"+OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y); 
            
            string text = "Y:0.00" + ThumbstickY.ToString();
            Text.GetComponent<TextMesh>().text = text;

        }
    }
}

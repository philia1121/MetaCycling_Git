using System.Collections;
using System.Collections.Generic;
using Meta.XR.ImmersiveDebugger.UserInterface;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ControllerHaptic : MonoBehaviour
{
    [SerializeField]private bool active = true;
    [SerializeField]private float duration = 1;
    [Range(0, 1)][SerializeField]private float amplitude = 1;
    [SerializeField]private OVRInput.Controller targetController;
    IEnumerator cor;
    float timer;

    public void Rumble()
    {
        if(!active) return;
        if(cor != null) StopCoroutine(cor);
        cor = SetHaptic();
        StartCoroutine(cor);
    }
    
    IEnumerator SetHaptic()
    {
        timer = duration;
        OVRInput.SetControllerVibration(1, amplitude, targetController);

        if(duration < 2.5f) // the vibration of OVRInput.SetControllerVibration can only last 2.5 sec. 
        {
            yield return new WaitForSeconds(duration);
        }
        else
        {
            while(timer > 2f)
            {
                timer -= 2f;
                yield return new WaitForSeconds(2f);
                OVRInput.SetControllerVibration(1, amplitude, targetController);
            }
            OVRInput.SetControllerVibration(1, amplitude, targetController);
            yield return new WaitForSeconds(timer);
        }
    
        OVRInput.SetControllerVibration(0, 0, targetController);
    }

    public void SetRumbleDuration(float value){ duration = value;}
    public void SetRumbleaAmplitude(float value){ amplitude = value;}
    public void SetRumbleCallActive(bool value){ active = value;}
}

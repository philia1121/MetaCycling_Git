using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class InfoBoard : MonoBehaviour
{
    public TextMeshProUGUI Timer;
    public TextMeshProUGUI CountDown;
    public Image LH_stateIcon, RH_stateIcon;
    public Image ObserverState, ParticipantState;
    public AutoLogManager logManager;
    void Update()
    {
        Timer.text = Time.time.ToString("0.00");
        CountDown.text = logManager.recordCount.ToString();
        ObserverState.color = logManager.Observer_Tracked? Color.green : Color.red;
        ParticipantState.color = logManager.Participant_Tracked? Color.green : Color.red;
        LH_stateIcon.color = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch)? Color.green : Color.red;
        RH_stateIcon.color = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch)? Color.green : Color.red;
    }
    
}

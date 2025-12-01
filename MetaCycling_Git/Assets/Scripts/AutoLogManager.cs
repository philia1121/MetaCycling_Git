using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoLogManager : MonoBehaviour
{
    public bool auto = true;
    public float LogInterval = 1;
    public bool Participant_Tracked = true;
    public bool Observer_Tracked = true;
    public bool Participant_Changed = false;
    public bool Observer_Changed = false;
    public bool RTouch_Tracked, LTouch_Tracked;
    public int recordCount = 130;

    void Start()
    {
        if(auto)
            CSVWriter.CSV_WriteTableTitle("Real World Time,Game World Time,Participant,Observer,LTouch,RTouch");
            StartCoroutine(AutoLog());
    }

    public void StartAutoLog()
    {
        auto = true;
        CSVWriter.CSV_WriteTableTitle("Real World Time,Game World Time,Participant,Observer,LTouch,RTouch");
        StartCoroutine(AutoLog());
    }

    IEnumerator AutoLog()
    {
        while(auto)
        {
            string participant = Participant_Tracked? "Tracked" : "Lost";
            string observer = Observer_Tracked? "Tracked" : "Lost";
            participant += Participant_Changed? " O," : ",";
            observer += Observer_Changed? " O," : ",";
            Participant_Changed = false;
            Observer_Changed = false;
            string L = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch)? "Tracked," : "Lost,";
            string R = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch)? "Tracked," : "Lost,";
            string data = participant + observer + L + R;
            CSVWriter.CSV_WriteByTime(true, true, data);

            recordCount --;
            auto = (recordCount < 1)? false : true;
            
            yield return new WaitForSeconds(LogInterval);
        }
        yield return null;
    }
    public void ParticipantTrackedReport(bool tracked)
    {
        Participant_Tracked = tracked;
        Participant_Changed = true;
    }
    public void ObserverTrackedReport(bool tracked)
    {
        Observer_Tracked = tracked;
        Observer_Changed = true;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HandInRangeEvent : MonoBehaviour
{
    [SerializeField]private bool RHandInRange;
    [SerializeField]private bool LHandInRange;
    
    [Header("")]
    public UnityEvent InRangeEvent, NotInRangeEvent, BothInRangeEvent;
    [SerializeField]private bool showLog = false;
    bool wasIn; 
    bool wasBothIn;
    public void SetRHandInRange(bool value){ RHandInRange = value;}
    public void SetLHandInRange(bool value){ LHandInRange = value;}

    void Start()
    {
        wasIn = RHandInRange | LHandInRange;
        wasBothIn = RHandInRange & LHandInRange;
    }

    public void CheckPettingState()
    {
        var isIn = RHandInRange | LHandInRange;
        if(isIn != wasIn)
        {
            if(isIn)
            {
                InRangeEvent.Invoke();
                if(showLog) Debug.Log("In Range Triggered");
            }
            else
            {
                NotInRangeEvent.Invoke();
                if(showLog) Debug.Log("Not In Range Triggered");
            }
        }
        wasIn = RHandInRange | LHandInRange;

        var isBothIn = RHandInRange & LHandInRange;
        if(isBothIn != wasBothIn)
        {
            if(isIn)
            {
                BothInRangeEvent.Invoke();
                if(showLog) Debug.Log("Both In Range Triggered");
            }
        }
        wasBothIn = RHandInRange & LHandInRange;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class AnimationEvent : MonoBehaviour
{
    [SerializeField]private int targetNumber;
    public UnityEvent AnimationTriggerEvent;
    void AnimationNumberEvent(int i) //Animaiton > Animation Clip > Events > Function
    {
        if (targetNumber != 0 && i == targetNumber)
        {
            AnimationTriggerEvent.Invoke();
        }
    }
    public void ChangeTargetNumber(int value)
    {
        targetNumber = value;
    }
}

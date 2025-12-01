using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SimpleTriggerEvent : MonoBehaviour
{
    [SerializeField]private CheckForType checkFor= CheckForType.Everyone;
    [SerializeField]private string target = "Player";
    
    [Header("Event Settings")]
    [SerializeField]private bool checkOnEnter = true;
    public UnityEvent OnEnter = new UnityEvent();
    [SerializeField]private bool checkOnExit = false;
    public UnityEvent OnExit = new UnityEvent();

    [SerializeField]private bool showLog = false;

    void OnTriggerEnter(Collider other)
    {
        if(checkOnEnter)
        {
            if(checkFor == CheckForType.Everyone | 
                (checkFor == CheckForType.Tag && other.tag == target) | 
                (checkFor == CheckForType.Name && other.name == target))
            {
                OnEnter.Invoke();
                if(showLog) Debug.Log("Trigger On Enter Simple Event");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if(checkOnExit)
        {
            if(checkFor == CheckForType.Everyone | 
                (checkFor == CheckForType.Tag && other.tag == target) | 
                (checkFor == CheckForType.Name && other.name == target))
            {
                OnExit.Invoke();
                if(showLog) Debug.Log("Trigger On Exit Simple Event");
            }
        }
    }
}
[System.Serializable]
public enum CheckForType
{
    Everyone,
    Name,
    Tag,
}
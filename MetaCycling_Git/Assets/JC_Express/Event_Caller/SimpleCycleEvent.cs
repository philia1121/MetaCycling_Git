using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SimpleCycleEvent : MonoBehaviour
{
    [SerializeField]private bool onAwake;
    public UnityEvent OnAwakeEvent;
    [SerializeField]private bool onStart;
    public UnityEvent OnStartEvent;
    [SerializeField]private bool onEnable;
    public UnityEvent OnEnableEvent;
    [SerializeField]private bool onDisable;
    public UnityEvent OnDisableEvent;

    [SerializeField]private bool showLog = false;

    void Awake()
    {
        if(onAwake) 
            OnAwakeEvent.Invoke();
            if(showLog) Debug.Log("Invoke on Awake(): " + this.gameObject.name);
    }
    void Start()
    {
        if(onStart) 
            OnStartEvent.Invoke();
            if(showLog) Debug.Log("Invoke on Start():" + this.gameObject.name);
    }
    void OnEnable()
    {
        if(onEnable) 
            OnEnableEvent.Invoke();
            if(showLog) Debug.Log("Invoke on OnEnable():" + this.gameObject.name);
    }
    void OnDisable()
    {
        if(onDisable)
            OnDisableEvent.Invoke();
            if(showLog) Debug.Log("Invoke on OnDisabled():" + this.gameObject.name);
    }
}

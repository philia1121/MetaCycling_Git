using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class SimpleTimerEvent : MonoBehaviour
{
    [SerializeField]private bool onStartTimer;
    public UnityEvent StartTimerEvent;
    [SerializeField]private bool onEndTimer;
    public UnityEvent EndTimerEvent;

    [Header("Random Settings")]
    [SerializeField] private float minInterval;
    [SerializeField] private float maxInterval;
    [SerializeField] private bool keepRandom = true;
    bool doRandom;
    IEnumerator cor;
    public UnityEvent RandomTriggerEvent;
    [SerializeField]private bool showLog = false;

    public void SetSimpleTimer(float duration)
    {
        if(onStartTimer)
            StartTimerEvent.Invoke();
            if(showLog) Debug.Log("Invoke on Start Timer: " + this.gameObject.name);
        Invoke("SimpleWait", duration);
    }
    void SimpleWait()
    {
        EndTimerEvent.Invoke();
        if(showLog) Debug.Log("Invoke on End Timer: " + this.gameObject.name);
    }

    public void SetRandomTimer(bool start)
    {
        if (start)
        {
            if (cor != null) return;
            cor = RandomTimer();
            StartCoroutine(cor);
        }
        else
        {
            if (cor != null) StopCoroutine(cor);
            cor = null;
        }
    }
    IEnumerator RandomTimer()
    {
        doRandom = true;
        while (doRandom)
        {
            float interval = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(interval);
            if (doRandom) RandomTriggerEvent.Invoke();
            if (showLog) Debug.Log("Random Timer Event Invoked");
            if (!keepRandom) doRandom = false;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
public class DurationCheckEvent : MonoBehaviour
{
    [SerializeField] private float duration = 1;
    [SerializeField] private float count = 0;
    IEnumerator cor;
    public UnityEvent TimeoutEvent;
    [SerializeField]private bool showLog = false;
    public void SetTimeCounter(bool start)
    {
        if (start)
        {
            if (cor != null) return;
            cor = TimeCounter();
            StartCoroutine(cor);
        }
        else
        {
            if (cor != null) StopCoroutine(cor);
            cor = null;
        }
    }
    public void ResetCount()
    {
        count = 0;
    }
    IEnumerator TimeCounter()
    {
        while (true)
        {
            count += Time.deltaTime;
            if (count >= duration)
            {
                TimeoutEvent.Invoke();
                if (showLog) Debug.Log("time out");
                StopCoroutine(cor);
            }
            yield return null;
        }
    }
}

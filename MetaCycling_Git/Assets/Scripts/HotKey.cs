using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
public class HotKey : MonoBehaviour
{
    public bool showFiredText;
    public KeyCode key;
    public UnityEvent HotKeyEvent;

    public void Update()
    {
        if(Input.GetKeyDown(key))
        {
            Debug.Log("Fired: " + this.gameObject.name);
            HotKeyEvent.Invoke();
        }
    }
}

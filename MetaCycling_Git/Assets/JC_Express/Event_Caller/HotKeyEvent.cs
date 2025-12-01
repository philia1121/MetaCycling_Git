using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HotKeyEvent : MonoBehaviour
{
    [SerializeField] private KeyCode Key;
    public UnityEvent KeyEvent;
    
    void Update()
    {
        if(Input.GetKeyDown(Key))
        {
            KeyEvent.Invoke();
        }
    }
}

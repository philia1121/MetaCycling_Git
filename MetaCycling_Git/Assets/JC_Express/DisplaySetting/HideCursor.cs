using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class HideCursor : MonoBehaviour
{
    void Awake()
    {
        #if UNITY_EDITOR
            Cursor.lockState = CursorLockMode.None;
        #else
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked; 
        #endif
    }
}

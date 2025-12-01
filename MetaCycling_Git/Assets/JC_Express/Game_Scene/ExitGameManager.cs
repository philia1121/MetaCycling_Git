using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExitGameManager : MonoBehaviour
{
    [SerializeField]private bool autoExitGame = true;
    [SerializeField]private float gameDuration = 30;

    void Start()
    {
        if(autoExitGame)
        {
            Invoke("Exit", gameDuration);
        }   
    }
    public void StartAutoExitGame()
    {
        Invoke("Exit", gameDuration);
    }
    public void Exit()
    {
        #if UNITY_EDITOR
            EditorApplication .isPlaying = false; //unity editor stop play mode
        #else
            Application.Quit(); //build.exe stop play mode
        #endif
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsMenuManager : MonoBehaviour
{
    ControlMap controlMap;
    public GameObject settingsMenu;
    // Start is called before the first frame update
    void Awake()
    {
        controlMap = new ControlMap();
    }
    void OnEnable()
    {
        controlMap.Prototype.Enable();
        controlMap.Prototype.SettingsToggle.started += ctx => SettingsMenuToggle();
    }
    public void SettingsMenuToggle()
    {
        settingsMenu.SetActive(!settingsMenu.activeSelf);
    }
}

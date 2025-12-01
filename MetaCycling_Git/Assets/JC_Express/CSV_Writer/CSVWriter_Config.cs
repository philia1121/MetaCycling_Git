using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSVWriter_Config : MonoBehaviour
{
    [SerializeField]private NamingType namingType = NamingType.Custom;
    [SerializeField]private string fileName;
    void Awake()
    {
        string fName = "DefaultFileName";
        switch(namingType)
        {
            case NamingType.Custom:
                fName = (fileName == null)? "default" : fileName;
                break;
            case NamingType.TimeLog:
                fName = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                break;
            case NamingType.Guid:
                fName = System.Guid.NewGuid().ToString();
                break;
        }
        
        CSVWriter.CSV_SetFileName(fName);
    }  
}

[System.Serializable]
public enum NamingType
{
    Custom,
    TimeLog,
    Guid,
}
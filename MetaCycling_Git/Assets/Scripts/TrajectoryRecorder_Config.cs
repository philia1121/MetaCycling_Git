using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryRecorder_Config : MonoBehaviour
{
    [SerializeField] private bool setOnAwake = true;
    [SerializeField]private NamingType namingType = NamingType.Custom;
    [SerializeField] private string filePrefix;

    void Awake()
    {
        if (!setOnAwake) return;

        string fPrefix = "";
        switch (namingType)
        {
            case NamingType.Custom:
                fPrefix = (filePrefix == null) ? "" : filePrefix;
                break;
            case NamingType.TimeLog:
                fPrefix = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                break;
            case NamingType.Guid:
                fPrefix = System.Guid.NewGuid().ToString();
                break;
        }
        if (TrajectoryRecorder.instance) TrajectoryRecorder.instance.SetFilePrefix(fPrefix);
    }
    public void ChangeConfig(string prefix)
    {
        if (TrajectoryRecorder.instance) TrajectoryRecorder.instance.SetFilePrefix(prefix);
    }
    public void ChangeConfigRandom()
    {
        if (TrajectoryRecorder.instance) TrajectoryRecorder.instance.SetFilePrefix(System.Guid.NewGuid().ToString());
    }
}

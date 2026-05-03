using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryRecorder_Config : MonoBehaviour
{
    [SerializeField] private bool setOnAwake = false;
    [SerializeField] private NamingType namingType = NamingType.Custom;
    [SerializeField] private string filePrefix = "MultiTraj";
    [SerializeField] private string MotionType = "Undefined";

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
                fPrefix = System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
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
    public void ChangeMotionType(string motion)
    {
        if (TrajectoryRecorder.instance) TrajectoryRecorder.instance.SetMotionType(motion);
    }
    public void ChangeUserName(string name)
    {
        if (TrajectoryRecorder.instance) TrajectoryRecorder.instance.SetUserName(name);
    }
}

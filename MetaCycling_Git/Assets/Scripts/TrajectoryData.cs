using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Waypoint
{
    public Vector3 position;
    public Quaternion rotation;
    public float timestamp; // 用於重現速度節奏

    public Waypoint(Vector3 pos, Quaternion rot, float time)
    {
        position = pos;
        rotation = rot;
        timestamp = time;
    }
}
[System.Serializable]
public class MultiTrackWaypoint
{
    public float timestamp;

    // 1. 左控制器 (Left Controller / LTouch)
    public Vector3 posL_Cont;
    public Quaternion rotL_Cont;

    // 2. 右控制器 (Right Controller / RTouch)
    public Vector3 posR_Cont;
    public Quaternion rotR_Cont;

    // 3. 左手 (Left Hand / LHand - 需開啟 Hand Tracking 或 Multimodal)
    public Vector3 posL_Hand;
    public Quaternion rotL_Hand;

    // 4. 右手 (Right Hand / RHand - 需開啟 Hand Tracking 或 Multimodal)
    public Vector3 posR_Hand;
    public Quaternion rotR_Hand;
}

[System.Serializable]
public class TrajectorySession
{
    public List<MultiTrackWaypoint> waypoints = new List<MultiTrackWaypoint>();
}

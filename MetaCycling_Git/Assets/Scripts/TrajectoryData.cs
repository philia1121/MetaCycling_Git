using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MultiTrackWaypoint
{
    public float timestamp;

    // Left Controller
    public Vector3 pos_LCont;
    public Quaternion rot_LCont;

    // Right Controller
    public Vector3 pos_RCont;
    public Quaternion rot_RCont;

    // Left Hand
    public Vector3 pos_LHand;
    public Quaternion rot_LHand;

    // Right Hand
    public Vector3 pos_RHand;
    public Quaternion rot_RHand;

    // HMD
    public Vector3 pos_HMD;
    public Quaternion rot_HMD;

    public bool RHand_PosTracked;
    public bool RHand_RotTracked;
    public bool RCont_PosTracked;
    public bool RCont_RotTracked;
    public bool LHand_PosTracked;
    public bool LHand_RotTracked;
    public bool LCont_PosTracked;
    public bool LCont_RotTracked;
}

[System.Serializable]
public class TrajectorySession
{
    public List<MultiTrackWaypoint> waypoints = new List<MultiTrackWaypoint>();
}

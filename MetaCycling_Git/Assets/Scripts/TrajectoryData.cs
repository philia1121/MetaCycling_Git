using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;
using Firebase.Extensions;

[FirestoreData]
[System.Serializable]
public class MultiTrackWaypoint
{
    public MultiTrackWaypoint() { }
    [FirestoreProperty] public float timestamp { get; set; }

    // Left Controller
    [FirestoreProperty] public Vector3 pos_LCont { get; set; }
    [FirestoreProperty] public Quaternion rot_LCont { get; set; }

    // Right Controller
    [FirestoreProperty] public Vector3 pos_RCont { get; set; }
    [FirestoreProperty] public Quaternion rot_RCont { get; set; }

    // Left Hand
    [FirestoreProperty] public Vector3 pos_LHand { get; set; }
    [FirestoreProperty] public Quaternion rot_LHand { get; set; }

    // Right Hand
    [FirestoreProperty] public Vector3 pos_RHand { get; set; }
    [FirestoreProperty] public Quaternion rot_RHand { get; set; }

    // HMD
    [FirestoreProperty] public Vector3 pos_HMD { get; set; }
    [FirestoreProperty] public Quaternion rot_HMD { get; set; }

    [FirestoreProperty] public bool RHand_PosTracked { get; set; }
    [FirestoreProperty] public bool RHand_RotTracked { get; set; }
    [FirestoreProperty] public bool RCont_PosTracked { get; set; }
    [FirestoreProperty] public bool RCont_RotTracked { get; set; }
    [FirestoreProperty] public bool LHand_PosTracked { get; set; }
    [FirestoreProperty] public bool LHand_RotTracked { get; set; }
    [FirestoreProperty] public bool LCont_PosTracked { get; set; }
    [FirestoreProperty] public bool LCont_RotTracked { get; set; }
}

[FirestoreData]
[System.Serializable]
public class TrajectorySession
{
    public TrajectorySession() { }
    [FirestoreProperty] public string userName { get; set; } = "";
    [FirestoreProperty] public string motionType { get; set; } = "";
    [FirestoreProperty] public List<MultiTrackWaypoint> waypoints { get; set; } = new List<MultiTrackWaypoint>();
}



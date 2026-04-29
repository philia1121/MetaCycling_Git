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
    [FirestoreProperty] public SerializableVector3 pos_LCont { get; set; }
    [FirestoreProperty] public SerializableQuaternion rot_LCont { get; set; }

    // Right Controller
    [FirestoreProperty] public SerializableVector3 pos_RCont { get; set; }
    [FirestoreProperty] public SerializableQuaternion rot_RCont { get; set; }

    // Left Hand
    [FirestoreProperty] public SerializableVector3 pos_LHand { get; set; }
    [FirestoreProperty] public SerializableQuaternion rot_LHand { get; set; }

    // Right Hand
    [FirestoreProperty] public SerializableVector3 pos_RHand { get; set; }
    [FirestoreProperty] public SerializableQuaternion rot_RHand { get; set; }

    // HMD
    [FirestoreProperty] public SerializableVector3 pos_HMD { get; set; }
    [FirestoreProperty] public SerializableQuaternion rot_HMD { get; set; }

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

[FirestoreData]
[System.Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;
    public SerializableVector3() { }

    public SerializableVector3(Vector3 rValue)
    {
        x = rValue.x;
        y = rValue.y;
        z = rValue.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[FirestoreData]
[System.Serializable]
public class SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;
    public SerializableQuaternion(){}
    public SerializableQuaternion(Quaternion rValue)
    {
        x = rValue.x;
        y = rValue.y;
        z = rValue.z;
        w = rValue.w;
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}


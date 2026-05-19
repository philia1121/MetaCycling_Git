using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;
using Firebase.Extensions;
using Newtonsoft.Json;

#region AOS
// AoS, the old version of JSON data structure
[System.Serializable]
public class MultiTrackWaypoint
{
    public MultiTrackWaypoint(){}
    public double timestamp;

    // Left Controller
    public SerializableVector3 pos_LCont;
    public SerializableQuaternion rot_LCont;

    // Right Controller
    public SerializableVector3 pos_RCont;
    public SerializableQuaternion rot_RCont;

    // Left Hand
    public SerializableVector3 pos_LHand;
    public SerializableQuaternion rot_LHand;

    // Right Hand
    public SerializableVector3 pos_RHand;
    public SerializableQuaternion rot_RHand;

    // HMD
    public SerializableVector3 pos_HMD;
    public SerializableQuaternion rot_HMD;

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
    public TrajectorySession(){}
    public string userName;
    public string motionType;
    public List<MultiTrackWaypoint> waypoints = new List<MultiTrackWaypoint>();
}
#endregion

#region SoA
// SoA, the new version of JSON data structure
[FirestoreData]
[System.Serializable]
public class FireRecordData
{
    public FireRecordData() { }
    [FirestoreProperty] public string userName { get; set; } = "";
    [FirestoreProperty] public string motionType { get; set; } = "";
    [FirestoreProperty] public string recordTime { get; set; } = "";
    [FirestoreProperty] public double sampleInterval { get; set; } = 0.015f;

    [FirestoreProperty] public List<double> timeStamp { get; set; } = new List<double>();
    [FirestoreProperty] public List<SerializableVector3> pHMD { get; set; } = new List<SerializableVector3>();
    [FirestoreProperty] public List<SerializableQuaternion> rHMD { get; set; } = new List<SerializableQuaternion>();
    [FirestoreProperty] public List<SerializableVector3> pRH { get; set; } = new List<SerializableVector3>();
    [FirestoreProperty] public List<SerializableQuaternion> rRH { get; set; } = new List<SerializableQuaternion>();
    [FirestoreProperty] public List<SerializableVector3> pLH { get; set; } = new List<SerializableVector3>();
    [FirestoreProperty] public List<SerializableQuaternion> rLH { get; set; } = new List<SerializableQuaternion>();
    [FirestoreProperty] public List<SerializableVector3> pRC { get; set; } = new List<SerializableVector3>();
    [FirestoreProperty] public List<SerializableQuaternion> rRC { get; set; } = new List<SerializableQuaternion>();
    [FirestoreProperty] public List<SerializableVector3> pLC { get; set; } = new List<SerializableVector3>();
    [FirestoreProperty] public List<SerializableQuaternion> rLC { get; set; } = new List<SerializableQuaternion>();
    [FirestoreProperty] public List<bool> ptRH { get; set; } = new List<bool>();
    [FirestoreProperty] public List<bool> rtRH { get; set; } = new List<bool>();
    [FirestoreProperty] public List<bool> ptLH { get; set; } = new List<bool>();
    [FirestoreProperty] public List<bool> rtLH { get; set; } = new List<bool>();
    [FirestoreProperty] public List<bool> ptRC { get; set; } = new List<bool>();
    [FirestoreProperty] public List<bool> rtRC { get; set; } = new List<bool>();
    [FirestoreProperty] public List<bool> ptLC { get; set; } = new List<bool>();
    [FirestoreProperty] public List<bool> rtLC { get; set; } = new List<bool>();
}


[System.Serializable]
public class StorageRecordData
{
    public StorageRecordData() { }
    public string userName = "";
    public string motionType = "";
    public string recordTime = "";
    public double sampleInterval = 0.015f;

    public List<double> timeStamp = new List<double>();
    public List<SerializableVector3> pHMD = new List<SerializableVector3>();
    public List<SerializableQuaternion> rHMD = new List<SerializableQuaternion>();
    public List<SerializableVector3> pRH = new List<SerializableVector3>();
    public List<SerializableQuaternion> rRH = new List<SerializableQuaternion>();
    public List<SerializableVector3> pLH = new List<SerializableVector3>();
    public List<SerializableQuaternion> rLH = new List<SerializableQuaternion>();
    public List<SerializableVector3> pRC = new List<SerializableVector3>();
    public List<SerializableQuaternion> rRC = new List<SerializableQuaternion>();
    public List<SerializableVector3> pLC = new List<SerializableVector3>();
    public List<SerializableQuaternion> rLC = new List<SerializableQuaternion>();
    public List<bool> ptRH = new List<bool>();
    public List<bool> rtRH = new List<bool>();
    public List<bool> ptLH = new List<bool>();
    public List<bool> rtLH = new List<bool>();
    public List<bool> ptRC = new List<bool>();
    public List<bool> rtRC = new List<bool>();
    public List<bool> ptLC = new List<bool>();
    public List<bool> rtLC = new List<bool>();
}
#endregion

#region  JSON Serializable

[System.Serializable]
public class SerializableVector3
{
    public double x;
    public double y;
    public double z;
    public SerializableVector3() { }

    public SerializableVector3(Vector3 rValue)
    {
        x = Math.Round(rValue.x, 4);
        y = Math.Round(rValue.y, 4);
        z = Math.Round(rValue.z, 4);
    }

    public Vector3 ToVector3()
    {
        return new Vector3((float)x, (float)y, (float)z);
    }
}
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
#endregion
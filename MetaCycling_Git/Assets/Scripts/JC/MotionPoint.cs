using UnityEngine;
using System;

[Serializable]
public struct MotionPoint
{
    public Vector3 position;
    public Quaternion rotation;
    public float timestamp;
    public float normalizedTime;
    public Vector3 velocity;
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataVisualizer : MonoBehaviour
{
    public PetrollerObjectInfo info;

    public Transform Parent_Transform, Drawer_Transform;
    public float max_Velocity;

    public bool debug = false;
    public Vector3 debug_Value;
    void Start()
    {
        Drawer_Transform.localPosition = new Vector3(0, 0, 0);
    }
    void Update()
    {
        Drawer_Transform.localPosition = debug ? ValueMapping(debug_Value, max_Velocity) : ValueMapping(info.Velocity, max_Velocity);
    }
    Vector3 ValueMapping(Vector3 origin, float max)
    {
        float x = ((Mathf.Abs(origin.x / max) > 1) ? (origin.x > 1 ? 1 : -1) : origin.x / max) / 2;
        float y = ((Mathf.Abs(origin.y / max) > 1) ? (origin.y > 1 ? 1 : -1) : origin.y / max) / 2;
        float z = ((Mathf.Abs(origin.z / max) > 1) ? (origin.z > 1 ? 1 : -1) : origin.z / max) / 2;
        return new Vector3(x, y, z);
    }
}

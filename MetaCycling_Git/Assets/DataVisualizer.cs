using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class DataVisualizer : MonoBehaviour
{
    public PetrollerObjectInfo info;

    public Transform Parent_Transform;
    public Transform[] Drawer_Transforms;
    public float max_Velocity;

    public bool debug = false;
    public Vector3 debug_Value;
    void Start()
    {
        foreach (var draw in Drawer_Transforms) draw.localPosition = new Vector3(0, 0, 0);
    }
    void Update()
    {
        Drawer_Transforms[0].localPosition = debug ? ValueMapping(debug_Value, max_Velocity) : ReValue(ValueMapping(info.Velocity, max_Velocity), "x");
        Drawer_Transforms[1].localPosition = debug ? ValueMapping(debug_Value, max_Velocity) : ReValue(ValueMapping(info.Velocity, max_Velocity), "y");
        Drawer_Transforms[2].localPosition = debug ? ValueMapping(debug_Value, max_Velocity) : ReValue(ValueMapping(info.Velocity, max_Velocity), "z");
    }
    Vector3 ValueMapping(Vector3 origin, float max)
    {
        float x = ((Mathf.Abs(origin.x / max) > 1) ? (origin.x > 1 ? 1 : -1) : origin.x / max) / (Parent_Transform.localScale.x / 2);
        float y = ((Mathf.Abs(origin.y / max) > 1) ? (origin.y > 1 ? 1 : -1) : origin.y / max) / (Parent_Transform.localScale.y / 2);
        float z = ((Mathf.Abs(origin.z / max) > 1) ? (origin.z > 1 ? 1 : -1) : origin.z / max) / (Parent_Transform.localScale.z / 2);
        return new Vector3(x, y, z);
    }
    Vector3 ReValue(Vector3 orig, string want)
    {
        switch (want)
        {
            case "x":
                return new Vector3(orig.x, 0, 0);
            case "y":
                return new Vector3(0, orig.y, 0);
            case "z":
                return new Vector3(0, 0, orig.z);
            default:
                return Vector3.zero;
        }
    }
}

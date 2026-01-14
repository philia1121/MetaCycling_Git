using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectRotationReader : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject target;

    [Header("Rotation Visualizer")]
    [SerializeField] private UIRotVisualizer rotX;
    [SerializeField] private UIRotVisualizer rotY;
    [SerializeField] private UIRotVisualizer rotZ;

    private Quaternion lastRotation;
    private float accumulatedX, accumulatedY, accumulatedZ;
    private void Update()
    {
        if(target == null)
            return;
        CalcEditorRotation();

        rotX.SetFillamount(accumulatedX);
        rotY.SetFillamount(accumulatedY);
        rotZ.SetFillamount(accumulatedZ);
    }

    private void CalcEditorRotation()
    {
        Quaternion current = target.transform.rotation;

        // calc rot
        Quaternion delta = current * Quaternion.Inverse(lastRotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        // Project angle to each axis
        float signX = angle * Mathf.Sign(Vector3.Dot(axis, target.transform.right));
        float signY = angle * Mathf.Sign(Vector3.Dot(axis, target.transform.up));
        float signZ = angle * Mathf.Sign(Vector3.Dot(axis, target.transform.forward));

        accumulatedX += signX;
        accumulatedY += signY;
        accumulatedZ += signZ;

        lastRotation = current;
    }
}

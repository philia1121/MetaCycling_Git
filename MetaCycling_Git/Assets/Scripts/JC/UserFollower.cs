using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserFollower : MonoBehaviour
{
    public Transform MainCam;
    public Vector3 offset;
    void Start()
    {
        if (!MainCam) MainCam = Camera.main.transform;
    }

    void Update()
    {
        transform.position = MainCam.TransformPoint(offset);

        Vector3 headRotation = MainCam.eulerAngles;
        Quaternion targetRotation = Quaternion.Euler(0, headRotation.y, 0);
        transform.rotation = targetRotation;
    }
}

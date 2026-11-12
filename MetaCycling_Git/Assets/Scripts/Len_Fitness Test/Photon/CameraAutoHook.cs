using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAutoHook : MonoBehaviour
{
    [SerializeField] private ThirdPersonCamera orbitCam;
    [SerializeField] private FixedTouchField touchField;
    [SerializeField] private GameObject planeFab;
    [SerializeField] private GameObject target;

    private void Start()
    {
        Instantiate(planeFab, Vector3.zero, Quaternion.identity);
        FindAndHookTarget();
    }

    private void Update()
    {
        // If the camera doesn't have a target yet, keep scanning the network objects
        if (orbitCam != null && !orbitCam.HasTarget())
        {
            FindAndHookTarget();
        }
    }

    private void FindAndHookTarget()
    {
            orbitCam.SetTarget(target.transform);
            orbitCam.SetTouchFIeld(touchField);
            Debug.Log($"[PC Spectator] Successfully locked camera onto VR Player footprint anchor!");
    }
}

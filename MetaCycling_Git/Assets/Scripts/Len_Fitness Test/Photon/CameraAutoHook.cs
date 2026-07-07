using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAutoHook : MonoBehaviour
{
    [SerializeField] private ThirdPersonCamera orbitCam;
    [SerializeField] private FixedTouchField touchField;
    [SerializeField] private GameObject planeFab;

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
        // Find all TrackingManagers instantiated over Photon
        TrackingManager[] trackers = FindObjectsByType<TrackingManager>(FindObjectsSortMode.None);

        foreach (var tracker in trackers)
        {
            PhotonView pv = tracker.GetComponentInParent<PhotonView>();

            // We want to track the REMOTE player (The Headset client, which is NOT Mine on PC)
            if (pv != null && !pv.IsMine && tracker.userBasePos != null)
            {
                orbitCam.SetTarget(tracker.userBasePos);
                orbitCam.SetTouchFIeld(touchField);
                Debug.Log($"[PC Spectator] Successfully locked camera onto VR Player footprint anchor!");
                break;
            }
        }
    }
}

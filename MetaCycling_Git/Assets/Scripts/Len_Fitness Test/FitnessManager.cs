using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FitnessManager : MonoBehaviour
{
    [SerializeField] private Transform hmdTransform;
    public static FitnessManager instance;

    private TrajectoryRecorder trajectoryRecorder;
    private PathVisualizer path;

    //var for the calibrated start and end point
    public Vector3 calibratedStartPos;
    public Vector3 calibratedEndPos;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        trajectoryRecorder = TrajectoryRecorder.instance;
        path = PathVisualizer.instance;

        calibratedStartPos = new Vector3(hmdTransform.transform.position.x, 0, hmdTransform.transform.position.z);
    }

    public void StartMovement(Action<bool> onComplete)
    {
        calibratedStartPos = new Vector3(hmdTransform.transform.position.x, 0, hmdTransform.transform.position.z);

        trajectoryRecorder.StartRecording();
        path.StartRecording();

        onComplete?.Invoke(true);
    }

    public JumpResult EndMovement()
    {
        trajectoryRecorder.StopRecording();
        path.EndRecording();

        path.DisplayPath();

        return new JumpResult
        {
            success = true,
            distance = 0,
            height = 0
        };
    }

}

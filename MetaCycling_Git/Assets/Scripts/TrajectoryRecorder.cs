using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
public class TrajectoryRecorder : MonoBehaviour
{
    public float recordInterval = 0.015f;
    public bool isRecording = false;
    public Material mat;
    private TrajectorySession currentSession;
    private float startTime;
    ControlMap controlMap;
    Transform VRMainCam;
    public TrackingInfo trackingInfo;
    Coroutine cor;
    void Awake()
    {
        controlMap = new ControlMap();
        VRMainCam = Camera.main.transform;
        trackingInfo = FindFirstObjectByType<TrackingInfo>();
        if (!trackingInfo) trackingInfo = gameObject.AddComponent<TrackingInfo>();
    }
    void OnEnable()
    {
        controlMap.Prototype.Enable();
        controlMap.Prototype.RecordButton.started += ctx => ToggleRecording();
    }

    public void ToggleRecording()
    {
        isRecording = !isRecording;
        if (isRecording)
        {
            if (mat) mat.color = Color.red;
            StartNewSession();
            if (cor != null) StopCoroutine(cor);
            cor = StartCoroutine(RecordRoutine());
            Debug.Log("start recording");
        }
        else
        {
            if (mat) mat.color = Color.white;
            if (cor != null) StopCoroutine(cor);
            SaveToFile();
            Debug.Log("stop recording");
        }
    }
    IEnumerator RecordRoutine()
    {
        while (isRecording)
        {
            OnRecord();
            yield return new WaitForSeconds(recordInterval);
        }
    }
    void StartNewSession()
    {
        currentSession = new TrajectorySession();
        startTime = Time.time;
    }
    void OnRecord()
    {
        // Time Stamp
        float timeSinceStart = Time.time - startTime;
        MultiTrackWaypoint wp = new MultiTrackWaypoint();
        wp.timestamp = timeSinceStart;

        // Left Controller
        wp.pos_LCont = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        wp.rot_LCont = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch).eulerAngles;

        // Right Controller
        wp.pos_RCont = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        wp.rot_RCont = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch).eulerAngles;
        // Left Hand
        wp.pos_LHand = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LHand);
        wp.rot_LHand = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LHand).eulerAngles;

        // Right Hand
        wp.pos_RHand = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RHand);
        wp.rot_RHand = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RHand).eulerAngles;

        // HMD
        wp.pos_HMD = VRMainCam.position;
        wp.rot_HMD = VRMainCam.rotation.eulerAngles;

        // Tracking State
        wp.RHand_PosTracked = trackingInfo.Get_RHand_PosTracked();
        wp.RHand_RotTracked = trackingInfo.Get_RHand_RotTracked();
        wp.RCont_PosTracked = trackingInfo.Get_RController_PosTracked();
        wp.RCont_RotTracked = trackingInfo.Get_RController_RotTracked();
        wp.LHand_PosTracked = trackingInfo.Get_LHand_PosTracked();
        wp.LHand_RotTracked = trackingInfo.Get_LHand_RotTracked();
        wp.LCont_PosTracked = trackingInfo.Get_LController_PosTracked();
        wp.LCont_RotTracked = trackingInfo.Get_RController_RotTracked();

        currentSession.waypoints.Add(wp);
    }
    void SaveToFile()
    {
        string json = JsonUtility.ToJson(currentSession, true);
        string path = Path.Combine(Application.persistentDataPath, $"MultiTraj_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(path, json);
        Debug.Log($"File saved at : {path}");
    }

}

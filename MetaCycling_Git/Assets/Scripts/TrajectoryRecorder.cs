using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
public class TrajectoryRecorder : MonoBehaviour
{
    public float recordInterval = 0.05f;
    private bool isRecording = false;
    private TrajectorySession currentSession;
    private float timer;
    private float startTime;
    public Material material;
    ControlMap controlMap;
    void Awake()
    {
        controlMap = new ControlMap();
    }
    void OnEnable()
    {
        controlMap.Prototype.Enable();
        controlMap.Prototype.RecordButton.started += ctx => ToggleRecording();
    }

    void Update()
    {
        if (isRecording)
        {
            timer += Time.deltaTime;
            if (timer >= recordInterval)
            {
                RecordFrame();
                timer = 0;
            }
        }
    }

    public void ToggleRecording()
    {
        isRecording = !isRecording;
        if (isRecording)
        {
            StartNewSession();
            Debug.Log("開始錄製四軌跡...");
            material.color = Color.red;
        }
        else
        {
            SaveToFile();
            Debug.Log("錄製結束。");
            material.color = Color.white;
        }
    }

    void StartNewSession()
    {
        currentSession = new TrajectorySession();
        startTime = Time.time;
        RecordFrame();
    }

    void RecordFrame()
    {
        float timeSinceStart = Time.time - startTime;
        MultiTrackWaypoint wp = new MultiTrackWaypoint();
        wp.timestamp = timeSinceStart;

        // --- 1. 獲取左控制器數據 (LTouch) ---
        wp.posL_Cont = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        wp.rotL_Cont = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);

        // --- 2. 獲取右控制器數據 (RTouch) ---
        wp.posR_Cont = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        wp.rotR_Cont = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);

        // --- 3. 獲取左手數據 (LHand) ---
        wp.posL_Hand = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LHand);
        wp.rotL_Hand = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LHand);

        // --- 4. 獲取右手數據 (RHand) ---
        wp.posR_Hand = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RHand);
        wp.rotR_Hand = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RHand);

        currentSession.waypoints.Add(wp);
    }

    void SaveToFile()
    {
        string json = JsonUtility.ToJson(currentSession, true);
        string path = Path.Combine(Application.persistentDataPath, $"MultiTraj_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
        File.WriteAllText(path, json);
        Debug.Log($"軌跡已儲存至: {path}");
    }

}

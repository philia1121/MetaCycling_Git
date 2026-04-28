using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.InputSystem;
using System.Net;
using Firebase.Firestore;
using System.Threading.Tasks;
using Firebase.Extensions;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
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

    //added for making this into singleton
    public static TrajectoryRecorder instance;
    string filePrefix = "MultiTraj";

    private FirebaseFirestore db;

    void Awake()
    {
        if (instance == null)
            instance = this;

        controlMap = new ControlMap();
        VRMainCam = Camera.main.transform;
        trackingInfo = FindFirstObjectByType<TrackingInfo>();
        if (!trackingInfo) trackingInfo = gameObject.AddComponent<TrackingInfo>();
         
        
        // Initialize Firebase Firestore
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                Debug.Log("Firebase Firestore 已成功初始化。");
            }
            else
            {
                Debug.LogError($"無法解析 Firebase 依賴項: {dependencyStatus}");
            }
        });
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

    public void StartRecording()
    {
        if (!isRecording)
        {
            isRecording = !isRecording;
            if (mat) mat.color = Color.red;
            StartNewSession();
            if (cor != null) StopCoroutine(cor);
            cor = StartCoroutine(RecordRoutine());
            Debug.Log("start recording");
        }
    }
    public void StopRecording()
    {
        if (isRecording)
        {
            isRecording = !isRecording;
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
        wp.pos_LCont = new SerializableVector3(OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch));
        wp.rot_LCont = new SerializableQuaternion(OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch));

        // Right Controller
        wp.pos_RCont = new SerializableVector3(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch));
        wp.rot_RCont = new SerializableQuaternion(OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch));
        // Left Hand
        wp.pos_LHand = new SerializableVector3(OVRInput.GetLocalControllerPosition(OVRInput.Controller.LHand));
        wp.rot_LHand = new SerializableQuaternion(OVRInput.GetLocalControllerRotation(OVRInput.Controller.LHand));

        // Right Hand
        wp.pos_RHand = new SerializableVector3(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RHand));
        wp.rot_RHand = new SerializableQuaternion(OVRInput.GetLocalControllerRotation(OVRInput.Controller.RHand));

        // HMD
        wp.pos_HMD = new SerializableVector3(VRMainCam.position);
        wp.rot_HMD = new SerializableQuaternion(VRMainCam.rotation);

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
    public void SaveToFile()
    {
        string json = JsonUtility.ToJson(currentSession, true);
        string name = $"{filePrefix}_{System.DateTime.Now:yyyy_MM_dd_HH_mm_ss_}.json";
        string path = Path.Combine(Application.persistentDataPath, name);
        File.WriteAllText(path, json);
        Debug.Log($"File saved at : {path}");

        UploadSessionToFirestore(currentSession, name);
    }
    void UploadSessionToFirestore<T>(T dataObject, string fileName)
    {
        if (db == null)
        {
            Debug.LogError("Firestore 尚未初始化！無法上傳。");
            return;
        }

        CollectionReference sessionCollection = db.Collection("trajectorySessions");
        DocumentReference docRef = sessionCollection.Document(fileName);

        docRef.SetAsync(dataObject).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"會話數據成功上傳到 Firestore。文檔 ID: {docRef.Id}");
            }
            else if (task.IsFaulted)
            {
                Debug.LogError($"上傳會話數據失敗: {task.Exception}");
            }
        });
    }

    public void SetFilePrefix(string prefix)
    {
        if (prefix == null) return;
        filePrefix = prefix;
    }
    public string GetFilePrefix() { return filePrefix; }
    public void SetMotionType(string motion)
    {
        if (motion == null) return;
        currentSession.motionType = motion;
    }

}

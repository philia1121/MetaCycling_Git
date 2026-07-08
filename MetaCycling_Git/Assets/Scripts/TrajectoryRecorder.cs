using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.Storage;
using Google.MiniJSON;
using Newtonsoft.Json;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class TrajectoryRecorder : MonoBehaviour
{
    [Header("Easy Show")]
    public bool isRecording = false;
    public Material mat;

    [Header("Save Related")]
    public bool uploadToStore = true;
    public bool uploadToStorage = true;
    public float recordInterval = 0.015f;
    string filePrefix = "MC";
    public string savedFolder = "SoA";
    string m_user = "";
    string m_motion = "";

    [Header("Network Streaming Buffers")]
    private List<string> networkArrayBuffer = new List<string>();   //accumulate data and flush
    private float lastNetworkFlushTime;                                     
    private float networkFlushInterval = 0.5f;                      //send data every how many seconds

    // Aos
    private TrajectorySession currentSession;
    private float startTime;

    // SoA
    private StorageRecordData recordData;

    public TrackingInfo trackingInfo;
    
    ControlMap controlMap;
    public Transform VRMainCam;
    Coroutine cor;

    //added for making this into singleton
    public static TrajectoryRecorder instance;

    // Firebase Essential
    private FirebaseFirestore db;
    private FirebaseStorage storage;
    private StorageReference storageRef;

    void Awake()
    {
        if (instance == null)
            instance = this;

        controlMap = new ControlMap();
        if(!VRMainCam) VRMainCam = Camera.main.transform;
        trackingInfo = FindFirstObjectByType<TrackingInfo>();
        if (!trackingInfo) trackingInfo = gameObject.AddComponent<TrackingInfo>();

        InitializeFirestore();
    }
    
    void OnEnable()
    {
        controlMap.Prototype.Enable();
        controlMap.Prototype.Record.started += ctx => ToggleRecording();
    }

    public void ToggleRecording()
    {
        isRecording = !isRecording;
        if (isRecording)
        {
            if (mat) mat.color = Color.red;

            startTime = Time.time;
            StartNewSession();
            StartNewRecordData();
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

            startTime = Time.time;
            StartNewSession();
            StartNewRecordData();
            if (cor != null) StopCoroutine(cor);
            cor = StartCoroutine(RecordRoutine());
            Debug.Log("start recording");

            // Empty caching states
            networkArrayBuffer.Clear();
            lastNetworkFlushTime = Time.time;

            string filename = $"{filePrefix}_{recordData.recordTime}";
            NetworkRecordingManager.Instance.RequestStartRecord(filename);

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

            // Extract what leftover points remain unpushed inside our cache array list
            string[] finalRemainingChunk = networkArrayBuffer.ToArray();
            networkArrayBuffer.Clear();

            string filename = $"{filePrefix}_{recordData.recordTime}";

            // Send the clean array block over along with the intended file name string
            NetworkRecordingManager.Instance.RequestFinalizeStream(finalRemainingChunk, filename);
            NetworkRecordingManager.Instance.RequestEndRecord();

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
        currentSession.userName = m_user;
        currentSession.motionType = m_motion;
    }
    void StartNewRecordData()
    {
        recordData = new StorageRecordData();
        recordData.userName = m_user;
        recordData.motionType = m_motion;
        recordData.recordTime = $"{System.DateTime.Now:yyyy_MM_dd_HH_mm_ss_}";
        recordData.sampleInterval = Math.Round(recordInterval, 4);

        NetworkRecordingManager.Instance.RequestStreamInit(
                    recordData.userName,
                    recordData.motionType,
                    (float)recordData.sampleInterval,
                    recordData.recordTime
                );
    }
    void OnRecord()
    {
        UpdateNewWayPoint();
        UpdateNewRecordData();

        // Convert newest waypoint to JSON fragment
        if (currentSession.waypoints.Count > 0)
        {
            var latestWaypoint = currentSession.waypoints[currentSession.waypoints.Count - 1];
            string flatWpJson = JsonConvert.SerializeObject(latestWaypoint);
            networkArrayBuffer.Add(flatWpJson);
        }

        // check .5 buffer
        if (Time.time - lastNetworkFlushTime >= networkFlushInterval)
        {
            if (networkArrayBuffer.Count > 0)
            {
                // change to network array buffer
                string[] chunkToSend = networkArrayBuffer.ToArray();
                networkArrayBuffer.Clear();

                NetworkRecordingManager.Instance.RequestStreamChunk(chunkToSend);
            }
            lastNetworkFlushTime = Time.time;
        }
    }
    void UpdateNewWayPoint()
    {
        // Time Stamp
        double timeSinceStart = Math.Round(Time.time - startTime,3);
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
        Debug.Log("Add Waypoint");
    }
    void UpdateNewRecordData()
    {
        // Time Stamp
        double timeSinceStart = Math.Round(Time.time - startTime, 3);
        recordData.timeStamp.Add(timeSinceStart);

        recordData.pHMD.Add(new SerializableVector3(VRMainCam.position));
        recordData.rHMD.Add(new SerializableQuaternion(VRMainCam.rotation));
        recordData.pLC.Add(new SerializableVector3(OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch)));
        recordData.rLC.Add(new SerializableQuaternion(OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch)));
        recordData.pRC.Add(new SerializableVector3(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch)));
        recordData.rRC.Add(new SerializableQuaternion(OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch)));
        recordData.pLH.Add(new SerializableVector3(OVRInput.GetLocalControllerPosition(OVRInput.Controller.LHand)));
        recordData.rLH.Add(new SerializableQuaternion(OVRInput.GetLocalControllerRotation(OVRInput.Controller.LHand)));
        recordData.pRH.Add(new SerializableVector3(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RHand)));
        recordData.rRH.Add(new SerializableQuaternion(OVRInput.GetLocalControllerRotation(OVRInput.Controller.RHand)));

        recordData.ptRH.Add(trackingInfo.Get_RHand_PosTracked());
        recordData.rtRH.Add(trackingInfo.Get_RHand_RotTracked());
        recordData.ptRC.Add(trackingInfo.Get_RController_PosTracked());
        recordData.rtRC.Add(trackingInfo.Get_RController_RotTracked());
        recordData.ptLH.Add(trackingInfo.Get_LHand_PosTracked());
        recordData.rtLH.Add(trackingInfo.Get_LHand_RotTracked());
        recordData.ptLC.Add(trackingInfo.Get_LController_PosTracked());
        recordData.rtLC.Add(trackingInfo.Get_RController_RotTracked());

        Debug.Log("Add record Data");
    }
    public void SaveToFile()
    {
        string name = $"{filePrefix}_{System.DateTime.Now:yyyy_MM_dd_HH_mm_ss_}.json";

        //Save TrajectorySession
        string ts = JsonConvert.SerializeObject(currentSession, Formatting.Indented);
        string ts_path = Path.Combine(Application.persistentDataPath, name);
        File.WriteAllText(ts_path, ts);
        Debug.Log($"AoS File saved at : {ts_path}");

        //Save FireRecordData
        string frd = JsonConvert.SerializeObject(recordData, Formatting.Indented);
        string frd_path = Path.Combine(Application.persistentDataPath, savedFolder, name);
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, savedFolder)); // Create the sub folder in case they're not exit yet
        File.WriteAllText(frd_path, frd);
        Debug.Log($"SoA File save at : {frd_path}");

        // if (uploadToStore) UploadContentToFirestore(recordData, name);
        if (uploadToStorage) UploadContentToStorage(frd, $"MC/{name}");
    }

    #region Firebase Related
    void InitializeFirestore()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                storage = FirebaseStorage.DefaultInstance;
                storageRef = storageRef = storage.GetReferenceFromUrl("gs://metacycling-d034c.firebasestorage.app");
                Debug.Log("Firebase Firestore 已成功初始化。");
            }
            else
            {
                Debug.LogError($"無法解析 Firebase 依賴項: {dependencyStatus}");
            }
        });
    }
    void UploadContentToFirestore<T>(T dataObject, string fileName)
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
    public async void UploadContentToStorage(string str_data, string pathInStorage)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str_data);

        StorageReference fileRef = storageRef.Child(pathInStorage);
        try
        {
            StorageMetadata metadata = await fileRef.PutBytesAsync(bytes);
            Debug.Log($"上傳成功！檔案名稱: {metadata.Name}, 大小: {metadata.SizeBytes}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"上傳失敗: {e.Message}");
        }
    }
    #endregion

    public void SetFilePrefix(string prefix)
    {
        if (prefix == null) return;
        filePrefix = prefix;
    }
    public string GetFilePrefix() { return filePrefix; }
    public void SetMotionType(string motion)
    {
        if (motion == null) return;
        m_motion = motion;
        if (currentSession != null) currentSession.motionType = motion;
        if (recordData != null) recordData.motionType = motion;
    }
    public void SetUserName(string name)
    {
        if (name == null) return;
        m_user = name;
        if (currentSession != null) currentSession.userName= name;
        if (recordData != null) recordData.userName = name;
        SetFilePrefix(name);
    }

}

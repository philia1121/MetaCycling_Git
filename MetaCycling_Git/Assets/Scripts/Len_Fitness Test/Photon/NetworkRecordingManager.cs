using Oculus.Interaction;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

public class NetworkRecordingManager : MonoBehaviourPunCallbacks
{
    [Header("Create Room Panel Items")]
    [SerializeField] private GameObject createRoomPanelObj;
    [SerializeField] private TMP_InputField createRoominput;
    [SerializeField] private Button createRoomBtn;

    [Header("Webcam Select Items")]
    [SerializeField] private GameObject webcamSelectPanelObj;
    [SerializeField] private TMP_Dropdown ddlWebcam;
    [SerializeField] private Button refreshWebcamListBtn;
    [SerializeField] private Button closeRoomBtn;
    [SerializeField] private GameObject alertPanel;
    [SerializeField] private TMP_Text clientInfo;

    [Header("Join Room Panel Items")]
    [SerializeField] private GameObject joinRoomPanelObj;
    [SerializeField] private TMP_InputField joinRoominput;
    //[SerializeField] private Button joinRoomBtn;

    [Header("Request Record Panel Items")]
    [SerializeField] private GameObject requestRecordingPanel;
    //[SerializeField] private Button requestRecordBtn;
    //[SerializeField] private Button leaveRoomButton;
    [SerializeField] private TMP_Text requestRecordBtnText;
    private bool isCurrentlyRecordingVideo = false;              //flag to toggle the recording thing

    [Header("Extra Details")]
    [SerializeField] private GameObject[] xrRelatedItems;
    [SerializeField] private Camera overlayCamera;
    [SerializeField] private TMP_Text infoText;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject playerFab;
    [SerializeField] private GameObject observer;

    public static NetworkRecordingManager Instance;

    public Action<bool> OnNetworkConnected;
    public Action<bool> OnRoomJoined;

    private bool isPlatformSetup = false;
    private WebCamTexture activeWebCamTexture;
    private string webcamName;

    private GameObject spawnedLocalVRPlayer;

    //to help save the data to write in PC
    private StorageRecordData compiledSessionRecordData = new StorageRecordData(); private string currentSessionUser = "";
    private string currentSessionMotion = "";
    private string currentSessionTime = "";
    private float currentSessionInterval = 0.015f;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
        overlayCamera.gameObject.SetActive(false);

        StartCoroutine(InitializeDelayedActionCalls());
    }

    private IEnumerator InitializeDelayedActionCalls()
    {
        yield return new WaitForEndOfFrame();

        OnNetworkConnected?.Invoke(false);
        OnRoomJoined?.Invoke(false);

        Debug.Log("Network state machine baseline initialized to false.");
    }

    #region UI Setup

    private void SetupPlatform()
    {
        if (isPlatformSetup)
            return;

        if (Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            SetupUI(createRoomPanelObj.name);
        }
        else
        {
            SetupUI(joinRoomPanelObj.name);
        }

        //add flag to check initially when connected
        isPlatformSetup = true;
    }

    private void SetupUI(string _name)
    {
        createRoomPanelObj.SetActive(createRoomPanelObj.name == _name);
        if (createRoomPanelObj.activeSelf)
        {
            //make the button can create and join room
            createRoomBtn.onClick.RemoveAllListeners();
            createRoomBtn.onClick.AddListener(() => {
                CreateRoomBtnClick(createRoominput);
            });

            //disable XR items
            foreach (GameObject g in xrRelatedItems)
                g.SetActive(false);
            overlayCamera.gameObject.SetActive(true);

        }

        joinRoomPanelObj.SetActive(joinRoomPanelObj.name == _name);
        if (joinRoomPanelObj.activeSelf)
        {
            //join room based on string
            //joinRoomBtn.onClick.RemoveAllListeners();
            //joinRoomBtn.onClick.AddListener(() => {
            //    JoinRoomBtnClick(joinRoominput);
            //});
        }

        webcamSelectPanelObj.SetActive(webcamSelectPanelObj.name == _name);
        if (webcamSelectPanelObj.activeSelf)
        {
            List<string> deviceNames = GetWebcamDeviceList();

            //setup dropdownlist
            ddlWebcam.ClearOptions();
            ddlWebcam.AddOptions(deviceNames);

            //setup so in case a new webcam device is plugged in 
            refreshWebcamListBtn.onClick.RemoveAllListeners();
            refreshWebcamListBtn.onClick.AddListener(() => {
                GetWebcamDeviceList();
                deviceNames = GetWebcamDeviceList();

                ddlWebcam.ClearOptions();
                ddlWebcam.AddOptions(deviceNames);

                infoText.text = "Webcam hardware device list updated!";

                if (deviceNames.Count > 0)
                {
                    OnPreviewWebcamClick(deviceNames[ddlWebcam.value]);
                }
            });

            closeRoomBtn.onClick.RemoveAllListeners();
            closeRoomBtn.onClick.AddListener(CloseRoomBtnClick);

            //as long as there is a webcam related item
            if (deviceNames.Count > 0)
            {
                // Start displaying the 1st webcam right away
                OnPreviewWebcamClick(deviceNames[0]);

                ddlWebcam.onValueChanged.RemoveAllListeners();
                ddlWebcam.onValueChanged.AddListener(delegate
                {
                    string selectedCamName = ddlWebcam.options[ddlWebcam.value].text;
                    OnPreviewWebcamClick(selectedCamName);
                    ddlWebcam.Hide();
                });
            }
            else
            {
                infoText.text = "<color=\"red\">No video capture devices found.</color>";
            }
        }

        requestRecordingPanel.SetActive(requestRecordingPanel.name == _name);
        if (requestRecordingPanel.activeSelf)
        {
            // Set up the click handler to toggle recording safely
            //requestRecordBtn.onClick.RemoveAllListeners();
            //requestRecordBtn.onClick.AddListener(OnRequestRecordClicked);

            //leaveRoomButton.onClick.RemoveAllListeners();
            //leaveRoomButton.onClick.AddListener(() => {
            //    RequestLeaveRoom();            
            //});
        }
    }

    public void RequestLeaveRoom()
    {
        if (PhotonNetwork.InRoom) 
            PhotonNetwork.LeaveRoom();

    }

    #endregion

    #region webcam setup
    private List<string> GetWebcamDeviceList()
    {
        //makeup device name list
        WebCamDevice[] devices = WebCamTexture.devices;
        List<string> deviceNames = new List<string>();

        foreach (var device in devices)
        {
            deviceNames.Add(device.name.ToString());
        }

        return deviceNames;
    }

    private void OnPreviewWebcamClick(string _name)
    {
        if (ddlWebcam.options.Count == 0) return;

        StopHardwareCamera();
        webcamName = _name;
        infoText.text = $"Previewing webcam: {webcamName}";

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (WebcamToMP4.instance != null)
        {
            WebcamToMP4.instance.PreviewWebcam(webcamName);
        }
#endif
    }

    private void StopHardwareCamera()
    {
        if (activeWebCamTexture != null)
        {
            activeWebCamTexture.Stop();
            activeWebCamTexture = null;
        }
    }
    #endregion

    #region photon or networking related
    public void CreateRoomBtnClick(TMP_InputField _inp)
    {
        string rname = _inp.text;
        if (string.IsNullOrEmpty(rname))
        {
            infoText.text = $"<color=\"red\">name invalid !</color>";
            return;
        }
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;

        _inp.text = "";//clear it out

        PhotonNetwork.CreateRoom(rname, roomOptions);
    }

    public void JoinRoomBtnClick(TMP_InputField _inp, Button _btn)
    {
        string rname = _inp.text;

        if (string.IsNullOrEmpty(rname))
        {
            infoText.text = "<color=\"red\">Please enter a room name!</color>";
            return;
        }

        // CRITICAL FIX: If we are already in a room or switching servers, block the click!
        if (PhotonNetwork.InRoom || PhotonNetwork.NetworkClientState == ClientState.Joining)
        {
            Debug.LogWarning("JoinRoom blocked: Client is already joining or inside a room.");
            return;
        }

        // Disable the button instantly so the user can't mash it a second time
        if (_btn != null) _btn.interactable = false;
        infoText.text = "Joining room...";

        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }

        PhotonNetwork.JoinRoom(rname);
    }
    private void CloseRoomBtnClick()
    {
        if (PhotonNetwork.InRoom)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            if (WebcamToMP4.instance != null) WebcamToMP4.instance.StopRecording();
#endif
            PhotonNetwork.LeaveRoom();
        }
    }

    public void RequestStartRecord(string _name)
    {
        if (!PhotonNetwork.InRoom) return;

        if (!isCurrentlyRecordingVideo)
        {
            isCurrentlyRecordingVideo = true;

            photonView.RPC("RPC_StartWebcamRecordingStream", RpcTarget.MasterClient, _name);
        }
    }

    public void RequestEndRecord()
    {
        if (!PhotonNetwork.InRoom) return;

        if (isCurrentlyRecordingVideo)
        {
            isCurrentlyRecordingVideo = false;

            photonView.RPC("RPC_StopWebcamRecordingStream", RpcTarget.MasterClient);
        }
    }

    public void OnRequestRecordClicked(string _name)
    {
        if (!PhotonNetwork.InRoom) return;

        if (!isCurrentlyRecordingVideo)
        {
            isCurrentlyRecordingVideo = true;

            photonView.RPC("RPC_StartWebcamRecordingStream", RpcTarget.MasterClient, _name);
        }
        else
        {
            isCurrentlyRecordingVideo = false;

            photonView.RPC("RPC_StopWebcamRecordingStream", RpcTarget.MasterClient);
        }
    }

    public void RequestStreamInit(string userName, string motionType, float sampleInterval, string recordTime)
    {
        if (!PhotonNetwork.InRoom) return;

        photonView.RPC("RPC_InitializeDataStreamHeader",
                       RpcTarget.MasterClient,
                       userName,
                       motionType,
                       sampleInterval,
                       recordTime);
    }

    public void RequestStreamChunk(string[] serializedWaypoints)
    {
        if (!PhotonNetwork.InRoom || serializedWaypoints == null || serializedWaypoints.Length == 0) return;

        photonView.RPC("RPC_ReceiveDataChunk", RpcTarget.MasterClient, (object)serializedWaypoints);
    }

    public void RequestFinalizeStream(string[] finalWaypoints, string targetFileName)
    {
        if (!PhotonNetwork.InRoom) return;

        photonView.RPC("RPC_FinalizeDataStream", RpcTarget.MasterClient, (object)finalWaypoints, targetFileName);
    }

    #endregion

    #region Photon Callbacks
    public override void OnConnected()
    {
        infoText.text = "Connecting...";
    }

    public override void OnConnectedToMaster()
    {
        infoText.text = $"Connected !";
        SetupPlatform();
        OnNetworkConnected?.Invoke(true);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        OnNetworkConnected?.Invoke(false);
        OnRoomJoined?.Invoke(false);
    }

    public override void OnCreatedRoom()
    {
        infoText.text = $"<color=\"green\">room {PhotonNetwork.CurrentRoom.Name} created !</color>";
        SetupUI(webcamSelectPanelObj.name);
    }
    public override void OnJoinedRoom()
    {
        Debug.Log($" joined to {PhotonNetwork.CurrentRoom.Name}");

        if (!PhotonNetwork.IsMasterClient)
        {
            infoText.text = $"Joined Room: <color=\"green\">{PhotonNetwork.CurrentRoom.Name}</color>";
            if (requestRecordBtnText != null) requestRecordBtnText.text = "START RECORDING";
            //if (joinRoomBtn != null) joinRoomBtn.interactable = true; // Reset state tracking

            SetupUI(requestRecordingPanel.name);

            if (spawnedLocalVRPlayer == null)
            {
                observer.SetActive(false);

                spawnedLocalVRPlayer = PhotonNetwork.Instantiate(playerFab.name, Vector3.zero, Quaternion.identity);
            }
        }
        OnRoomJoined?.Invoke(true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"Join room failed. Code: {returnCode} Message: {message}");

        // room doesnt exist, say room unfound
        //if (joinRoomBtn != null) joinRoomBtn.interactable = true;

        if (returnCode == ErrorCode.GameDoesNotExist)
        {
            // Clear out the input text field so the user can re-type
            if (joinRoominput != null)
                joinRoominput.text = "";

            // Alert the user via your infoText mesh layout
            infoText.text = "<color=\"red\">Room not found.</color>";
        }
        else if (returnCode == ErrorCode.GameFull)
        {
            if (joinRoominput != null)
                joinRoominput.text = "";

            infoText.text = "<color=\"red\">Room is full!</color>";
        }
        OnRoomJoined?.Invoke(false);
    }

    //these 2 only fires on the one that creates the room
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        infoText.text = $"VR Headset connected";
        alertPanel.SetActive(false);

    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        infoText.text = $"VR Headset left or disconnected.";

        if (otherPlayer.IsMasterClient)
        {
            Debug.LogWarning("PC Master Client left the session. Forcing VR Client shutdown loop...");
            infoText.text = "<color=\"red\">Room closed by host. Disconnecting...</color>";

            // Explicitly force the VR client to leave the defunct room session
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            infoText.text = $"Remote user left or disconnected.";
            alertPanel.SetActive(true);
        }
    }

    public override void OnLeftRoom()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (WebcamToMP4.instance != null) WebcamToMP4.instance.StopRecording();
#endif
        StopHardwareCamera();

        // Reset the layout platform loop cleanly back to the home screens
        isPlatformSetup = false;
        SetupPlatform();

        infoText.text = "<color=\"red\">Disconnected: Room was closed.</color>";
        OnRoomJoined?.Invoke(false);

    }
    #endregion

    #region RPC
    [PunRPC]
    public void RPC_StartWebcamRecordingStream(string mp4OutputFileName)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        infoText.text = $"<color=\"red\">VR STARTED RECORDING LIVE!</color>\nFile output: {mp4OutputFileName}.mp4";

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (WebcamToMP4.instance != null)
        {
            WebcamToMP4.instance.StartWebcamAndRecording(this.webcamName, mp4OutputFileName);

            ddlWebcam.interactable = false;
            refreshWebcamListBtn.interactable = false;
            closeRoomBtn.interactable = false;
        }
#endif
    }

    [PunRPC]
    public void RPC_StopWebcamRecordingStream()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        infoText.text = "<color=\"green\">Recording saved successfully to PC Storage!</color>";
        if (clientInfo != null)
        {
            clientInfo.text = $"";
        }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (WebcamToMP4.instance != null)
        {
            WebcamToMP4.instance.StopRecording();

            ddlWebcam.interactable = true;
            refreshWebcamListBtn.interactable = true;
            closeRoomBtn.interactable = true;
        }
#endif

        SetupUI(webcamSelectPanelObj.name);
    }

    [PunRPC]
    public void RPC_InitializeDataStreamHeader(string userName, string motionType, float sampleInterval, string recordTime)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Reset memory containers for the new recording iteration
        compiledSessionRecordData = new StorageRecordData();

        compiledSessionRecordData.timeStamp = new List<double>();
        compiledSessionRecordData.pHMD = new List<SerializableVector3>();
        compiledSessionRecordData.rHMD = new List<SerializableQuaternion>();
        compiledSessionRecordData.pLC = new List<SerializableVector3>();
        compiledSessionRecordData.rLC = new List<SerializableQuaternion>();
        compiledSessionRecordData.pRC = new List<SerializableVector3>();
        compiledSessionRecordData.rRC = new List<SerializableQuaternion>();
        compiledSessionRecordData.pLH = new List<SerializableVector3>();
        compiledSessionRecordData.rLH = new List<SerializableQuaternion>();
        compiledSessionRecordData.pRH = new List<SerializableVector3>();
        compiledSessionRecordData.rRH = new List<SerializableQuaternion>();

        compiledSessionRecordData.ptRH = new List<bool>();
        compiledSessionRecordData.rtRH = new List<bool>();
        compiledSessionRecordData.ptRC = new List<bool>();
        compiledSessionRecordData.rtRC = new List<bool>();
        compiledSessionRecordData.ptLH = new List<bool>();
        compiledSessionRecordData.rtLH = new List<bool>();
        compiledSessionRecordData.ptLC = new List<bool>();
        compiledSessionRecordData.rtLC = new List<bool>();

        // Set top-level metadata values
        compiledSessionRecordData.userName = userName;
        compiledSessionRecordData.motionType = motionType;
        compiledSessionRecordData.recordTime = recordTime;
        compiledSessionRecordData.sampleInterval = System.Math.Round(sampleInterval, 4);

        if (clientInfo != null)
        {
            clientInfo.text = $"<Username: {userName}" +
                $"\nMotion Type: {motionType}" +
                $"\nRecord Time: {recordTime}";
        }
    }

    [PunRPC]
    public void RPC_ReceiveDataChunk(string[] waypointsChunk)
    {
        if (!PhotonNetwork.IsMasterClient || waypointsChunk == null) return;

        // Unpack the incoming serialized lines and store them directly as structural data in memory
        foreach (string serializedWp in waypointsChunk)
        {
            var wp = Newtonsoft.Json.JsonConvert.DeserializeObject<MultiTrackWaypoint>(serializedWp);
            if (wp != null) AppendWaypointToRecordContainer(wp);
        }

        if (infoText != null)
        {
            infoText.text = $"<color=\"yellow\">Streaming Tracking Logs...</color>\nBuffered waypoints: {compiledSessionRecordData.timeStamp.Count}";
        }
    }

    [PunRPC]
    public void RPC_FinalizeDataStream(string[] finalWaypoints, string targetFileName)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Add any remaining elements from the final data flush push
        if (finalWaypoints != null)
        {
            foreach (string serializedWp in finalWaypoints)
            {
                var wp = Newtonsoft.Json.JsonConvert.DeserializeObject<MultiTrackWaypoint>(serializedWp);
                if (wp != null)
                {
                    AppendWaypointToRecordContainer(wp);
                }
            }
        }

        // Serialize the container using your format (Indented formatting matches the template)
        string alignedJsonOutput = Newtonsoft.Json.JsonConvert.SerializeObject(compiledSessionRecordData, Newtonsoft.Json.Formatting.Indented);

        // Save the file cleanly to PC storage
        SaveStreamedContentToDisk(targetFileName, alignedJsonOutput);

        // Clear the data container from memory
        compiledSessionRecordData = null;
    }

    #endregion

    #region Write To Disk
    private void AppendWaypointToRecordContainer(MultiTrackWaypoint wp)
    {
        // Deconstruct the multi-track waypoint to fill the target data columns
        compiledSessionRecordData.timeStamp.Add(wp.timestamp);
        compiledSessionRecordData.pHMD.Add(wp.pos_HMD);
        compiledSessionRecordData.rHMD.Add(wp.rot_HMD);

        compiledSessionRecordData.pLC.Add(wp.pos_LCont);
        compiledSessionRecordData.rLC.Add(wp.rot_LCont);
        compiledSessionRecordData.pRC.Add(wp.pos_RCont);
        compiledSessionRecordData.rRC.Add(wp.rot_RCont);

        compiledSessionRecordData.pLH.Add(wp.pos_LHand);
        compiledSessionRecordData.rLH.Add(wp.rot_LHand);
        compiledSessionRecordData.pRH.Add(wp.pos_RHand);
        compiledSessionRecordData.rRH.Add(wp.rot_RHand);

        compiledSessionRecordData.ptRH.Add(wp.RHand_PosTracked);
        compiledSessionRecordData.rtRH.Add(wp.RHand_RotTracked);
        compiledSessionRecordData.ptRC.Add(wp.RCont_PosTracked);
        compiledSessionRecordData.rtRC.Add(wp.RCont_RotTracked);
        compiledSessionRecordData.ptLH.Add(wp.LHand_PosTracked);
        compiledSessionRecordData.rtLH.Add(wp.LHand_RotTracked);
        compiledSessionRecordData.ptLC.Add(wp.LCont_PosTracked);
        compiledSessionRecordData.rtLC.Add(wp.LCont_RotTracked);
    }

    private void SaveStreamedContentToDisk(string fileName, string fullFileContent)
    {
        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, "StreamedFiles");
            Directory.CreateDirectory(folderPath);

            string fullPath = Path.Combine(folderPath, $"{fileName}.json");
            File.WriteAllText(fullPath, fullFileContent);

            Debug.Log($"[PC Save Success] Clean JSON compiled and saved at: {fullPath}");
            if (infoText != null)
            {
                infoText.text = $"<color=\"green\">File compiled & saved to PC!</color>\nName: {fileName}.json";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to commit compiled stream file payload down to PC disk: {ex.Message}");
        }
    }

    #endregion
}

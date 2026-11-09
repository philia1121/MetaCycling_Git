using extOSC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// OSC version of the Photon NetworkRecordingManager.
///
/// Host  (Windows / macOS) : opens a "room", broadcasts its name on the LAN, mirrors the VR pose,
///                           records the webcam, compiles streamed tracking data and writes JSON.
/// Client (Quest / Android): finds the room by name (or manual IP), streams pose + tracking data.
///
/// Messages that must not be lost (record start/stop, data header/chunks/final, force-stop) go through
/// a small reliable layer (sequence numbers + ack + resend + in-order delivery). Pose is fire-and-forget.
/// </summary>
public class OSC_NetworkRecordingManager : MonoBehaviour
{
    [Header("OSC")]
    [SerializeField] private OSCTransmitter transmitter;   // Auto Connect OFF
    [SerializeField] private OSCReceiver receiver;         // Auto Connect OFF
    [SerializeField] private int hostPort = 7000;          // host listens here
    [SerializeField] private int clientPort = 7001;        // client listens here
    [SerializeField] private int discoveryPort = 7002;     // room-name broadcast
    [SerializeField] private TMP_InputField manualHostIpInput; // optional fallback if broadcast is blocked

    [Header("Timing")]
    [SerializeField] private float heartbeatInterval = 1f;
    [SerializeField] private float connectionTimeout = 5f;
    [SerializeField] private float reconnectRetryDelay = .5f;
    [SerializeField] private float reconnectGiveUpSeconds = 30f;
    [SerializeField] private float discoveryTimeout = 5f;
    [SerializeField] private float joinTimeout = 4f;
    [SerializeField] private float resendInterval = .25f;
    [SerializeField] private int maxInFlight = 64;
    [SerializeField] private int maxPacketBytes = 1200;

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
    [Header("Webcam Extra Item")]
    [SerializeField] private RectTransform PopupPanel;
    [SerializeField] private Button PopupOutBtn;
    [SerializeField] private Button PopupInBtn;
    [SerializeField] private float PopupOutXPos;
    [SerializeField] private float PopupInXPos;

    [Header("Join Room Panel Items")]
    [SerializeField] private GameObject joinRoomPanelObj;
    [SerializeField] private TMP_InputField joinRoominput;

    [Header("Request Record Panel Items")]
    [SerializeField] private GameObject requestRecordingPanel;
    [SerializeField] private TMP_Text requestRecordBtnText;
    private bool isCurrentlyRecordingVideo = false;

    [Header("Extra Details")]
    [SerializeField] private GameObject[] xrRelatedItems;
    [SerializeField] private Camera overlayCamera;
    [SerializeField] private TMP_Text infoText;

    [Header("Scene objects")]
    [SerializeField] private GameObject observer;           // disabled on the VR client once joined

    [Header("Live Pose Sync (replaces the Photon player prefab)")]
    [Tooltip("VR side: e.g. HMD, left controller, right controller, left hand, right hand (max 8)")]
    [SerializeField] private Transform[] clientPoseSources;
    [Tooltip("PC side: replica transforms, SAME ORDER as the sources")]
    [SerializeField] private Transform[] hostPoseReplicas;
    [SerializeField] private GameObject remoteAvatarRoot;   // PC side, shown while a headset is connected
    [SerializeField] private float poseSendRate = 30f;

    public static OSC_NetworkRecordingManager Instance;

    public Action<bool> OnNetworkConnected;
    public Action<bool> OnRoomJoined;

    // ------------------------------------------------------------------ constants
    private const string A_Join = "/join";
    private const string A_Joined = "/joined";
    private const string A_JoinFail = "/joinfail";
    private const string A_Ping = "/ping";
    private const string A_Pong = "/pong";
    private const string A_Leave = "/leave";
    private const string A_CloseRoom = "/closeRoom";
    private const string A_Pose = "/pose";
    private const string A_Ack = "/ack";

    // reliable (first value is always the sequence number)
    private const string R_StartRec = "/rec/start";
    private const string R_StopRec = "/rec/stop";
    private const string R_DataInit = "/data/init";
    private const string R_DataChunk = "/data/chunk";
    private const string R_DataFinal = "/data/final";
    private const string R_ForceStop = "/cmd/forceStop";

    // ------------------------------------------------------------------ state
    private static bool IsHost =>
        Application.platform == RuntimePlatform.WindowsPlayer ||
        Application.platform == RuntimePlatform.WindowsEditor ||
        Application.platform == RuntimePlatform.OSXEditor ||
        Application.platform == RuntimePlatform.OSXPlayer ||
        Application.platform == RuntimePlatform.LinuxEditor ||
        Application.platform == RuntimePlatform.LinuxPlayer;

    private enum JoinState { Idle, Waiting, Joined, Full }

    private bool isPlatformSetup;
    private bool socketsReady;
    private WebCamTexture activeWebCamTexture;
    private string webcamName;
    private Button activeJoinButtonRef;

    private string roomName = "";
    private bool inRoom;            // host: room is open   | client: joined
    private bool peerConnected;     // host only: a headset is currently connected
    private bool reconnecting;      // client only
    private string sessionId = "";  // identifies one client session (survives reconnects)
    private string clientIp;        // host only
    private string hostIp;          // client only
    private int hostPortInUse;      // client only
    private JoinState joinState = JoinState.Idle;
    private float lastPeerSeen;
    private float nextPingTime;
    private float nextPoseTime;

    private Coroutine broadcastRoutine, joinRoutine, reconnectRoutine;

    private StorageRecordData compiledSessionRecordData = new StorageRecordData();

    // reliable layer
    private class OutMsg { public int seq; public OSCMessage msg; public float lastSent; }
    private class InMsg { public string address; public List<OSCValue> values; }
    private int nextSendSeq;
    private int nextExpectedSeq;
    private readonly Dictionary<int, OutMsg> pending = new Dictionary<int, OutMsg>();
    private readonly Queue<OutMsg> outbox = new Queue<OutMsg>();
    private readonly Dictionary<int, InMsg> inBuffer = new Dictionary<int, InMsg>();

    // ------------------------------------------------------------------ unity
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        overlayCamera.gameObject.SetActive(false);
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        yield return new WaitForEndOfFrame();

        OnNetworkConnected?.Invoke(false);
        OnRoomJoined?.Invoke(false);

        BindAddresses();
        StartSockets();

        infoText.text = "Connected !";
        ReturnToHome();
    }

    private void Update()
    {
        if (!inRoom) return;
        float now = Time.unscaledTime;

        if (IsHost)
        {
            if (peerConnected && now - lastPeerSeen > connectionTimeout)
                PeerLost();

            if (peerConnected && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.LogWarning("[PC Host] Spacebar pressed. Requesting VR stream termination...");
                SendReliable(R_ForceStop);
                HostStopWebcamRecording();
            }
        }
        else if (!reconnecting)
        {
            if (now >= nextPingTime)
            {
                nextPingTime = now + heartbeatInterval;
                transmitter.Send(Build(A_Ping, OSCValue.String(sessionId)));
            }

            if (now - lastPeerSeen > connectionTimeout)
            {
                reconnectRoutine = StartCoroutine(ReconnectRoutine());
            }
            else if (clientPoseSources != null && clientPoseSources.Length > 0 && now >= nextPoseTime)
            {
                nextPoseTime = now + 1f / poseSendRate;
                SendPose();
            }
        }

        PumpReliable();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused || !socketsReady) return;

        // headset woke up: sockets are often dead after sleep
        Debug.LogWarning("[Resume] Re-opening OSC sockets");
        receiver.Close(); receiver.Connect();
        transmitter.Close(); transmitter.Connect();
        lastPeerSeen = Time.unscaledTime;
    }

    // ------------------------------------------------------------------ sockets / binding
    private void StartSockets()
    {
        receiver.Close();
        receiver.LocalPort = IsHost ? hostPort : clientPort;
        receiver.Connect();

        transmitter.Close();
        transmitter.RemotePort = IsHost ? clientPort : hostPort;
        transmitter.RemoteHost = "127.0.0.1";
        transmitter.Connect();

        socketsReady = true;
    }

    private void BindAddresses()
    {
        receiver.Bind(A_Join, OnJoin);
        receiver.Bind(A_Joined, OnJoined);
        receiver.Bind(A_JoinFail, OnJoinFail);
        receiver.Bind(A_Ping, OnPing);
        receiver.Bind(A_Pong, OnPong);
        receiver.Bind(A_Leave, OnLeave);
        receiver.Bind(A_CloseRoom, OnCloseRoom);
        receiver.Bind(A_Pose, OnPose);
        receiver.Bind(A_Ack, OnAck);

        foreach (string a in new[] { R_StartRec, R_StopRec, R_DataInit, R_DataChunk, R_DataFinal, R_ForceStop })
            receiver.Bind(a, OnReliable);
    }

    private void RetargetTransmitter(string ip, int port)
    {
        if (transmitter.RemoteHost == ip && transmitter.RemotePort == port) return;
        transmitter.Close();
        transmitter.RemoteHost = ip;
        transmitter.RemotePort = port;
        transmitter.Connect();
    }

    private static OSCMessage Build(string address, params OSCValue[] values)
    {
        var m = new OSCMessage(address);
        foreach (var v in values) m.AddValue(v);
        return m;
    }

    // ------------------------------------------------------------------ reliable layer
    private bool LinkAlive => inRoom && (IsHost ? peerConnected : !reconnecting);

    private void ResetReliable()
    {
        nextSendSeq = 0;
        nextExpectedSeq = 0;
        pending.Clear();
        outbox.Clear();
        inBuffer.Clear();
    }

    private void SendReliable(string address, params OSCValue[] payload)
    {
        var m = new OSCMessage(address);
        int seq = nextSendSeq++;
        m.AddValue(OSCValue.Int(seq));
        foreach (var v in payload) m.AddValue(v);

        var o = new OutMsg { seq = seq, msg = m, lastSent = Time.unscaledTime };
        if (pending.Count < maxInFlight)
        {
            pending[seq] = o;
            transmitter.Send(m);
        }
        else outbox.Enqueue(o);
    }

    private void PumpReliable()
    {
        if (!LinkAlive || pending.Count == 0) return;
        float now = Time.unscaledTime;
        foreach (var p in pending.Values)
        {
            if (now - p.lastSent > resendInterval)
            {
                p.lastSent = now;
                transmitter.Send(p.msg);
            }
        }
    }

    private void FillWindow()
    {
        while (outbox.Count > 0 && pending.Count < maxInFlight)
        {
            var o = outbox.Dequeue();
            o.lastSent = Time.unscaledTime;
            pending[o.seq] = o;
            transmitter.Send(o.msg);
        }
    }

    private void OnAck(OSCMessage m)
    {
        if (!inRoom || m.Values.Count == 0) return;
        if (IsHost && !FromPairedPeer(m)) return;
        Touch();
        if (pending.Remove(m.Values[0].IntValue)) FillWindow();
    }

    private void OnReliable(OSCMessage m)
    {
        if (!inRoom || m.Values.Count == 0) return;
        if (IsHost && !FromPairedPeer(m)) return;
        Touch();

        int seq = m.Values[0].IntValue;
        transmitter.Send(Build(A_Ack, OSCValue.Int(seq)));   // always ack (acks can be lost too)

        if (seq < nextExpectedSeq) return;                   // duplicate

        var item = new InMsg { address = m.Address, values = m.Values.GetRange(1, m.Values.Count - 1) };
        if (seq > nextExpectedSeq)                           // arrived early, hold until gap is filled
        {
            inBuffer[seq] = item;
            return;
        }

        nextExpectedSeq++;
        DispatchReliable(item);
        while (inBuffer.TryGetValue(nextExpectedSeq, out var next))
        {
            inBuffer.Remove(nextExpectedSeq);
            nextExpectedSeq++;
            DispatchReliable(next);
        }
    }

    private void DispatchReliable(InMsg it)
    {
        var v = it.values;
        switch (it.address)
        {
            case R_StartRec: if (IsHost) HostStartWebcamRecording(v[0].StringValue); break;
            case R_StopRec: if (IsHost) HostStopWebcamRecording(); break;
            case R_DataInit: if (IsHost) HostInitDataHeader(v[0].StringValue, v[1].StringValue, v[2].FloatValue, v[3].StringValue); break;
            case R_DataChunk: if (IsHost) HostReceiveChunk(v, 0); break;
            case R_DataFinal: if (IsHost) HostFinalize(v[0].StringValue, v); break;
            case R_ForceStop: if (!IsHost) ClientForceStop(); break;
        }
    }

    private void Touch() { lastPeerSeen = Time.unscaledTime; }

    // host only: true if the packet came from the headset that owns this room
    private bool FromPairedPeer(OSCMessage m)
    {
        return clientIp != null && m.Ip.ToString() == clientIp;
    }

    // ------------------------------------------------------------------ UI setup
    #region UI Setup

    private void SetupPlatform()
    {
        if (isPlatformSetup) return;

        SetupUI(IsHost ? createRoomPanelObj.name : joinRoomPanelObj.name);
        isPlatformSetup = true;
    }

    private void ReturnToHome()
    {
        isPlatformSetup = false;
        SetupPlatform();
        OnNetworkConnected?.Invoke(true);
    }

    private void SetPopup(bool slideOut)
    {
        Vector3 p = PopupPanel.anchoredPosition;
        PopupPanel.anchoredPosition = new Vector3(slideOut ? PopupOutXPos : PopupInXPos, p.y, p.z);
        PopupOutBtn.gameObject.SetActive(!slideOut);
        PopupInBtn.gameObject.SetActive(slideOut);
    }

    private void SetupUI(string _name)
    {
        createRoomPanelObj.SetActive(createRoomPanelObj.name == _name);
        if (createRoomPanelObj.activeSelf)
        {
            createRoomBtn.onClick.RemoveAllListeners();
            createRoomBtn.onClick.AddListener(() => CreateRoomBtnClick(createRoominput));

            foreach (GameObject g in xrRelatedItems) g.SetActive(false);
            overlayCamera.gameObject.SetActive(true);
        }

        joinRoomPanelObj.SetActive(joinRoomPanelObj.name == _name);

        webcamSelectPanelObj.SetActive(webcamSelectPanelObj.name == _name);
        if (webcamSelectPanelObj.activeSelf)
        {
            List<string> deviceNames = GetWebcamDeviceList();
            ddlWebcam.ClearOptions();
            ddlWebcam.AddOptions(deviceNames);

            refreshWebcamListBtn.onClick.RemoveAllListeners();
            refreshWebcamListBtn.onClick.AddListener(() =>
            {
                deviceNames = GetWebcamDeviceList();
                ddlWebcam.ClearOptions();
                ddlWebcam.AddOptions(deviceNames);
                infoText.text = "Webcam hardware device list updated!";
                if (deviceNames.Count > 0) OnPreviewWebcamClick(deviceNames[ddlWebcam.value]);
            });

            SetPopup(false);
            PopupOutBtn.onClick.RemoveAllListeners();
            PopupOutBtn.onClick.AddListener(() => SetPopup(true));
            PopupInBtn.onClick.RemoveAllListeners();
            PopupInBtn.onClick.AddListener(() => SetPopup(false));

            closeRoomBtn.onClick.RemoveAllListeners();
            closeRoomBtn.onClick.AddListener(CloseRoomBtnClick);

            if (deviceNames.Count > 0)
            {
                OnPreviewWebcamClick(deviceNames[0]);

                ddlWebcam.onValueChanged.RemoveAllListeners();
                ddlWebcam.onValueChanged.AddListener(delegate
                {
                    OnPreviewWebcamClick(ddlWebcam.options[ddlWebcam.value].text);
                    ddlWebcam.Hide();
                });
            }
            else
            {
                infoText.text = "<color=\"red\">No video capture devices found.</color>";
            }
        }

        requestRecordingPanel.SetActive(requestRecordingPanel.name == _name);
    }

    #endregion

    #region webcam setup
    private List<string> GetWebcamDeviceList()
    {
        var names = new List<string>();
        foreach (var d in WebCamTexture.devices) names.Add(d.name);
        return names;
    }

    private void OnPreviewWebcamClick(string _name)
    {
        if (ddlWebcam.options.Count == 0) return;

        StopHardwareCamera();
        webcamName = _name;
        infoText.text = $"Previewing webcam: {webcamName}";

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (WebcamToMP4.instance != null) WebcamToMP4.instance.PreviewWebcam(webcamName);
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

    // ------------------------------------------------------------------ rooms: public API
    #region Room API

    private bool creatingRoom;

    public void CreateRoomBtnClick(TMP_InputField _inp)
    {
        string rname = _inp.text.Trim();
        if (string.IsNullOrEmpty(rname))
        {
            infoText.text = "<color=\"red\">name invalid !</color>";
            return;
        }
        if (inRoom || creatingRoom) return;

        _inp.text = "";
        StartCoroutine(CreateRoomRoutine(rname));
    }

    private IEnumerator CreateRoomRoutine(string rname)
    {
        // listen briefly: if another PC already broadcasts this name, don't take it
        creatingRoom = true;
        infoText.text = "Checking room name...";
        string taken = null;
        yield return DiscoverHost(rname, r => taken = r, 1.5f);
        creatingRoom = false;

        if (taken != null)
        {
            infoText.text = "<color=\"red\">Room name already in use on this network.</color>";
            yield break;
        }

        roomName = rname;
        inRoom = true;
        peerConnected = false;
        currentSession = "";
        clientIp = null;
        ResetReliable();

        StopRoutine(ref broadcastRoutine);
        broadcastRoutine = StartCoroutine(BroadcastRoutine());

        infoText.text = $"<color=\"green\">room {roomName} created !</color>\n(LAN IP: {GetLocalIP()})";
        SetupUI(webcamSelectPanelObj.name);

        OnNetworkConnected?.Invoke(true);
        OnRoomJoined?.Invoke(true);
    }

    public void JoinRoomBtnClick(TMP_InputField _inp, Button _btn)
    {
        string rname = _inp.text.Trim();

        if (string.IsNullOrEmpty(rname))
        {
            infoText.text = "<color=\"red\">Please enter a room name!</color>";
            return;
        }

        if (inRoom || joinRoutine != null)
        {
            Debug.LogWarning("JoinRoom blocked: Client is already joining or inside a room.");
            return;
        }

        activeJoinButtonRef = _btn;
        if (activeJoinButtonRef != null) activeJoinButtonRef.interactable = false;
        infoText.text = "Joining room...";

        joinRoutine = StartCoroutine(JoinRoutine(rname));
    }

    public void RequestLeaveRoom()
    {
        if (!inRoom) return;

        if (!IsHost)
            for (int i = 0; i < 3; i++) transmitter.Send(Build(A_Leave, OSCValue.String(sessionId)));

        LeaveRoomLocal("<color=\"red\">Disconnected: Room was closed.</color>");
    }

    private void CloseRoomBtnClick()
    {
        if (!inRoom) return;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (WebcamToMP4.instance != null) WebcamToMP4.instance.StopRecording();
#endif
        if (peerConnected)
            for (int i = 0; i < 3; i++) transmitter.Send(Build(A_CloseRoom));

        LeaveRoomLocal("<color=\"red\">Disconnected: Room was closed.</color>");
    }

    private void LeaveRoomLocal(string message)
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (WebcamToMP4.instance != null) WebcamToMP4.instance.StopRecording();
#endif
        StopHardwareCamera();
        StopRoutine(ref broadcastRoutine);
        StopRoutine(ref joinRoutine);
        StopRoutine(ref reconnectRoutine);

        inRoom = false;
        peerConnected = false;
        reconnecting = false;
        joinState = JoinState.Idle;
        currentSession = "";
        sessionId = "";
        clientIp = null;
        isCurrentlyRecordingVideo = false;
        ResetReliable();

        if (remoteAvatarRoot != null) remoteAvatarRoot.SetActive(false);
        if (activeJoinButtonRef != null) { activeJoinButtonRef.interactable = true; activeJoinButtonRef = null; }

        OnRoomJoined?.Invoke(false);
        ReturnToHome();
        infoText.text = message;
    }

    private void StopRoutine(ref Coroutine c)
    {
        if (c != null) StopCoroutine(c);
        c = null;
    }

    #endregion

    // ------------------------------------------------------------------ host: room discovery + join handling
    #region Host side handshake

    private string currentSession = "";   // host: session id of the connected headset

    private IEnumerator BroadcastRoutine()
    {
        UdpClient udp = null;
        try { udp = new UdpClient { EnableBroadcast = true }; }
        catch (Exception ex) { Debug.LogWarning($"Broadcast socket failed: {ex.Message}"); }
        if (udp == null) yield break;

        var target = new IPEndPoint(IPAddress.Broadcast, discoveryPort);
        byte[] payload = Encoding.UTF8.GetBytes($"OSCROOM|{roomName}|{hostPort}");

        try
        {
            while (inRoom)
            {
                try { udp.Send(payload, payload.Length, target); } catch { }
                yield return new WaitForSecondsRealtime(1f);
            }
        }
        finally { udp.Close(); }
    }

    private void OnJoin(OSCMessage m)
    {
        if (!IsHost || !inRoom || m.Values.Count < 2) return;

        string sid = m.Values[0].StringValue;
        string room = m.Values[1].StringValue;
        if (room != roomName) return;

        string ip = m.Ip.ToString();
        bool sameSession = sid == currentSession;

        if (!sameSession && currentSession != "")
        {
            // slot is reserved for the first headset (even if it is temporarily offline):
            // tell the newcomer it's full, then point back at the real client
            RetargetTransmitter(ip, clientPort);
            transmitter.Send(Build(A_JoinFail, OSCValue.String("full")));
            if (clientIp != null) RetargetTransmitter(clientIp, clientPort);
            return;
        }

        if (!sameSession)
        {
            currentSession = sid;
            ResetReliable();
        }

        clientIp = ip;
        RetargetTransmitter(ip, clientPort);
        Touch();
        transmitter.Send(Build(A_Joined, OSCValue.String(roomName)));

        if (!peerConnected) MarkPeerConnected();
    }

    private void MarkPeerConnected()
    {
        peerConnected = true;
        infoText.text = "VR Headset connected";
        if (alertPanel != null) alertPanel.SetActive(false);
        if (remoteAvatarRoot != null) remoteAvatarRoot.SetActive(true);
        Touch();
    }

    private void PeerLost()
    {
        peerConnected = false;
        infoText.text = "VR Headset left or disconnected.";
        if (alertPanel != null) alertPanel.SetActive(true);
        if (remoteAvatarRoot != null) remoteAvatarRoot.SetActive(false);
    }

    private void OnPing(OSCMessage m)
    {
        if (!IsHost || !inRoom || m.Values.Count == 0) return;
        if (m.Values[0].StringValue != currentSession) return;

        Touch();
        if (!peerConnected) MarkPeerConnected();   // headset came back (e.g. after sleep)
        transmitter.Send(Build(A_Pong));
    }

    private void OnLeave(OSCMessage m)
    {
        if (!IsHost || !inRoom || m.Values.Count == 0) return;
        if (m.Values[0].StringValue != currentSession) return;

        currentSession = "";
        ResetReliable();
        PeerLost();
    }

    #endregion

    // ------------------------------------------------------------------ client: join / reconnect
    #region Client side handshake

    private IEnumerator JoinRoutine(string room)
    {
        string ip = null;
        int port = hostPort;

        if (manualHostIpInput != null && !string.IsNullOrWhiteSpace(manualHostIpInput.text))
        {
            ip = manualHostIpInput.text.Trim();
        }
        else
        {
            string hit = null;
            yield return DiscoverHost(room, r => hit = r, discoveryTimeout);
            if (hit != null)
            {
                string[] p = hit.Split('|');
                ip = p[0];
                int.TryParse(p[1], out port);
            }
        }

        if (ip == null)
        {
            JoinFailed("<color=\"red\">Room not found.</color>");
            yield break;
        }

        roomName = room;
        hostIp = ip;
        hostPortInUse = port;
        sessionId = Guid.NewGuid().ToString("N");
        ResetReliable();
        RetargetTransmitter(ip, port);

        joinState = JoinState.Waiting;
        float end = Time.unscaledTime + joinTimeout;
        while (joinState == JoinState.Waiting && Time.unscaledTime < end)
        {
            transmitter.Send(Build(A_Join, OSCValue.String(sessionId), OSCValue.String(room)));
            yield return new WaitForSecondsRealtime(0.4f);
        }

        if (joinState == JoinState.Full) { JoinFailed("<color=\"red\">Room is full!</color>"); yield break; }
        if (joinState != JoinState.Joined) { JoinFailed("<color=\"red\">Room not found.</color>"); yield break; }

        // success
        inRoom = true;
        reconnecting = false;
        Touch();
        nextPingTime = 0;

        if (activeJoinButtonRef != null) { activeJoinButtonRef.interactable = true; activeJoinButtonRef = null; }

        infoText.text = $"Joined Room: <color=\"green\">{roomName}</color>";
        if (requestRecordBtnText != null) requestRecordBtnText.text = "START RECORDING";
        SetupUI(requestRecordingPanel.name);
        if (observer != null) observer.SetActive(false);

        joinRoutine = null;
        OnNetworkConnected?.Invoke(true);
        OnRoomJoined?.Invoke(true);
    }

    private void JoinFailed(string message)
    {
        joinState = JoinState.Idle;
        joinRoutine = null;
        if (activeJoinButtonRef != null) { activeJoinButtonRef.interactable = true; activeJoinButtonRef = null; }
        if (joinRoominput != null) joinRoominput.text = "";
        infoText.text = message;
        OnRoomJoined?.Invoke(false);
    }

    private IEnumerator DiscoverHost(string room, Action<string> done, float timeout)
    {
        UdpClient listener = null;
        try { listener = new UdpClient(discoveryPort); }
        catch (Exception ex) { Debug.LogWarning($"Discovery socket failed: {ex.Message}"); }
        if (listener == null) { done(null); yield break; }

        string hit = null;
        float end = Time.unscaledTime + timeout;
        try
        {
            while (hit == null && Time.unscaledTime < end)
            {
                hit = TryReadDiscovery(listener, room);
                if (hit == null) yield return null;
            }
        }
        finally { listener.Close(); }

        done(hit);
    }

    private static string TryReadDiscovery(UdpClient l, string room)
    {
        try
        {
            while (l.Available > 0)
            {
                var ep = new IPEndPoint(IPAddress.Any, 0);
                string[] p = Encoding.UTF8.GetString(l.Receive(ref ep)).Split('|');
                if (p.Length >= 3 && p[0] == "OSCROOM" && p[1] == room && int.TryParse(p[2], out int port))
                    return ep.Address + "|" + port;
            }
        }
        catch { }
        return null;
    }

    private IEnumerator ReconnectRoutine()
    {
        reconnecting = true;
        OnNetworkConnected?.Invoke(false);
        OnRoomJoined?.Invoke(false);

        receiver.Close(); receiver.Connect();
        transmitter.Close(); transmitter.Connect();
        RetargetTransmitter(hostIp, hostPortInUse);

        joinState = JoinState.Waiting;
        float giveUp = Time.unscaledTime + reconnectGiveUpSeconds;

        while (Time.unscaledTime < giveUp)
        {
            infoText.text = "<color=\"yellow\">Reconnecting to host...</color>";
            transmitter.Send(Build(A_Join, OSCValue.String(sessionId), OSCValue.String(roomName)));
            yield return new WaitForSecondsRealtime(reconnectRetryDelay);

            if (joinState == JoinState.Joined)
            {
                reconnecting = false;
                reconnectRoutine = null;
                Touch();
                foreach (var p in pending.Values) p.lastSent = -999f;   // flush unacked data right away
                infoText.text = $"Joined Room: <color=\"green\">{roomName}</color>";
                OnNetworkConnected?.Invoke(true);
                OnRoomJoined?.Invoke(true);
                yield break;
            }
            if (joinState == JoinState.Full) break;
        }

        reconnectRoutine = null;
        LeaveRoomLocal("<color=\"red\">Host unreachable. Room closed.</color>");
    }

    private void OnJoined(OSCMessage m)
    {
        if (IsHost) return;
        joinState = JoinState.Joined;
        Touch();
    }

    private void OnJoinFail(OSCMessage m)
    {
        if (IsHost) return;
        joinState = JoinState.Full;
    }

    private void OnPong(OSCMessage m)
    {
        if (!IsHost) Touch();
    }

    private void OnCloseRoom(OSCMessage m)
    {
        if (IsHost || !inRoom) return;
        Debug.LogWarning("PC host closed the session.");
        LeaveRoomLocal("<color=\"red\">Room closed by host. Disconnecting...</color>");
    }

    #endregion

    // ------------------------------------------------------------------ pose sync
    #region Pose

    private void SendPose()
    {
        int n = Mathf.Min(clientPoseSources.Length, 8);
        var m = new OSCMessage(A_Pose);
        for (int i = 0; i < n; i++)
        {
            Transform t = clientPoseSources[i];
            Vector3 p = t != null ? t.position : Vector3.zero;
            Quaternion r = t != null ? t.rotation : Quaternion.identity;
            m.AddValue(OSCValue.Float(p.x)); m.AddValue(OSCValue.Float(p.y)); m.AddValue(OSCValue.Float(p.z));
            m.AddValue(OSCValue.Float(r.x)); m.AddValue(OSCValue.Float(r.y));
            m.AddValue(OSCValue.Float(r.z)); m.AddValue(OSCValue.Float(r.w));
        }
        transmitter.Send(m);
    }

    private void OnPose(OSCMessage m)
    {
        if (!IsHost || !inRoom || hostPoseReplicas == null || !FromPairedPeer(m)) return;
        Touch();

        int n = Mathf.Min(hostPoseReplicas.Length, m.Values.Count / 7);
        for (int i = 0; i < n; i++)
        {
            Transform t = hostPoseReplicas[i];
            if (t == null) continue;
            int k = i * 7;
            var pos = new Vector3(m.Values[k].FloatValue, m.Values[k + 1].FloatValue, m.Values[k + 2].FloatValue);
            var rot = new Quaternion(m.Values[k + 3].FloatValue, m.Values[k + 4].FloatValue,
                                     m.Values[k + 5].FloatValue, m.Values[k + 6].FloatValue);
            t.SetPositionAndRotation(pos, rot);
        }
    }

    #endregion

    // ------------------------------------------------------------------ client -> host requests (same names as before)
    #region Requests (VR client)

    public void RequestStartRecord(string _name)
    {
        if (!inRoom || IsHost) return;

        if (!isCurrentlyRecordingVideo)
        {
            isCurrentlyRecordingVideo = true;
            SendReliable(R_StartRec, OSCValue.String(_name ?? ""));
        }
    }

    public void RequestEndRecord()
    {
        if (!inRoom || IsHost) return;

        if (isCurrentlyRecordingVideo)
        {
            isCurrentlyRecordingVideo = false;
            SendReliable(R_StopRec);
        }
    }

    public void OnRequestRecordClicked(string _name)
    {
        if (!inRoom || IsHost) return;

        if (!isCurrentlyRecordingVideo)
        {
            isCurrentlyRecordingVideo = true;
            SendReliable(R_StartRec, OSCValue.String(_name ?? ""));
        }
        else
        {
            isCurrentlyRecordingVideo = false;
            SendReliable(R_StopRec);
        }
    }

    public void RequestStreamInit(string userName, string motionType, float sampleInterval, string recordTime)
    {
        if (!inRoom || IsHost) return;

        SendReliable(R_DataInit,
            OSCValue.String(userName ?? ""),
            OSCValue.String(motionType ?? ""),
            OSCValue.Float(sampleInterval),
            OSCValue.String(recordTime ?? ""));
    }

    public void RequestStreamChunk(string[] serializedWaypoints)
    {
        if (!inRoom || IsHost || serializedWaypoints == null || serializedWaypoints.Length == 0) return;

        foreach (var batch in PackStrings(serializedWaypoints))
            SendReliable(R_DataChunk, ToValues(batch));
    }

    public void RequestFinalizeStream(string[] finalWaypoints, string targetFileName)
    {
        if (!inRoom || IsHost) return;

        var batches = new List<List<string>>();
        if (finalWaypoints != null) foreach (var b in PackStrings(finalWaypoints)) batches.Add(b);

        // everything except the last batch goes as ordinary chunks, last one rides with the "final" message
        for (int i = 0; i < batches.Count - 1; i++)
            SendReliable(R_DataChunk, ToValues(batches[i]));

        var last = batches.Count > 0 ? batches[batches.Count - 1] : new List<string>();
        var payload = new List<OSCValue> { OSCValue.String(targetFileName ?? "session") };
        payload.AddRange(ToValues(last));
        SendReliable(R_DataFinal, payload.ToArray());
    }

    private IEnumerable<List<string>> PackStrings(string[] items)
    {
        var batch = new List<string>();
        int bytes = 0;
        foreach (string s in items)
        {
            string str = s ?? "";
            int size = Encoding.UTF8.GetByteCount(str) + 8;
            if (batch.Count > 0 && bytes + size > maxPacketBytes)
            {
                yield return batch;
                batch = new List<string>();
                bytes = 0;
            }
            batch.Add(str);
            bytes += size;
        }
        if (batch.Count > 0) yield return batch;
    }

    private static OSCValue[] ToValues(List<string> strings)
    {
        var arr = new OSCValue[strings.Count];
        for (int i = 0; i < arr.Length; i++) arr[i] = OSCValue.String(strings[i]);
        return arr;
    }

    #endregion

    // ------------------------------------------------------------------ host handlers (were the [PunRPC] methods)
    #region Host handlers

    private void HostStartWebcamRecording(string mp4OutputFileName)
    {
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

    private void HostStopWebcamRecording()
    {
        infoText.text = "<color=\"green\">Recording saved successfully to PC Storage!</color>";
        if (clientInfo != null) clientInfo.text = "";

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

    private void HostInitDataHeader(string userName, string motionType, float sampleInterval, string recordTime)
    {
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

    private void HostReceiveChunk(List<OSCValue> values, int startIndex)
    {
        if (compiledSessionRecordData == null || compiledSessionRecordData.timeStamp == null) return;

        for (int i = startIndex; i < values.Count; i++)
        {
            var wp = Newtonsoft.Json.JsonConvert.DeserializeObject<MultiTrackWaypoint>(values[i].StringValue);
            if (wp != null) AppendWaypointToRecordContainer(wp);
        }

        if (infoText != null)
        {
            infoText.text = $"<color=\"yellow\">Streaming Tracking Logs...</color>\nBuffered waypoints: {compiledSessionRecordData.timeStamp.Count}";
        }
    }

    private void HostFinalize(string targetFileName, List<OSCValue> values)
    {
        if (compiledSessionRecordData == null || compiledSessionRecordData.timeStamp == null) return;

        HostReceiveChunk(values, 1);   // value 0 is the file name

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(compiledSessionRecordData, Newtonsoft.Json.Formatting.Indented);
        SaveStreamedContentToDisk(targetFileName, json);

        compiledSessionRecordData = null;
    }

    #endregion

    // ------------------------------------------------------------------ client handler
    private void ClientForceStop()
    {
        Debug.LogWarning("[VR Client] Remote stop command received from PC Host. Intercepting data loop...");

        if (FitnessUIManager.Instance != null && TrajectoryRecorder.instance.isRecording)
        {
            FitnessUIManager.Instance.EndRecord();

            isCurrentlyRecordingVideo = false;
            if (requestRecordBtnText != null) requestRecordBtnText.text = "START RECORDING";
            if (infoText != null) infoText.text = "<color=\"red\">Recording stopped remotely by Host.</color>";
        }
        else
        {
            Debug.LogWarning("[VR Client] Force-stop ignored: Local singleton instance is null or not actively tracking.");
        }
    }

    // ------------------------------------------------------------------ write to disk
    #region Write To Disk
    private void AppendWaypointToRecordContainer(MultiTrackWaypoint wp)
    {
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
                infoText.text = $"<color=\"green\">File compiled & saved to PC!</color>\nName: {fileName}.json";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to commit compiled stream file payload down to PC disk: {ex.Message}");
        }
    }
    #endregion

    // ------------------------------------------------------------------ misc
    private static string GetLocalIP()
    {
        try
        {
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                s.Connect("8.8.8.8", 65530);   // no packet is actually sent
                return ((IPEndPoint)s.LocalEndPoint).Address.ToString();
            }
        }
        catch { return "unknown"; }
    }
}
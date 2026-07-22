//using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using Meta.XR.MultiplayerBlocks.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FitnessUIManager : MonoBehaviour
{
    public static FitnessUIManager Instance;

    [Header("Bar UI Variables")]
    [SerializeField] private Button startBtn;
    [SerializeField] private Button endBtn;
    [SerializeField] private Button settingUIBtn;
    [SerializeField] private Image settingUISpr;
    [SerializeField] private Button speedUIBtn;
    [SerializeField] private Image speedUISpr;
    [SerializeField] private Slider playbackSlider;

    [Header("Settings UI Variables")]
    [SerializeField] private GameObject settingsRadialGameObj;
    [SerializeField] private GameObject[] radialBgArray;
    [SerializeField] private Button radialNextBtn;           
    [SerializeField] private Button radialBackBtn;
    [SerializeField] private TMP_Text radialPageInfo;           //text of current settings page just in case
    private int currSettingsIndex = 0;

    [Header("Page 1")]
    [SerializeField] private Button changeDisplayBtn;           //change the viewing style
    [SerializeField] private TMP_Text changeDisplayBtnText;     //text of current viewing style
    [SerializeField] private Button joinRoomBtn;                //join room swaps with quitRoom
    [SerializeField] private Button quitRoomBtn;                //quit room swaps with joinRoom
    [SerializeField] private Button densityButton;              //pops up the slider
    [SerializeField] private Slider densitySlider;              //updates the slider
    [SerializeField] private TMP_Text sliderInfo;               //gives visual feedback to player on the slider
    [SerializeField] private Button resetBtn;                   //reset current prefabs, make them dissapear completely
    [SerializeField] private TMP_Text fpsText;                  //display fps value
    
    [Header("Page 2")]
    [SerializeField] private Button hideFloatingHeadBtn;        //hide the replaying statue head
    [SerializeField] private TMP_Text hideFloatingHeadText;     
    [SerializeField] private Button hideFloatingMenuBtn;        //hide the replay menu
    [SerializeField] private TMP_Text hideFloatingMenuText;     
    [SerializeField] private Button hideBtn;                    //disable the head and hand prefab
    [SerializeField] private TMP_Text hideInfo;                 
    [SerializeField] private Button hideReplayBtn;              //hides the entire replaying system
    [SerializeField] private TMP_Text hideReplayText;
    [SerializeField] private bool isHidingReplay = true;           

    [Header("Network Related")]
    [SerializeField] private GameObject roomInpGameObj;         //parent for this header
    [SerializeField] private TMP_InputField roomInp;            //room name input field
    [SerializeField] private Button roomJoimBtn;                //join this room
    [SerializeField] private Button roomCancelBtn;              //closes the parent without joining the room
    [SerializeField] private TMP_Text connText;

    [Header("Radial Speed UI Buttons")]
    [SerializeField] private GameObject speedRadialGameObj;
    [SerializeField] private Button playBtn;
    [SerializeField] private Button playRewindBtn;
    [SerializeField] private Button pauseBtn;
    [SerializeField] private Button fastBtn;
    private float fastId = 1;
    [SerializeField] private Button fastRewindBtn;
    private float rewindId = 1;
    [SerializeField] private Button slowBtn;
    private float slowId = 1;

    //this is for visualization for users
    [Header("UI Visual Feedback")]
    [SerializeField] private TMP_Text barInfo;
    [SerializeField] private GameObject playIcon;
    [SerializeField] private GameObject playRewindIcon;
    [SerializeField] private GameObject pauseIcon;
    [SerializeField] private GameObject fastIcon;
    [SerializeField] private GameObject fastRewindIcon;
    [SerializeField] private GameObject recordingIcon;
    [SerializeField] private GameObject recordingIconText;
    [SerializeField] private GameObject slowIcon;

    [Header("Name Input")]
    [SerializeField] private GameObject nameInpGameObj;
    [SerializeField] private GameObject barGameObj;
    [SerializeField] private TMP_Dropdown ddlMotionType;
    [SerializeField] private GameObject ddlContent;
    [SerializeField] private string[] motionSelection = { "long jump", "vertical jump", "jump rope", "walking", "others" };
    [SerializeField] private TMP_InputField nameInp;
    [SerializeField] private Button nameStartBtn;
    [SerializeField] private Button nameCancelBtn;

    [Header("Replay List UI")]
    [SerializeField] private GameObject statueHeadGameObj;
    [SerializeField] private GameObject sortListGameObj;
    [SerializeField] private Button sortListBtn;
    [SerializeField] private TMP_Text sortListText;

    //[Header("Position Indicators")]
    //[SerializeField] private GameObject startPrefab;
    //[SerializeField] private TMP_Text moveDistText;
    //[SerializeField] private TMP_Text heightTxt;

    [Header("special")]
    [SerializeField] private UIPlayerMovementStats m_stats;
    [SerializeField] private TMP_Text logTxt;

    private FitnessTestManager m_fitness;
    private PathVisualizer m_path;
    private ReplayManager m_replay;
    private TrajectoryRecorder_Config m_recorder;
    private NetworkRecordingManager m_network;

    private string motType;
    private string playerName;

    private bool isPlaying = false;
    private bool isHidden = true;
    private bool isMasterControl = false;
    private JumpResult jumpRes;

    //recording effect bcs its not apparent enough
    private Coroutine recordingAnimationCoroutine;
    private float simulatedRecordingTime = 0f;

    private bool isUpdatingSliderFromCode = false;
    private bool isUserDraggingSlider;

    public Action<string> OnMovementTypeChanged;

    //button color
    private Color32 activeColor = new Color32(118, 118, 118, 255);
    private Color32 defaultColor = Color.white;

    //for master control
    ControlMap controlMap;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        m_fitness = FitnessTestManager.instance;
        m_path = PathVisualizer.instance;
        m_replay = ReplayManager.instance;
        m_recorder = TrajectoryRecorder_Config.instance;
        m_network = NetworkRecordingManager.Instance;

        CheckDisplayType();
        endBtn.gameObject.SetActive(false);
        barInfo.text = "";
        ActivateIcon("");
        sortListText.text = m_replay.isSortByName ? "name" : "time";
        playbackSlider.gameObject.SetActive(false);

        //make sure text is correct
        hideFloatingHeadText.text = "Show Statue Replay";
        hideInfo.text = isHidden ? $"Hide Hands" : $"Show Hands";
        hideFloatingMenuText.text = sortListGameObj.activeSelf ? "Hide Floating Menu" : "Show Floating Menu";
        //hide replay
        
        m_path.SetReplayVisibility(isHidingReplay);
        hideReplayText.text = isHidingReplay ? "Hide replay" : "Show replay";

        //setup ddl
        ddlMotionType.ClearOptions();
        List<string> options = new List<string>(motionSelection);
        ddlMotionType.AddOptions(options);

        //make sure no motion type is selected
        motType = motionSelection[0];
        m_stats.ClearDisplay();

        //make sure the name input UI is disabled
        nameInpGameObj.SetActive(false);
        roomInpGameObj.SetActive(false);
        speedRadialGameObj.SetActive(false);
        settingsRadialGameObj.SetActive(false);

        m_network.OnRoomJoined+= CheckRoomState;
        m_network.OnNetworkConnected += CheckNetworkState;

        controlMap = new ControlMap();
        controlMap.Prototype.Enable();

        //controller button binding
        isMasterControl = false;
        MasterControl(isMasterControl);
        controlMap.Prototype.MasterControl.started += ctx =>
        {
            isMasterControl = !isMasterControl;
            MasterControl(isMasterControl);
        };

        controlMap.Prototype.Record.started += ctx =>
        {
            if (string.IsNullOrEmpty(nameInp.text))
                return;

                if (!isPlaying)
                StartRecord();
            else if (isPlaying)
                EndRecord();
        };

        #region Start and Stop recording


        nameStartBtn.onClick.AddListener(() =>
        {
            StartRecord();
        });

        endBtn.onClick.AddListener(() => {
            EndRecord();
        });

        #endregion

        #region display buttons
        //this is for settings
        settingUIBtn.onClick.AddListener(() =>
        {
            if (speedRadialGameObj.activeSelf)
            {
                speedRadialGameObj.SetActive(false);
                speedUISpr.color = defaultColor;
            }

            bool nextState = !settingsRadialGameObj.activeSelf;

            settingsRadialGameObj.SetActive(nextState);

            settingUISpr.color = nextState ? activeColor : defaultColor;

            barInfo.gameObject.SetActive(!nextState);
            densitySlider.gameObject.SetActive(!nextState);

            m_stats.ChangeDisplayActiveState(!nextState);
        });

        if (radialBgArray.Length <= 1)
        {
            radialNextBtn.gameObject.SetActive(false);
            radialBackBtn.gameObject.SetActive(false);
            radialPageInfo.gameObject.SetActive(false);
            radialBgArray[0].SetActive(true);
        }
        else
        {
            radialNextBtn.onClick.RemoveAllListeners();
            radialNextBtn.onClick.AddListener(() => ChangePage(1));

            radialBackBtn.onClick.RemoveAllListeners();
            radialBackBtn.onClick.AddListener(() => ChangePage(-1));

            ChangePage(0);
        }

        speedUIBtn.onClick.AddListener(() =>
        {
            if (settingsRadialGameObj.activeSelf)
            {
                settingsRadialGameObj.SetActive(false);
                settingUISpr.color = defaultColor;
            }

            bool nextState = !speedRadialGameObj.activeSelf;

            speedRadialGameObj.SetActive(nextState);

            speedUISpr.color = nextState ? activeColor : defaultColor;

            barInfo.gameObject.SetActive(!nextState);

            m_stats.ChangeDisplayActiveState(!nextState);
        });

        #endregion

        #region settings button page 1

        densityButton.onClick.AddListener(() => {
            densitySlider.gameObject.SetActive(!densitySlider.gameObject.activeSelf);
        });

        densitySlider.onValueChanged.AddListener((float _i)=> {
            m_path.OnDensityChanged(_i);
            sliderInfo.text = $"1/{_i} density";
        });

        hideBtn.onClick.AddListener(() =>
        {
            isHidden = !isHidden;
            m_path.SetHandsVisibility(isHidden);
            hideInfo.text = isHidden ? $"Hide Hands" : $"Show Hands";
        });

        changeDisplayBtn.onClick.AddListener(()=> { m_path.DisplayTrailingPath(1);
            CheckDisplayType();
        });

        resetBtn.onClick.AddListener(()=> { 
            m_fitness.ClearTrackingData();
            playbackSlider.gameObject.SetActive(false);
            barInfo.text = $"";
        });
        #endregion

        #region settings button page 2
        hideFloatingHeadBtn.onClick.AddListener(() => { 
            bool _b = PlayerReplayBox.instance.DisableMesh();
            statueHeadGameObj.SetActive(_b);

            hideFloatingHeadText.text = _b ? "Hide Statue Replay" : "Show Statue Replay";
        });

        hideFloatingMenuBtn.onClick.AddListener(() => { 
            sortListGameObj.SetActive(!sortListGameObj.activeSelf);
            hideFloatingMenuText.text = sortListGameObj.activeSelf ? "Hide Floating Menu" : "Show Floating Menu";
        });

        hideReplayBtn.onClick.AddListener(() =>
        {
            isHidingReplay = !isHidingReplay;
            m_path.SetReplayVisibility(isHidingReplay);
            hideReplayText.text = isHidingReplay ? "Hide replay" : "Show replay";

            if(m_path.hmdMotionPoints.Count > 0)
                playbackSlider.gameObject.SetActive(isHidingReplay);

        });

        roomJoimBtn.onClick.AddListener(() => { 
            m_network.JoinRoomBtnClick(roomInp, roomJoimBtn);
            roomInp.text = "";

            bool nextState = false;
            settingsRadialGameObj.SetActive(nextState);
            settingUISpr.color = nextState ? activeColor : defaultColor;
            barInfo.gameObject.SetActive(!nextState);
            densitySlider.gameObject.SetActive(!nextState);
            m_stats.ChangeDisplayActiveState(!nextState);
        });

        joinRoomBtn.onClick.AddListener(() =>
        {
            //back to main bar basically
            settingsRadialGameObj.SetActive(false);

            roomInpGameObj.SetActive(true);
        });

        roomCancelBtn.onClick.AddListener(() => {
            settingsRadialGameObj.SetActive(true);
            roomInpGameObj.SetActive(false);
        });

        quitRoomBtn.onClick.AddListener(() => {
            m_network.RequestLeaveRoom();
        });
        #endregion

        #region Name Setup

        //make name panel opens
        startBtn.onClick.AddListener(() => {
            //check radial fuckers first
            if (speedRadialGameObj.activeSelf == true || settingsRadialGameObj.activeSelf == true)
            {
                speedRadialGameObj.SetActive(false);
                speedUISpr.color = Color.white;

                settingsRadialGameObj.SetActive(false);
                settingUISpr.color = Color.white;

                m_stats.ChangeDisplayActiveState(false);
                return;
            }

            //if none are active do the normal thing
            nameInpGameObj.SetActive(true);
            barGameObj.SetActive(false);
            m_stats.ClearDisplay();
        });

        ddlMotionType.onValueChanged.AddListener(delegate {
            motType = motionSelection[ddlMotionType.value];
            m_recorder.ChangeMotionType(motType);

            m_stats.ChangeDisplay(motType, false);

            ddlMotionType.Hide();

            if (ddlContent != null)
                ddlContent.SetActive(false);

            // CRITICAL for VR: Clear selection to prevent the dropdown from staying "highlighted"
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        });

        nameInp.onValueChanged.AddListener(delegate {
            playerName = nameInp.text;
        });

        nameCancelBtn.onClick.AddListener(() => {
            m_stats.UpdateMovementType(motType, false);
            nameInpGameObj.SetActive(false);
            barGameObj.SetActive(true);
        });

        #endregion

        #region slider setup
        playbackSlider.onValueChanged.AddListener(delegate
        {
            if (!isUpdatingSliderFromCode)
            {
                m_path.ScrubToTimePercentage(playbackSlider.value);
            }
        });

        EventTrigger trigger = playbackSlider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = playbackSlider.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => { isUserDraggingSlider = true; });
        trigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => { isUserDraggingSlider = false; });
        trigger.triggers.Add(pointerUpEntry);

        #endregion

        #region playback buttons and slider setup
        playBtn.onClick.AddListener(() => {
            playBtn.gameObject.SetActive(false);
            pauseBtn.gameObject.SetActive(true);

            m_path.speedMult = 1f;
            fastId = 1; rewindId = 1; slowId = 1;
            barInfo.text = "x 1.0"; 
            ActivateIcon(playIcon.name);
        });

        pauseBtn.onClick.AddListener(() => {
            playBtn.gameObject.SetActive(true);
            pauseBtn.gameObject.SetActive(false);

            m_path.speedMult = 0f;
            fastId = 1; rewindId = 1; slowId = 1;
            barInfo.text = "Paused";
            ActivateIcon(pauseIcon.name);
        });

        playRewindBtn.onClick.AddListener(() => {
            m_path.speedMult = -1f;
            fastId = 1; rewindId = 1; slowId = 1;
            barInfo.text = "x 1.0";
            ActivateIcon(playRewindIcon.name);
        });

        fastBtn.onClick.AddListener(() => {
            rewindId = 1; slowId = 1;
            switch (fastId){
                case 1:
                    m_path.speedMult = 1.5f;
                    barInfo.text = $"x 1.5";
                    break;
                case 2:
                    m_path.speedMult = 2f;
                    barInfo.text = $"x 2.0";
                    break;
                case 3:
                    m_path.speedMult = 3f;
                    barInfo.text = $"x 3.0";
                    break;
            }
            fastId++;
            if (fastId > 3) fastId = 1;
            ActivateIcon(fastIcon.name);
        });

        fastRewindBtn.onClick.AddListener(() => {
            fastId = 1; slowId = 1;
            switch (rewindId)
            {
                case 1:
                    m_path.speedMult = -1f;
                    barInfo.text = $"x 1";
                    ActivateIcon(playRewindIcon.name);
                    break;
                case 2:
                    m_path.speedMult = -1.5f;
                    barInfo.text = $"x 1.5";
                    ActivateIcon(fastRewindIcon.name);
                    break;
                case 3:
                    m_path.speedMult = -2f;
                    barInfo.text = $"x 2.0";
                    ActivateIcon(fastRewindIcon.name);
                    break;
                case 4:
                    m_path.speedMult = -3f;
                    barInfo.text = $"x 3.0";
                    ActivateIcon(fastRewindIcon.name);
                    break;
            }
            rewindId++;
            if (rewindId > 4) rewindId = 1;
        });

        slowBtn.onClick.AddListener(() => {
            fastId = 1; rewindId = 1; 
            switch (slowId)
            {
                case 1:
                    m_path.speedMult = .75f;
                    barInfo.text = $"x 0.75";
                    break;
                case 2:
                    m_path.speedMult = .5f;
                    barInfo.text = $"x 0.5";
                    break;
                case 3:
                    m_path.speedMult = .25f;
                    barInfo.text = $"x 0.25";
                    break;
            }
            slowId++;
            if (slowId> 3) slowId= 1;
            ActivateIcon(slowIcon.name);
        });
        #endregion

        #region sorting list button setup
        sortListBtn.onClick.AddListener(() => {
            m_replay.isSortByName = !m_replay.isSortByName;
            sortListText.text = m_replay.isSortByName ? "name" : "time";
            m_replay.RefreshReplayList();
        });
        #endregion
    }

    private void Update()
    {
        //startPrefab.SetActive(m_fitness.calibratedStartPos != Vector3.zero);

        float fps = 1.0f / Time.unscaledDeltaTime;
        fpsText.text = $"FPS: {Mathf.Ceil(fps)}";

        if (!isUpdatingSliderFromCode && !isUserDraggingSlider)
        {
            isUpdatingSliderFromCode = true;
            playbackSlider.value = m_path.currentPlaybackTimeRaw;
            isUpdatingSliderFromCode = false;
        }
    }

    public void StartRecord()
    {
        string nameToSave = string.IsNullOrEmpty(nameInp.text) ? "Guest" : nameInp.text;
        m_recorder.ChangeUserName(nameToSave);

        nameInpGameObj.SetActive(false);
        barGameObj.SetActive(true);
        playbackSlider.enabled = false;
        m_fitness.StartMovement(success =>
        {
            if (success)
            {
                isPlaying = true;

                startBtn.gameObject.SetActive(false);
                endBtn.gameObject.SetActive(true);

                speedUIBtn.gameObject.SetActive(false);
                settingUIBtn.gameObject.SetActive(false);

                ActivateIcon(recordingIcon.name);

                //barInfo.text = "Recording"; GIVE RECORDING EFFECT
                m_stats.ClearDisplay();

                if (recordingAnimationCoroutine != null) StopCoroutine(recordingAnimationCoroutine);
                recordingAnimationCoroutine = StartCoroutine(RecordingAnimationRoutine());

                m_stats.UpdateMovementType(motType, true);
            }
        });

        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    //the code originally just inputted to EndRecord is moved here because needs to be called by PC
    public void EndRecord()
    {
        if (isPlaying)
        {
            if (recordingAnimationCoroutine != null)
            {
                StopCoroutine(recordingAnimationCoroutine);
                recordingAnimationCoroutine = null;
            }

            jumpRes = m_fitness.EndMovement();
            if (!jumpRes.success)
                return;

            isPlaying = !isPlaying;

            startBtn.gameObject.SetActive(true);
            endBtn.gameObject.SetActive(false);

            densitySlider.value = 1;

            m_replay.RefreshReplayList();
            ActivateIcon("");
            barInfo.text = $"";

            //resets to 1x speed
            m_path.speedMult = 1f;
            fastId = 1; rewindId = 1; slowId = 1;
            barInfo.text = isHidingReplay ? "Replaying..." : "Finished !";

            playbackSlider.enabled = true;

            playBtn.gameObject.SetActive(false);
            pauseBtn.gameObject.SetActive(true);

            speedUIBtn.gameObject.SetActive(true);
            settingUIBtn.gameObject.SetActive(true);
        }
        m_stats.UpdateMovementType(motType, false);
        m_stats.ChangeDisplay(motType, true);

        if (m_path.hmdMotionPoints.Count > 0)
            playbackSlider.gameObject.SetActive(isHidingReplay);
    }

    private void CheckDisplayType()
    {
        switch (m_path.displayID)
        {
            case 1:
                changeDisplayBtnText.text = "Trailing";
                break;

            case 2:
                changeDisplayBtnText.text = "Show All";
                break;

            case 3:
                changeDisplayBtnText.text = "Show Line";
                break;
        }
    }

    private IEnumerator RecordingAnimationRoutine()
    {
        simulatedRecordingTime = 0f;
        float blinkTimer = 0f;
        bool iconState = true;

        while (isPlaying)
        {
            // 1. Advance time based exactly on your 0.015s tracking intervals
            float dt = Time.deltaTime;
            simulatedRecordingTime += dt;

            // 2. Format the time into HH:MM:SS string style
            TimeSpan t = TimeSpan.FromSeconds(simulatedRecordingTime);
            string timeString = string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);

            // Update the display bar string
            barInfo.text = $"{timeString}";

            // 3. Handle the 0.5s Blinking Logic for the Icon
            blinkTimer += dt;
            if (blinkTimer >= 0.5f)
            {
                blinkTimer = 0f;
                iconState = !iconState;
                recordingIcon.SetActive(iconState);
            }

            // Wait exactly your recording interval rate before the next update tick
            yield return null;
        }
    }

    private void ActivateIcon(string name)
    {
        playIcon.SetActive(playIcon.name == name );
        pauseIcon.SetActive(pauseIcon.name == name );
        fastIcon.SetActive(fastIcon.name == name );
        playRewindIcon.SetActive(playRewindIcon.name == name );
        fastRewindIcon.SetActive(fastRewindIcon.name == name );
        recordingIcon.SetActive(recordingIcon.name == name );
        recordingIconText.SetActive(recordingIcon.name == name );
        slowIcon.SetActive(slowIcon.name == name );
    }

    private void CheckNetworkState(bool _conn)
    {
        connText.text = _conn ? $"connecting..." : "connected!";
        joinRoomBtn.interactable = _conn;
        quitRoomBtn.interactable = _conn;

    }

    private void CheckRoomState(bool _room)
    {
        joinRoomBtn.gameObject.SetActive(!_room);
        quitRoomBtn.gameObject.SetActive(_room);

        if (_room && roomInpGameObj.activeSelf)
            roomInpGameObj.SetActive(false);

        connText.text = _room ? $"room joined!" : "room left";
    }

    private void ChangePage(int direction)
    {
        radialBgArray[currSettingsIndex].SetActive(false);
        currSettingsIndex += direction;

        currSettingsIndex = Mathf.Clamp(currSettingsIndex, 0, radialBgArray.Length - 1);
        radialBgArray[currSettingsIndex].SetActive(true);

        radialBackBtn.interactable = (currSettingsIndex > 0);
        radialNextBtn.interactable = (currSettingsIndex < radialBgArray.Length - 1);

        if (radialPageInfo != null)
        {
            radialPageInfo.text = $" {currSettingsIndex + 1} / {radialBgArray.Length}";
        }
    }

    private void MasterControl(bool _b)
    {
        hideFloatingHeadBtn.gameObject.SetActive(_b);
        hideFloatingMenuBtn.gameObject.SetActive(_b);
        hideReplayBtn.gameObject.SetActive(_b);
    }
}

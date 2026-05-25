//using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using Meta.XR.MultiplayerBlocks.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FitnessUIManager : MonoBehaviour
{
    public static FitnessUIManager Instance;

    [Header("UI Indicatiors")]
    [SerializeField] private Button startBtn;
    [SerializeField] private Button endBtn;
    [SerializeField] private Button changeDisplayBtn;
    [SerializeField] private TMP_Text changeDisplayBtnText;
    [SerializeField] private Button resetBtn;
    [SerializeField] private Button hideBtn;
    [SerializeField] private TMP_Text hideInfo;
    [SerializeField] private Slider densitySlider;
    [SerializeField] private TMP_Text sliderInfo;
    [SerializeField] private TMP_Text fpsText;

    [Header("Bar UI Buttons")]
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
    [Header("Bar UI Visuals")]
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
    [SerializeField] private Button sortListBtn;
    [SerializeField] private TMP_Text sortListText;

    [Header("Position Indicators")]
    [SerializeField] private GameObject startPrefab;
    [SerializeField] private TMP_Text moveDistText;
    [SerializeField] private TMP_Text heightTxt;

    [Header("special")]
    [SerializeField] private UIPlayerMovementStats m_stats;
    [SerializeField] private TMP_Text logTxt;

    private FitnessTestManager m_fitness;
    private PathVisualizer m_path;
    private ReplayManager m_replay;
    private TrajectoryRecorder_Config m_recorder;

    private string motType;
    private string playerName;

    private bool isPlaying = false;
    private bool isHidden = true;
    private JumpResult jumpRes;

    //recording effect bcs its not apparent enough
    private Coroutine recordingAnimationCoroutine;
    private float simulatedRecordingTime = 0f;
    private const float RECORDING_TICK_RATE = 0.015f;

    public Action<string> OnMovementTypeChanged;

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

        CheckDisplayType();
        //endBtn.interactable = false;
        endBtn.gameObject.SetActive(false);
        barInfo.text = "";
        ActivateIcon("");
        sortListText.text = m_replay.isSortByName ? "name" : "time";
        hideInfo.text = isHidden ? $"cubes enabled" : $"cubes disabled";

        ddlMotionType.ClearOptions();
        List<string> options = new List<string>(motionSelection);
        ddlMotionType.AddOptions(options);

        motType = motionSelection[0];
        m_stats.ClearDisplay();

        nameInpGameObj.SetActive(false);

        #region recording btn setup
        startBtn.onClick.AddListener(() => {
            nameInpGameObj.SetActive(true);
            barGameObj.SetActive(false);
            m_stats.ClearDisplay();

            //moved to the startName btn

            //start moving if isplaying false
            //if (!isPlaying)
            //    m_fitness.StartMovement(success => {
            //        if (!success)
            //            return;
            //        jumpRes = new JumpResult();
            //        isPlaying = !isPlaying;

            //        startBtn.gameObject.SetActive(false);
            //        endBtn.gameObject.SetActive(true);

            //        startBtn.interactable = false;
            //        endBtn.interactable = true;
            //    });
            //or end moving if isplaying is true
            //ActivateIcon(recordingIcon.name);
            //barInfo.text = $"Recording";
        });

        endBtn.onClick.AddListener(() => {
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

                moveDistText.text = $"Moved {jumpRes.distance} cm";
                heightTxt.text = $"Highest point {jumpRes.height} cm";
                isPlaying = !isPlaying;

                startBtn.gameObject.SetActive(true);
                endBtn.gameObject.SetActive(false);

                //startBtn.interactable = true;
                //endBtn.interactable = false;

                densitySlider.value = 1;

                m_replay.RefreshReplayList();
                ActivateIcon("");
                barInfo.text = $"";

                m_path.speedMult = 1f;
                fastId = 1; rewindId = 1; slowId = 1;
                barInfo.text = "x 1.0";
            }
            m_stats.UpdateMovementType(motType, false);
            m_stats.ChangeDisplay(motType);
        });

        #endregion

        densitySlider.onValueChanged.AddListener((float _i)=> {
            m_path.OnDensityChanged(_i);
            sliderInfo.text = $"1/{_i} density";
        });

        hideBtn.onClick.AddListener(() =>
        {
            isHidden = !isHidden;
            m_path.PlaybackMeshObjSetActive(isHidden);
            hideInfo.text = isHidden ? $"cubes enabled" : $"cubes disabled";
        });

        changeDisplayBtn.onClick.AddListener(()=> { m_path.DisplayTrailingPath();
            CheckDisplayType();
        });

        resetBtn.onClick.AddListener(m_fitness.ClearTrackingData);

        #region Name Setup
        ddlMotionType.onValueChanged.AddListener(delegate {
            motType = motionSelection[ddlMotionType.value];
            m_recorder.ChangeMotionType(motType);

            m_stats.ChangeDisplay(motType);

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

        nameStartBtn.onClick.AddListener(() =>
        {
            string nameToSave = string.IsNullOrEmpty(nameInp.text) ? "Guest" : nameInp.text;
            m_recorder.ChangeUserName(nameToSave);

            nameInpGameObj.SetActive(false);
            barGameObj.SetActive(true);

            m_fitness.StartMovement(success =>
            {
                if (success)
                {
                    isPlaying = true;

                    startBtn.gameObject.SetActive(false);
                    endBtn.gameObject.SetActive(true);

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
        });

        nameCancelBtn.onClick.AddListener(() => {
            m_stats.UpdateMovementType(motType, false);
            nameInpGameObj.SetActive(false);
            barGameObj.SetActive(true);
        });

        #endregion
        #region playback buttons bar setup
        playBtn.onClick.AddListener(() => {
            m_path.speedMult = 1f;
            fastId = 1; rewindId = 1; slowId = 1;
            barInfo.text = "x 1.0"; 
            ActivateIcon(playIcon.name);
        });

        playRewindBtn.onClick.AddListener(() => {
            m_path.speedMult = -1f;
            fastId = 1; rewindId = 1; slowId = 1;
            barInfo.text = "x 1.0";
            ActivateIcon(playRewindIcon.name);
        });

        pauseBtn.onClick.AddListener(() => {
            m_path.speedMult = 0f;
            fastId = 1; rewindId = 1; slowId = 1;
            barInfo.text = "Paused";
            ActivateIcon(pauseIcon.name);
        });

        fastBtn.onClick.AddListener(() => {
            switch(fastId){
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
            switch (rewindId)
            {
                case 1:
                    m_path.speedMult = -1.5f;
                    barInfo.text = $"x 1.5";
                    break;
                case 2:
                    m_path.speedMult = -2f;
                    barInfo.text = $"x 2.0";
                    break;
                case 3:
                    m_path.speedMult = -3f;
                    barInfo.text = $"x 3.0";
                    break;
            }
            rewindId++;
            if (rewindId > 3) rewindId = 1;
            ActivateIcon(fastRewindIcon.name);
        });

        slowBtn.onClick.AddListener(() => {
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
        startPrefab.SetActive(m_fitness.calibratedStartPos != Vector3.zero);

        float fps = 1.0f / Time.unscaledDeltaTime;
        fpsText.text = $"FPS: {Mathf.Ceil(fps)}";
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
}

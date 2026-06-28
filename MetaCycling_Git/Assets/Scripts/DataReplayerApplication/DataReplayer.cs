using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using SFB;
using TMPro;
public class DataReplayer : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject hmdObj;
    public GameObject rcObj;
    public GameObject lcObj;
    public GameObject rhObj;
    public GameObject lhObj;

    [Header("UI 控件")]
    public TextMeshProUGUI fileNameText;
    public TextMeshProUGUI motionLabelText;
    public Button loadButton;
    public Button playPauseButton;
    public Slider timelineSlider;
    public TextMeshProUGUI timeDisplay;

    [Header("顯示/隱藏 Toggles")]
    public Toggle hmdToggle;
    public Toggle rcToggle;
    public Toggle lcToggle;
    public Toggle rhToggle;
    public Toggle lhToggle;

    [Header("視覺化特效 (軌跡與面向)")]
    public Button modeToggleButton; // 👇 新增：用來切換 Show All / Trailing 的按鈕
    public TextMeshProUGUI modeButtonText;
    private bool isShowAllMode = false;

    [Header("視覺化特效 (軌跡與面向)")]
    [Tooltip("0 = 不顯示, 1 = 顯示至當前時間的所有軌跡")]
    public Slider trailLengthSlider;
    [Tooltip("0 = 關閉, 0.01 = 稀疏 (每100點), 1 = 極密 (每點)")]
    public Slider orientationDensitySlider;
    public float trailWidth = 0.015f;
    [Tooltip("請放入一個 3D 模型 (例如有指向性的箭頭或細長方塊)")]
    public Mesh orientationMesh;
    public Material orientationMaterial;
    [Tooltip("調整面向物件的縮放大小 (X, Y, Z)")]
    public Vector3 orientationScale = Vector3.one;

    // 👇 新增這行來控制面向物件的初始旋轉補償
    [Tooltip("面向物件的基礎旋轉偏移量 (修正建模與Unity的軸向差異)")]
    public Vector3 orientationRotationOffset = new Vector3(-90f, 0f, 0f);
    
    [Header("影片同步對照")]
    public VideoPlayer videoPlayer;      // 👇 綁定 Unity 的 VideoPlayer
    public TMP_InputField syncDelayInput;    // 👇 讓使用者輸入延遲秒數的 UI (例如輸入 0.15)
    private float syncDelay = 0.15f;     // 預設影片比 JSON 慢 0.15 秒


    private StorageRecordData motionData;
    private bool isPlaying = false;
    private float currentTime = 0f;
    private float maxTime = 0f;

    // --- 快取陣列 (提升效能用) ---
    private Vector3[] hmdPos, rcPos, lcPos, rhPos, lhPos;
    private Quaternion[] hmdRot, rcRot, lcRot, rhRot, lhRot;

    // --- 軌跡渲染器 ---
    [Header("軌跡渲染器")]
    public LineRenderer hmdLine, rcLine, lcLine, rhLine, lhLine;

    // --- 當前播放狀態紀錄 ---
    private int currentFrameIndex = 0;
    private int trailStartIndex = 0;

    void Start()
    {
        // 綁定檔案讀取與播放事件
        loadButton.onClick.AddListener(SelectAndLoadFile);
        playPauseButton.onClick.AddListener(() => {
            isPlaying = !isPlaying;
            if (!isPlaying && videoPlayer != null)
            {
                videoPlayer.Pause();
            }
        });

        // 處理 Slider 拖拉 (Scrubbing)
        timelineSlider.onValueChanged.AddListener(OnTimelineScrub);

        // 綁定物件的顯示與隱藏
        if (hmdToggle) hmdToggle.onValueChanged.AddListener(isOn => hmdObj.SetActive(isOn));
        if (rcToggle) rcToggle.onValueChanged.AddListener(isOn => rcObj.SetActive(isOn));
        if (lcToggle) lcToggle.onValueChanged.AddListener(isOn => lcObj.SetActive(isOn));
        if (rhToggle) rhToggle.onValueChanged.AddListener(isOn => rhObj.SetActive(isOn));
        if (lhToggle) lhToggle.onValueChanged.AddListener(isOn => lhObj.SetActive(isOn));

        // 綁定物件與軌跡線的顯示隱藏
        SetupToggle(hmdToggle, hmdObj, hmdLine);
        SetupToggle(rcToggle, rcObj, rcLine);
        SetupToggle(lcToggle, lcObj, lcLine);
        SetupToggle(rhToggle, rhObj, rhLine);
        SetupToggle(lhToggle, lhObj, lhLine);

        if (modeToggleButton != null)
        {
            modeToggleButton.onClick.AddListener(ToggleDisplayMode);
            UpdateModeText();
        }

        if (syncDelayInput != null)
        {
            syncDelayInput.text = syncDelay.ToString("F3");
            syncDelayInput.onValueChanged.AddListener(val => {
                if (float.TryParse(val, out float res))
                {
                    syncDelay = res;
                    SyncVideoTime(); // 數值改變時立刻強制更新影片畫面
                }
            });
        }

        // 當特效滑桿數值改變時，強制更新畫面
        if (trailLengthSlider) trailLengthSlider.onValueChanged.AddListener(_ => UpdateTrailLines());
    }

    #region Select and Load File
    public void SelectAndLoadFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", "", false);
        if (paths.Length > 0)
        {
            AutoLoadPair(paths[0]);
        }
    }
    private void AutoLoadPair(string selectedPath)
    {
        string fileName = Path.GetFileNameWithoutExtension(selectedPath);
        string currentDir = Path.GetDirectoryName(selectedPath);
        
        // 取得上一層資料夾 (也就是你的 "A資料夾")
        DirectoryInfo parentDirInfo = Directory.GetParent(currentDir);
        
        if (parentDirInfo == null) 
        {
            Debug.LogWarning("無法解析上一層資料夾！");
            return;
        }

        string rootDir = parentDirInfo.FullName;

        // 在 A 資料夾底下全域搜尋同名的 .json 與 .mp4
        string[] jsonFiles = Directory.GetFiles(rootDir, fileName + ".json", SearchOption.AllDirectories);
        string[] mp4Files = Directory.GetFiles(rootDir, fileName + ".mp4", SearchOption.AllDirectories);

        string jsonPath = jsonFiles.Length > 0 ? jsonFiles[0] : null;
        string mp4Path = mp4Files.Length > 0 ? mp4Files[0] : null;

        // 將 InputField 顯示為當前載入的檔名 (比較乾淨)
        fileNameText.text = fileName;

        // 分別執行讀取
        if (!string.IsNullOrEmpty(jsonPath))
        {
            Debug.Log("載入 JSON: " + jsonPath);
            LoadJson(jsonPath);
        }
        else
        {
            Debug.LogWarning("找不到對應的 JSON 檔案！");
        }

        if (!string.IsNullOrEmpty(mp4Path))
        {
            Debug.Log("載入 MP4: " + mp4Path);
            LoadVideo(mp4Path);
        }
        else
        {
            Debug.LogWarning("找不到對應的 MP4 檔案！");
        }
    }

    public void LoadJson(string path)
    {
        string jsonContent = File.ReadAllText(path);
        motionData = JsonUtility.FromJson<StorageRecordData>(jsonContent);
        motionLabelText.text = motionData.motionType;

        if (motionData != null && motionData.timeStamp.Count > 0)
        {
            // 初始化時間軸
            maxTime = (float)motionData.timeStamp[motionData.timeStamp.Count - 1];
            timelineSlider.maxValue = maxTime;
            timelineSlider.value = 0f;
            currentTime = 0f;
            isPlaying = false;

            CacheMotionData();
            UpdateFrame(currentTime);
            UpdateTrailLines();
            SyncVideoTime();
        }
    }
    private void LoadVideo(string path)
    {
        if (videoPlayer != null)
        {
            // Unity 的 VideoPlayer 可以直接吃本機絕對路徑 (Url)
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = path;
            
            // 呼叫 Prepare 讓影片在背景預先載入，避免播放瞬間卡頓
            videoPlayer.Prepare(); 
        }
    }

    private void CacheMotionData()
    {
        int count = motionData.timeStamp.Count;
        hmdPos = new Vector3[count]; rcPos = new Vector3[count]; lcPos = new Vector3[count]; rhPos = new Vector3[count]; lhPos = new Vector3[count];
        hmdRot = new Quaternion[count]; rcRot = new Quaternion[count]; lcRot = new Quaternion[count]; rhRot = new Quaternion[count]; lhRot = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            if (i < motionData.pHMD.Count) { hmdPos[i] = motionData.pHMD[i].ToVector3(); hmdRot[i] = motionData.rHMD[i].ToQuaternion(); }
            if (i < motionData.pRC.Count) { rcPos[i] = motionData.pRC[i].ToVector3(); rcRot[i] = motionData.rRC[i].ToQuaternion(); }
            if (i < motionData.pLC.Count) { lcPos[i] = motionData.pLC[i].ToVector3(); lcRot[i] = motionData.rLC[i].ToQuaternion(); }
            if (i < motionData.pRH.Count) { rhPos[i] = motionData.pRH[i].ToVector3(); rhRot[i] = motionData.rRH[i].ToQuaternion(); }
            if (i < motionData.pLH.Count) { lhPos[i] = motionData.pLH[i].ToVector3(); lhRot[i] = motionData.rLH[i].ToQuaternion(); }
        }
    }
    #endregion

    void Update()
    {
        if (isPlaying && motionData != null)
        {
            currentTime += Time.deltaTime;
            if (currentTime >= maxTime)
            {
                currentTime = 0;
            }

            // 使用 SetValueWithoutNotify 避免觸發 Slider 的 onValueChanged 產生無窮迴圈
            timelineSlider.SetValueWithoutNotify(currentTime);
            UpdateFrame(currentTime);

            // 處理播放時的影片同步防漂移 (Drift Correction)
            if (videoPlayer != null)
            {
                float expectedVideoTime = currentTime - syncDelay;

                // 如果 JSON 剛開始播，但還沒到影片開始的時間 (例如 JSON 的前 0.15 秒)
                if (expectedVideoTime < 0f)
                {
                    if (videoPlayer.isPlaying) videoPlayer.Pause();
                    videoPlayer.time = 0f;
                }
                else
                {
                    // 確保影片處於播放狀態
                    if (!videoPlayer.isPlaying) videoPlayer.Play();

                    // Unity VideoPlayer 播放時會有微小的時間漂移，超過 0.05 秒強制校正
                    if (Mathf.Abs((float)videoPlayer.time - expectedVideoTime) > 0.05f)
                    {
                        videoPlayer.time = expectedVideoTime;
                    }
                }
            }
        }

        DrawOrientations();
    }

    private void OnTimelineScrub(float value)
    {
        if (motionData == null) return;

        currentTime = value;
        UpdateFrame(currentTime);
        SyncVideoTime(); // 拖拉時強制影片跳躍到對應影格
    }

    // 精準計算並設定影片時間
    private void SyncVideoTime()
    {
        if (videoPlayer == null || motionData == null) return;

        float expectedVideoTime = currentTime - syncDelay;

        if (expectedVideoTime < 0f)
        {
            expectedVideoTime = 0f; // 影片尚未開始
        }
        
        videoPlayer.time = expectedVideoTime;
    }

    private void UpdateFrame(float time)
    {
        if (motionData == null || motionData.timeStamp == null || motionData.timeStamp.Count == 0) return;

        // 將 float 轉型為 double，以符合 List<double> 的搜尋要求
        double targetTime = (double)time;

        // 直接呼叫 List 內建的 BinarySearch
        int index = motionData.timeStamp.BinarySearch(targetTime);

        // 找不到完全吻合的數值時，會回傳負數補數
        if (index < 0)
        {
            // 反轉補數，取得「小於目前時間的最後一個影格索引」
            index = ~index - 1;
        }
        currentFrameIndex = Mathf.Clamp(index, 0, motionData.timeStamp.Count - 1);

        // 確保 Index 不會超出 List 範圍
        index = Mathf.Clamp(index, 0, motionData.timeStamp.Count - 1);

        if (timeDisplay != null)
            timeDisplay.text = $"{time:F2} / {maxTime:F2} s";

        // 更新五個物件的 Transform
        if (hmdObj.activeInHierarchy) { hmdObj.transform.position = hmdPos[currentFrameIndex]; hmdObj.transform.rotation = hmdRot[currentFrameIndex]; }
        if (rcObj.activeInHierarchy) { rcObj.transform.position = rcPos[currentFrameIndex]; rcObj.transform.rotation = rcRot[currentFrameIndex]; }
        if (lcObj.activeInHierarchy) { lcObj.transform.position = lcPos[currentFrameIndex]; lcObj.transform.rotation = lcRot[currentFrameIndex]; }
        if (rhObj.activeInHierarchy) { rhObj.transform.position = rhPos[currentFrameIndex]; rhObj.transform.rotation = rhRot[currentFrameIndex]; }
        if (lhObj.activeInHierarchy) { lhObj.transform.position = lhPos[currentFrameIndex]; lhObj.transform.rotation = lhRot[currentFrameIndex]; }

        // 更新軌跡
        UpdateTrailLines();
    }
    private void UpdateTrailLines()
    {
        if (motionData == null || trailLengthSlider == null) return;

        int totalFrames = motionData.timeStamp.Count;
        // 計算軌跡應該包含幾個點 (Slider 0~1 對應 0~全部影格)
        int pointCount = currentFrameIndex - trailStartIndex + 1;

        if (isShowAllMode)
        {
            trailStartIndex = 0; // 從第0格開始
            pointCount = totalFrames; // 畫到最後一格
        }
        else
        {
            int trailLengthFrames = Mathf.RoundToInt(trailLengthSlider.value * totalFrames);
            trailStartIndex = Mathf.Max(0, currentFrameIndex - trailLengthFrames);
            pointCount = currentFrameIndex - trailStartIndex + 1; // 畫到當前時間格
        }

        UpdateSingleLine(hmdLine, hmdObj, hmdPos, pointCount);
        UpdateSingleLine(rcLine, rcObj, rcPos, pointCount);
        UpdateSingleLine(lcLine, lcObj, lcPos, pointCount);
        UpdateSingleLine(rhLine, rhObj, rhPos, pointCount);
        UpdateSingleLine(lhLine, lhObj, lhPos, pointCount);
    }
    private void UpdateSingleLine(LineRenderer lr, GameObject obj, Vector3[] cache, int count)
    {
        if (!obj.activeSelf || count <= 1 || trailLengthSlider.value == 0)
        {
            lr.positionCount = 0;
            return;
        }

        lr.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            lr.SetPosition(i, cache[trailStartIndex + i]);
        }
    }
    private void DrawOrientations()
    {
        if (motionData == null || orientationDensitySlider == null || orientationDensitySlider.value <= 0f) return;
        if (orientationMesh == null || orientationMaterial == null || trailLengthSlider.value == 0f) return;

        if (!isShowAllMode && trailLengthSlider.value == 0f) return;

        int step = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(10, 1, orientationDensitySlider.value)));

        // 提前計算好旋轉偏移量的四元數，節省迴圈內的運算效能
        Quaternion offsetQuat = Quaternion.Euler(orientationRotationOffset);

        int startIdx = isShowAllMode ? 0 : trailStartIndex;
        int endIdx = isShowAllMode ? (motionData.timeStamp.Count - 1) : currentFrameIndex;

        for (int i = startIdx; i <= endIdx; i++)
        {
            if (i % step == 0)
            {
                // 將原始的旋轉資料 (例如 hmdRot[i]) 乘上 offsetQuat，達到疊加局部旋轉的效果
                if (hmdObj.activeSelf)
                    Graphics.DrawMesh(orientationMesh, Matrix4x4.TRS(hmdPos[i], hmdRot[i] * offsetQuat, orientationScale), orientationMaterial, 0);

                if (rcObj.activeSelf)
                    Graphics.DrawMesh(orientationMesh, Matrix4x4.TRS(rcPos[i], rcRot[i] * offsetQuat, orientationScale), orientationMaterial, 0);

                if (lcObj.activeSelf)
                    Graphics.DrawMesh(orientationMesh, Matrix4x4.TRS(lcPos[i], lcRot[i] * offsetQuat, orientationScale), orientationMaterial, 0);

                if (rhObj.activeSelf)
                    Graphics.DrawMesh(orientationMesh, Matrix4x4.TRS(rhPos[i], rhRot[i] * offsetQuat, orientationScale), orientationMaterial, 0);

                if (lhObj.activeSelf)
                    Graphics.DrawMesh(orientationMesh, Matrix4x4.TRS(lhPos[i], lhRot[i] * offsetQuat, orientationScale), orientationMaterial, 0);
            }
        }
    }

    private void SetupToggle(Toggle toggle, GameObject obj, LineRenderer line)
    {
        // 動態建立軌跡線組件
        line.startWidth = trailWidth;
        line.endWidth = trailWidth;
        line.useWorldSpace = true;
        // 使用 Unity 預設的無光照材質畫線
        line.material = new Material(Shader.Find("Sprites/Default"));

        if (toggle)
        {
            LineRenderer refLine = line; // 避免閉包問題
            toggle.onValueChanged.AddListener(isOn =>
            {
                obj.SetActive(isOn);
                refLine.enabled = isOn;
                UpdateTrailLines(); // 切換時刷新軌跡
            });
        }
    }
    private void ToggleDisplayMode()
    {
        isShowAllMode = !isShowAllMode;
        UpdateModeText();
        UpdateTrailLines(); // 切換模式時立刻強制刷新畫面
    }
    private void UpdateModeText()
    {
        if (modeButtonText != null)
        {
            modeButtonText.text = isShowAllMode ? "Show All" : "Trailing";
        }
    }
}

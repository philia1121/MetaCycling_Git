using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    private StorageRecordData motionData;
    private bool isPlaying = false;
    private float currentTime = 0f;
    private float maxTime = 0f;

    void Start()
    {
        // 綁定檔案讀取與播放事件
        loadButton.onClick.AddListener(SelectAndLoadFile);
        playPauseButton.onClick.AddListener(() => isPlaying = !isPlaying);

        // 處理 Slider 拖拉 (Scrubbing)
        timelineSlider.onValueChanged.AddListener(OnTimelineScrub);

        // 綁定物件的顯示與隱藏
        if (hmdToggle) hmdToggle.onValueChanged.AddListener(isOn => hmdObj.SetActive(isOn));
        if (rcToggle) rcToggle.onValueChanged.AddListener(isOn => rcObj.SetActive(isOn));
        if (lcToggle) lcToggle.onValueChanged.AddListener(isOn => lcObj.SetActive(isOn));
        if (rhToggle) rhToggle.onValueChanged.AddListener(isOn => rhObj.SetActive(isOn));
        if (lhToggle) lhToggle.onValueChanged.AddListener(isOn => lhObj.SetActive(isOn));
    }

    public void SelectAndLoadFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", "", true);
        if (paths.Length > 0)
        {
            string fullPath = paths[0];
            LoadJson(fullPath);

            string fileName = Path.GetFileName(fullPath);
            fileNameText.text = fileName;
        }
    }

    public void LoadJson(string path)
    {
        string jsonContent = File.ReadAllText(path);
        motionData = JsonUtility.FromJson<StorageRecordData>(jsonContent);

        if (motionData != null && motionData.timeStamp.Count > 0)
        {
            // 初始化時間軸
            maxTime = (float)motionData.timeStamp[motionData.timeStamp.Count - 1];
            timelineSlider.maxValue = maxTime;
            timelineSlider.value = 0f;
            currentTime = 0f;
            isPlaying = false;

            UpdateFrame(currentTime);
        }
    }

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
        }
    }

    private void OnTimelineScrub(float value)
    {
        if (motionData == null) return;

        currentTime = value;
        UpdateFrame(currentTime);
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

        // 確保 Index 不會超出 List 範圍
        index = Mathf.Clamp(index, 0, motionData.timeStamp.Count - 1);

        if (timeDisplay != null)
            timeDisplay.text = $"{time:F2} / {maxTime:F2} s";

        // 更新五個物件的 Transform
        if (motionData.pHMD != null && index < motionData.pHMD.Count)
        {
            hmdObj.transform.position = motionData.pHMD[index].ToVector3();
            hmdObj.transform.rotation = motionData.rHMD[index].ToQuaternion();
        }

        if (motionData.pRC != null && index < motionData.pRC.Count)
        {
            rcObj.transform.position = motionData.pRC[index].ToVector3();
            rcObj.transform.rotation = motionData.rRC[index].ToQuaternion();
        }

        if (motionData.pLC != null && index < motionData.pLC.Count)
        {
            lcObj.transform.position = motionData.pLC[index].ToVector3();
            lcObj.transform.rotation = motionData.rLC[index].ToQuaternion();
        }

        if (motionData.pRH != null && index < motionData.pRH.Count)
        {
            rhObj.transform.position = motionData.pRH[index].ToVector3();
            rhObj.transform.rotation = motionData.rRH[index].ToQuaternion();
        }

        if (motionData.pLH != null && index < motionData.pLH.Count)
        {
            lhObj.transform.position = motionData.pLH[index].ToVector3();
            lhObj.transform.rotation = motionData.rLH[index].ToQuaternion();
        }
    }
}

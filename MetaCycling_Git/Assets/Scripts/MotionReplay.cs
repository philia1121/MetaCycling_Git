using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class MotionReplay : MonoBehaviour
{
    [Header("File Settings")]
    public string folderPath;
    public string subFolder;
    public string fileName = "MultiTraj_20251212_161936.json";

    [Header("GameObject Refs")]
    public Transform ReplayRoot_Transform;
    public Transform HMD_Transform;
    public Transform RCont_Transform, LCont_Transform, RHand_Transform, LHand_Transform;

    [Header("Play Settings")]
    public bool autoRecenter;
    public bool isPlaying = false;
    public float playbackSpeed = 1.0f;

    private TrajectorySession logData;
    private float currentTime = 0f;
    private int currentIndex = 0;

    void Start()
    {
        // 如果 folderPath 沒填，預設設為專案的 StreamingAssets 資料夾
        if (string.IsNullOrEmpty(folderPath))
        {
            folderPath = Path.Combine(Application.streamingAssetsPath, subFolder);
        }

        LoadAndParseJSON();
    }

    void Update()
    {
        if (!isPlaying || logData == null || logData.waypoints.Count == 0) return;

        currentTime += Time.deltaTime * playbackSpeed;

        while (currentIndex < logData.waypoints.Count - 1 &&
               logData.waypoints[currentIndex + 1].timestamp <= currentTime)
        {
            currentIndex++;
        }

        if (currentIndex >= logData.waypoints.Count - 1)
        {
            Debug.Log("End of Replay");
            isPlaying = false;
            return;
        }

        ApplyMotion(logData.waypoints[currentIndex]);
    }

    void LoadAndParseJSON()
    {
        fileName += fileName.EndsWith(".json") ? "" : ".json";
        string fullPath = Path.Combine(folderPath, fileName);

        if (File.Exists(fullPath))
        {
            try
            {
                string jsonContent = File.ReadAllText(fullPath);
                logData = JsonUtility.FromJson<TrajectorySession>(jsonContent);
                Debug.Log($"成功讀取 Log，共 {logData.waypoints.Count} 筆數據。");

                if (autoRecenter)
                {
                    AlignToOrigin();
                }

                currentTime = 0;
                currentIndex = 0;
                isPlaying = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"JSON 解析失敗: {e.Message}");
            }
        }
        else
        {
            Debug.LogError($"找不到檔案: {fullPath}");
        }
    }

    void ApplyMotion(MultiTrackWaypoint data)
    {
        if (HMD_Transform != null)
        {
            HMD_Transform.localPosition = data.pos_HMD;
            HMD_Transform.localEulerAngles = data.rot_HMD;
        }

        if (RCont_Transform != null)
        {
            RCont_Transform.localPosition = data.pos_RCont;
            RCont_Transform.localEulerAngles = data.rot_RCont;
        }

        if (LCont_Transform != null)
        {
            LCont_Transform.localPosition = data.pos_LCont;
            LCont_Transform.localEulerAngles = data.rot_LCont;
        }

        if (RHand_Transform != null)
        {
            RHand_Transform.localPosition = data.pos_RHand;
            RHand_Transform.localEulerAngles = data.rot_RHand;
        }

        if (LHand_Transform != null)
        {
            LHand_Transform.localPosition = data.pos_LHand;
            LHand_Transform.localEulerAngles = data.rot_LHand;
        }
    }
    void AlignToOrigin()
    {
        if (logData.waypoints.Count == 0 || ReplayRoot_Transform == null) return;

        Vector3 startEuler = logData.waypoints[0].rot_HMD;
        float startYaw = startEuler.y;

        ReplayRoot_Transform.rotation = Quaternion.Euler(0, -startYaw, 0);
    }
}

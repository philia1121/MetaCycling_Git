using Meta.XR.BuildingBlocks.AIBlocks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

public class ReplayManager : MonoBehaviour
{
    [SerializeField] private Transform scrollViewContainer;
    [SerializeField] private Transform content;
    [SerializeField] private ReplayDataContainer jsonPrefab;

    [SerializeField] private Transform hmdPlayback;
    [SerializeField] private Transform lHandPlayback;
    [SerializeField] private Transform rHandPlayback;

    [SerializeField] private TMP_Text loggingTxt;

    public bool isSortByName = true;

    public static ReplayManager instance;

    private string folderPath;
    private FitnessTestManager m_Fitness;
    private PathVisualizer m_Path;

    private Vector3 calibratedStartpos;

    private List<string> jsonFiles = new List<string>();

    private void Awake()
    {
        if (instance == null)
            instance = this;

    }

    private void Start()
    {
        m_Fitness = FitnessTestManager.instance;
        m_Path = PathVisualizer.instance;

        folderPath = Application.persistentDataPath;

        RefreshReplayList();
        if(m_Fitness.calibratedStartPos == Vector3.zero)
            calibratedStartpos = Vector3.zero;

    }

    public void RefreshReplayList()
    {
        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"Directory not found: {folderPath}");
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        // Grab all files ending in .json
        DirectoryInfo info = new DirectoryInfo(folderPath);

        //Sort by LastWriteTime (Newest to Oldest)
        var sortedFiles = isSortByName ?
            info.GetFiles("*.json")
            .OrderByDescending(f => f.Name)
            .ToList()
            :
            info.GetFiles("*.json")
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        string debugAccumulator = "";

        for (int i = 0; i < sortedFiles.Count; i++)
        {
            try
            {
                string jsonContent = File.ReadAllText(sortedFiles[i].FullName);
                TrajectorySession logData = JsonUtility.FromJson<TrajectorySession>(jsonContent);

                if (logData == null) continue;

                string cleanFileName = sortedFiles[i].Name;

                debugAccumulator += $"{i}. {cleanFileName}\n";

                ReplayDataContainer _c = Instantiate(jsonPrefab, content);

                _c.transform.SetParent(content, false);
                _c.transform.localScale = Vector3.one;

                _c.SetData(this, cleanFileName, logData);
            }
            catch (System.Exception e)
            {
                // This was likely catching the error and stopping the spawn without you noticing
                Debug.LogError($"Loop failed at index {i}: {e.Message}");
            }
        }

        if (loggingTxt != null) loggingTxt.text = debugAccumulator;
    }

    public void SpawnReplay(ReplayData savedData, Vector3 savedOffset)
    {
        calibratedStartpos = new Vector3(m_Fitness.calibratedStartPos.x, 0, m_Fitness.calibratedStartPos.z);

        ReplayData adjustedData = new ReplayData
        {
            hmdMotionPoints = OffsetPointList(savedData.hmdMotionPoints, savedOffset, calibratedStartpos),
            lHandMotionPoints = OffsetPointList(savedData.lHandMotionPoints, savedOffset, calibratedStartpos),
            rHandMotionPoints = OffsetPointList(savedData.rHandMotionPoints, savedOffset, calibratedStartpos)
        };

        m_Path.DisplayGhostPath(adjustedData);
    }

    private List<MotionPointSimple> OffsetPointList(List<MotionPointSimple> points, Vector3 oldOrigin, Vector3 newOrigin)
    {
        List<MotionPointSimple> offsetList = new List<MotionPointSimple>();

        foreach (var p in points)
        {
            //brain not working but prolly => (Original Point - Old Start Position) + New Start Position
            Vector3 alignedPos = (p.position - oldOrigin) + newOrigin;

            offsetList.Add(new MotionPointSimple
            {
                position = alignedPos,
                rotation = p.rotation
            });
        }
        return offsetList;
    }
}

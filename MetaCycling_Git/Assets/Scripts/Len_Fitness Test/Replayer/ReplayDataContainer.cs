using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class ReplayDataContainer : MonoBehaviour
{
    [SerializeField] TMP_Text disptxt;

    private ReplayManager _m;
    private List<MotionPointSimple> hmdMotionPoints = new List<MotionPointSimple>();
    private List<MotionPointSimple> rHandMotionPoints = new List<MotionPointSimple>();
    private List<MotionPointSimple> lHandMotionPoints = new List<MotionPointSimple>();

    [SerializeField] private Button btn;

    private void Start()
    {
        _m = ReplayManager.instance;
    }

    public void SetData(ReplayManager _manager, string fileName,  TrajectorySession session)
    {
        if (_m == null)
            _m = _manager;

        string rawName = fileName.Replace(".json", "");
        string[] parts = rawName.Split('_');

        if (parts.Length >= 3)
        {
            string name = parts[0];
            string datePart = parts[1]; // yyyymmdd
            string timePart = parts[2]; // hhmmss

            string mm = datePart.Substring(4, 2);
            string dd = datePart.Substring(6, 2);

            string hh = timePart.Substring(0, 2);
            string min = timePart.Substring(2, 2);

            disptxt.text = $"{name} - {mm}/{dd} - {hh}:{min} ({session.waypoints.Count})";
        }

        //just in case
        hmdMotionPoints.Clear();
        lHandMotionPoints.Clear();
        rHandMotionPoints.Clear();

        MotionPointSimple lastHmd = new MotionPointSimple { rotation = Quaternion.identity };
        MotionPointSimple lastLHand = new MotionPointSimple { rotation = Quaternion.identity };
        MotionPointSimple lastRHand = new MotionPointSimple { rotation = Quaternion.identity };

        for (int i = 0; i < session.waypoints.Count; i++)
        {
            var wp = session.waypoints[i];

            // 1. Process HMD
            if (wp.pos_HMD.sqrMagnitude > 0.001f)
            {
                lastHmd = new MotionPointSimple { position = wp.pos_HMD, rotation = wp.rot_HMD };
            }
            hmdMotionPoints.Add(lastHmd);

            // 2. Process Left Hand
            if (wp.pos_LHand.sqrMagnitude > 0.001f)
            {
                lastLHand = new MotionPointSimple { position = wp.pos_LHand, rotation = wp.rot_LHand };
            }
            lHandMotionPoints.Add(lastLHand);

            // 3. Process Right Hand
            if (wp.pos_RHand.sqrMagnitude > 0.001f)
            {
                lastRHand = new MotionPointSimple { position = wp.pos_RHand, rotation = wp.rot_RHand };
            }
            rHandMotionPoints.Add(lastRHand);
        }

        if (btn == null) btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ReturnData);

        //for (int i = 0; i < session.waypoints.Count; i++)
        //{
        //    //hmd
        //    MotionPointSimple _pHmd = new MotionPointSimple
        //    {
        //        position = session.waypoints[i].pos_HMD,
        //        rotation = session.waypoints[i].rot_HMD,
        //    };
        //    hmdMotionPoints.Add(_pHmd);

        //    //lHand
        //    MotionPointSimple _pLH = new MotionPointSimple
        //    {
        //        position = session.waypoints[i].pos_LHand,
        //        rotation = session.waypoints[i].rot_LHand,
        //    };
        //    lHandMotionPoints.Add(_pLH);

        //    //rHand
        //    MotionPointSimple _pRH = new MotionPointSimple
        //    {
        //        position = session.waypoints[i].pos_RHand,
        //        rotation = session.waypoints[i].rot_RHand,
        //    };
        //    rHandMotionPoints.Add(_pRH);
        //}

        //btn.onClick.AddListener(ReturnData);
    }

    public void ReturnData()
    {
        ReplayData data = new ReplayData
        {
            hmdMotionPoints = this.hmdMotionPoints,
            rHandMotionPoints = this.rHandMotionPoints,
            lHandMotionPoints = this.lHandMotionPoints
        };

        // Extract the first HMD position for X and Z alignment
        Vector3 offset = Vector3.zero;
        if (hmdMotionPoints.Count > 0)
        {
            // We only care about X and Z for the floor-level spawning position
            offset = new Vector3(hmdMotionPoints[0].position.x, 0, hmdMotionPoints[0].position.z);
        }

        _m.SpawnReplay(data, offset);
    }
}

[System.Serializable]
public class ReplayData
{
    public List<MotionPointSimple> hmdMotionPoints;
    public List<MotionPointSimple> rHandMotionPoints;
    public List<MotionPointSimple> lHandMotionPoints;
}

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
    private List<MotionPointSimple> lContMotionPoints = new List<MotionPointSimple>();
    private List<MotionPointSimple> rContMotionPoints = new List<MotionPointSimple>();
    private List<MotionPointSimple> lHandMotionPoints = new List<MotionPointSimple>();
    private List<MotionPointSimple> rHandMotionPoints = new List<MotionPointSimple>();

    [SerializeField] private Button btn;

    private void Start()
    {
        _m = ReplayManager.instance;
    }

    public string SetData(ReplayManager _manager, string fileName,  TrajectorySession session)
    {
        if (_m == null)
            _m = _manager;
        

        string rawName = fileName.Replace(".json", "");
        //disptxt.text = $"{rawName} - ( {session.waypoints.Count}pt, {session.waypoints.Count * 0.015f}s)";
        string[] parts = rawName.Split('_');

        if (parts.Length >= 3)
        {
            string name = (session.userName=="") ? parts[0] : session.userName;

            string mm = parts[2];
            string dd = parts[3];

            string hh = parts[4];
            string min = parts[5];

            disptxt.text = $"{name} - {mm}/{dd} - {hh}:{min} - [{(session.motionType=="" ? "others" : session.motionType)}] ({session.waypoints.Count*0.015f}s)";
        }

        //just in case
        hmdMotionPoints.Clear();
        lContMotionPoints.Clear();
        rContMotionPoints.Clear();
        lHandMotionPoints.Clear();
        rHandMotionPoints.Clear();

        MotionPointSimple lastHmd = new MotionPointSimple { rotation = Quaternion.identity };
        MotionPointSimple lastLHand = new MotionPointSimple { rotation = Quaternion.identity };
        MotionPointSimple lastRHand = new MotionPointSimple { rotation = Quaternion.identity };

        for (int i = 0; i < session.waypoints.Count; i++)
        {
            var wp = session.waypoints[i];

            // 1. Process HMD
            if (wp.pos_HMD.ToVector3().sqrMagnitude > 0.001f)
            {
                lastHmd = new MotionPointSimple { position = wp.pos_HMD.ToVector3(), rotation = wp.rot_HMD.ToQuaternion() };
            }
            hmdMotionPoints.Add(lastHmd);

            if (wp.pos_LCont.ToVector3().sqrMagnitude > 0.001f)
            {
                lastLHand = new MotionPointSimple { position = wp.pos_LCont.ToVector3(), rotation = wp.rot_LCont.ToQuaternion() };
            }
            lContMotionPoints.Add(lastLHand);

            if (wp.pos_RCont.ToVector3().sqrMagnitude > 0.001f)
            {
                lastRHand = new MotionPointSimple { position = wp.pos_RCont.ToVector3(), rotation = wp.rot_RCont.ToQuaternion() };
            }
            rContMotionPoints.Add(lastRHand);

            if (wp.pos_LHand.ToVector3().sqrMagnitude > 0.001f)
            {
                lastLHand = new MotionPointSimple { position = wp.pos_LHand.ToVector3(), rotation = wp.rot_LHand.ToQuaternion() };
            }
            lHandMotionPoints.Add(lastLHand);

            if (wp.pos_RHand.ToVector3().sqrMagnitude > 0.001f)
            {
                lastRHand = new MotionPointSimple { position = wp.pos_RHand.ToVector3(), rotation = wp.rot_RHand.ToQuaternion() };
            }
            rHandMotionPoints.Add(lastRHand);
        }

        if (btn == null) btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(ReturnData);
        return (rContMotionPoints.Count.ToString());
    }

    public void ReturnData()
    {
        ReplayData data = new ReplayData
        {
            hmdMotionPoints = this.hmdMotionPoints,
            lContMotionPoints = this.lContMotionPoints,
            rContMotionPoints = this.rContMotionPoints,
            lHandMotionPoints = this.lHandMotionPoints,
            rHandMotionPoints = this.rHandMotionPoints,
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
    public List<MotionPointSimple> lContMotionPoints;
    public List<MotionPointSimple> rContMotionPoints;
    public List<MotionPointSimple> lHandMotionPoints;
    public List<MotionPointSimple> rHandMotionPoints;
}

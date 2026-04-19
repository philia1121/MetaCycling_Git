//using Meta.XR.Editor.Tags;
using Oculus.Interaction.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class FitnessTestManager : MonoBehaviour
{
    [Header("Tracked Obj data")]
    [SerializeField] private GameObject trackedGameObject;

    [SerializeField] private GameObject hmdGameObject;
    [SerializeField] private GameObject rightArmGameObject;
    [SerializeField] private float toleranceDist = 0.05f;               //max distance the user can be from the established points
    private SphereCollider hmdColl;

    [Header("Fitness Position Data")]
    [SerializeField] private float positionCalibrationTime = 2f;        //how long the user have to stop in a certain position
    [SerializeField] private float calibrationSampleInterval = 0.05f;   //sample taken for the calibration

    [SerializeField] private float fitnessSampleDistance = 0.03f;       //distance each "point" hav to be to add a new point
    [Range(0, 1)]
    [SerializeField] private float fitnessSampledCompletionRate = 0.6f; //min number of points the user have to go through before counted as 1

    [Header("Prefab Variables")]
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private GameObject pointPrefabStart;
    [SerializeField] private GameObject pointPrefabEnd;
    [SerializeField] private GameObject planePrefabStart;
    [SerializeField] private GameObject planePrefabEnd;

    [Header("Display Variables")]
    [SerializeField] private TMP_Text startTxt;
    [SerializeField] private TMP_Text endTxt;
    [SerializeField] private TMP_Text jumpTxt;
    [SerializeField] private TMP_Text ptsTxt;
    [SerializeField] private TMP_Text etcTxt;

    private List<MotionPoint> posSamples = new List<MotionPoint>();     //for the calibration
    private List<MotionPoint> moveSamples = new List<MotionPoint>();    //for the movement between the 2 points
    
    private List<GameObject> instantiatedObj = new List<GameObject>();  //temp list to store the instantiated obj, ensure no overlapping point

    //var for the tracked GO
    private Vector3 lastTrackedPos;
    private bool isRecording = false;
    private bool isLastHitStart;
    private int point;
    private int exerciseCount;

    private Vector3 jumpedPos;

    //to save the spawned GO, reuse them
    private GameObject spawnedStartJumpPoint;
    private GameObject spawnedEndJumpPoint;
    private GameObject spawnedStartJumpPlane;
    private GameObject spawnedEndJumpPlane;

    //var for the calibrated start and end point
    public Vector3 calibratedStartPos;
    public Vector3 calibratedEndPos;

    TrajectoryRecorder trajectoryRecorder;

    ControlMap controlMap;
    private PathVisualizer path;
    public static FitnessTestManager instance;

    void Awake()
    {
        trajectoryRecorder = TrajectoryRecorder.instance;

        controlMap = new ControlMap();
        controlMap.Prototype.Enable();

        if (instance == null)
            instance = this;


        #region jump dist buttons and calc
        //spawn the planes n calc
        controlMap.Prototype.Right_Trigger.started += ctx => 
        {
            return;
            SpawnJumpPlane(ref spawnedStartJumpPlane, planePrefabStart);

            if (spawnedStartJumpPlane == null && spawnedEndJumpPlane == null)
                return;

            float dist = CalcDist(spawnedStartJumpPlane.transform.position, spawnedEndJumpPlane.transform.position);
            etcTxt.text = $"measured dist is {dist *100} cm";
        };

        controlMap.Prototype.Right_Grip.started += ctx =>
        {
            return;
            SpawnJumpPlane(ref spawnedEndJumpPlane, planePrefabEnd);

            if (spawnedStartJumpPlane == null && spawnedEndJumpPlane == null)
                return;

            float dist = CalcDist(spawnedStartJumpPlane.transform.position, spawnedEndJumpPlane.transform.position);
            etcTxt.text = $"measured dist is {dist * 100} cm";
        };

        //end jump
        controlMap.Prototype.X.started += ctx => {
            jumpedPos = Vector3.zero;
            jumpedPos = trackedGameObject.transform.position;

            spawnedEndJumpPoint = CheckInstantiatedObject(spawnedEndJumpPoint, pointPrefabEnd, new Vector3(jumpedPos.x, 0, jumpedPos.z), Quaternion.identity);

            float res = CalcDist(new Vector3(jumpedPos.x, 0, jumpedPos.z), new Vector3(calibratedStartPos.x, 0, calibratedStartPos.z));

            float highestPoint = 0;
            //float lowestPoint = 0;
            foreach (var point in moveSamples)
            {
                if (point.position.y > highestPoint)
                    highestPoint = point.position.y;
            }

            jumpTxt.text = $"jumped {res * 100} cm, highest {(highestPoint - calibratedStartPos.y) * 100} cm";

            //if still recording, end it
            if (isRecording)
                Record(false);

            trajectoryRecorder.StopRecording();
            path.EndRecording();

            path.DisplayPath();
            etcTxt.text = "Displaying path";
        };

        #endregion


        //swap tracking
        //controlMap.Prototype.Left_Trigger.started += ctx => SwapTracked();

        //determine start, then start to record
        controlMap.Prototype.A.started += ctx => StartCoroutine(CalibratePos(result =>
        {
            ClearTrackingData();

            calibratedStartPos = result;
            Debug.Log("Start Pos: " + result);
            etcTxt.text = "start pos calibrated";
            lastTrackedPos = result;

            spawnedStartJumpPoint = CheckInstantiatedObject(spawnedStartJumpPoint, pointPrefabStart, new Vector3(result.x, 0, result.z), Quaternion.identity);
          
            //add 1st pos
            MotionPoint point = new MotionPoint
            {
                position = result,
                rotation = trackedGameObject.transform.rotation,
                timestamp = Time.time,
                velocity = Vector3.zero
            };

            moveSamples.Add(point);
            Record(true);

            trajectoryRecorder.StartRecording();
            path.StartRecording();

            //basically call the function to record the movement,n/x
            //so the track can be calc'd after the start point is calibrated
        }));

        //record manually for now unused
        //controlMap.Prototype.B.started += ctx => {
        //    Record(true);

        //    trajectoryRecorder.StopRecording();
        //    path.EndRecording();
        //    path.DisplayPath();
        //};

        //record end pos
        controlMap.Prototype.Y.started += ctx =>
        {
            path.DisplayTrailingPath();
        };

        controlMap.Prototype.Left_Grip.started += ctx => ClearTrackingData();
    }

    private GameObject CheckInstantiatedObject(GameObject objVar, GameObject objPrefab, Vector3 pos, Quaternion rot)
    {

        if (objVar == null)
            objVar = Instantiate(objPrefab, pos, rot);
        else
        {
            objVar.transform.position = pos;
            objVar.SetActive(true);
        }

        return objVar;
    }

    private void Start()
    {
        hmdColl = trackedGameObject.GetComponent<SphereCollider>();
        if( hmdColl != null )
            hmdColl.radius = toleranceDist;
    }

    private void Update()
    {
        if (trajectoryRecorder == null)
            trajectoryRecorder = TrajectoryRecorder.instance;
        if (path == null)
            path = PathVisualizer.instance;

        if (isRecording)
        {
            RecordMovement();
        }

        startTxt.text = $"start pos{(calibratedStartPos == Vector3.zero ? "<b> not </b>" : " ")}set";
        endTxt.text = $"end pos{(calibratedEndPos == Vector3.zero ? "<b> not </b>" : " ")}set";

        if (calibratedStartPos == Vector3.zero || calibratedEndPos == Vector3.zero)
            return;

        #region 2 points of movement logic

        Vector3 currPos = trackedGameObject.transform.position;

        float minDist = .05f;
        //jumpTxt.text = $"toStr {Mathf.Abs(CalcDist(currPos, calibratedStartPos))}, toEd {Mathf.Abs(CalcDist(currPos, calibratedEndPos))}, {minDist}";

        if (!isLastHitStart && Mathf.Abs(CalcDist(currPos, calibratedStartPos)) <= minDist)
        {
            isLastHitStart = true;
            //etcTxt.text = $"hits start";
            CalcScore();
        }

        if (isLastHitStart && Mathf.Abs(CalcDist(currPos, calibratedEndPos)) <= minDist)
        {
            isLastHitStart= false;
            //etcTxt.text = $"hits end";
            CalcScore();
        }
        #endregion;
    }

    private void Record(bool spawnEndPoint)
    {
        //clear the instantiated stuff
        foreach(var obj in instantiatedObj)
        {
            Destroy(obj.gameObject);
        }
        if (!spawnEndPoint)
            moveSamples.Clear();

        isRecording = !isRecording;
        etcTxt.text = $"{(isRecording ? "started" : "ended")} recording";

        if (isRecording)
            return;

        if (!spawnEndPoint)
            return;

        StartCoroutine(CalibratePos(result =>
        {
            if (calibratedStartPos == Vector3.zero)
            {
                etcTxt.text = $" start position is not set, press A";
                return;
            }
            
            calibratedEndPos = result;
            etcTxt.text = "end pos calibrated";

            //add basically last pos
            MotionPoint point = new MotionPoint
            {
                position = result,
                rotation = trackedGameObject.transform.rotation,
                timestamp = Time.time,
                velocity = Vector3.zero
            };

            moveSamples.Add(point);

            //this is to enable the rep counting
            isLastHitStart = true;

            InstantiatePoints();
        }));


    }
    private void SwapTracked()
    {
        trackedGameObject = trackedGameObject == hmdGameObject
            ? rightArmGameObject
            : hmdGameObject;
        etcTxt.text = $"Tracking: {(trackedGameObject == hmdGameObject ? "head" : "right hand")}, tag: {trackedGameObject.tag}";

        trackedGameObject.transform.localScale = new Vector3(toleranceDist, toleranceDist, toleranceDist);
        SphereCollider c = trackedGameObject.GetComponent<SphereCollider>();
        c.radius = toleranceDist;

    }

    public IEnumerator CalibratePos(Action<Vector3> callback)
    {
        float timer = 0f;

        posSamples.Clear();

        while (timer < positionCalibrationTime)
        {
            float remaining = positionCalibrationTime - timer;
            etcTxt.text = $"Hold still: {remaining:F1}s";

            MotionPoint point = new MotionPoint
            {
                position = trackedGameObject.transform.position,
                rotation = trackedGameObject.transform.rotation,
                timestamp = Time.time - timer,
                velocity = Vector3.zero
            };

            posSamples.Add(point);

            timer += calibrationSampleInterval;
            yield return new WaitForSeconds(calibrationSampleInterval);
        }

        Vector3 result = CalculateAverage(posSamples);
        callback?.Invoke(result);
    }

    private Vector3 CalculateAverage(List<MotionPoint> pt)
    {
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < pt.Count; i++)
        {
            sum += pt[i].position;
        }

        return sum / pt.Count;
    }
    private void SpawnJumpPlane(ref GameObject obj, GameObject prefab)
    {
        var (pos, rot) = GetPosition();

        if (pos == Vector3.zero)
        {
            etcTxt.text = "Floor undetected";
            return;
        }

        obj = CheckInstantiatedObject(obj, prefab, pos, rot);
    }

    private void RecordMovement()
    {
        //save to list if the position changes around 2-3 cm
        Vector3 currentPos = trackedGameObject.transform.position;
        if (MathF.Abs(CalcDist(currentPos, lastTrackedPos)) < fitnessSampleDistance)
            return;

        MotionPoint point = new MotionPoint
        {
            position = currentPos,
            rotation = trackedGameObject.transform.rotation,
            timestamp = Time.time,
            velocity = Vector3.zero
        };

        moveSamples.Add(point);
        lastTrackedPos = currentPos;
    }

    private void InstantiatePoints()
    {
        int ptCount = 0;
        //instantiate the points
        foreach (var point in moveSamples)
        {
            GameObject g = null;
            if (ptCount == 0)
                g = Instantiate(pointPrefabStart, point.position, point.rotation);
            else if (ptCount == moveSamples.Count - 1)
                g = Instantiate(pointPrefabEnd, point.position, point.rotation);
            else
                g = Instantiate(pointPrefab, point.position, point.rotation);


            var relay = g.GetComponent<ColliderEventRelay>();

            //code what the collider is supposed to do
            if (relay != null)
            {
                relay.OnObjHit += OnPointHit;

                instantiatedObj.Add(g);
                ptCount++;
            }
        }

        etcTxt.text = $"spawned {ptCount} balls";
    }

    //adds point, disables the point

    private void OnPointHit(GameObject obj, bool isDestroyable)
    {
        point++;

        //etcTxt.text = $"point: {point}/{moveSamples.Count}";

        if (!isDestroyable)
            return;
        obj.SetActive(false);
    }

    private void CalcScore()
    {
        float ratio = (float)point / instantiatedObj.Count;

        if (ratio >= fitnessSampledCompletionRate)
            exerciseCount++;

        //etcTxt.text = $"{instantiatedObj.Count}, {point} / {instantiatedObj.Count}, {ratio} >= {fitnessSampledCompletionRate}, {(ratio >= fitnessSampledCompletionRate ? "add" : "not")}";
        point = 0;

        //reactivate em all
        foreach (var obj in instantiatedObj)
        {
            if (!obj.activeSelf)
                obj.SetActive(true);
        }

        etcTxt.text = $"did {MathF.Floor(exerciseCount / 2)} reps";
    }

    private float CalcDist(Vector3 start, Vector3 end)
    {
        float sqDist = (end - start).sqrMagnitude;
        return (MathF.Sqrt(sqDist));
    }

    public void ClearTrackingData()
    {
        posSamples.Clear();
        moveSamples.Clear();
        calibratedStartPos = Vector3.zero;
        calibratedEndPos = Vector3.zero;

        //clear the instantiated stuff
        foreach (var obj in instantiatedObj)
        {
            Destroy(obj.gameObject);
        }
        instantiatedObj.Clear();
        exerciseCount = 0;

        if (spawnedStartJumpPlane != null) spawnedEndJumpPlane.SetActive(false);
        if (spawnedEndJumpPlane != null) spawnedEndJumpPlane.SetActive(false);
        if (spawnedStartJumpPoint != null) spawnedStartJumpPoint.SetActive(false);
        if (spawnedEndJumpPoint != null) spawnedEndJumpPoint.SetActive(false);

        path.ClearMotionData();

        etcTxt.text = "data cleared";
    }

    //shoot beam out of hands, spawn a plane 
    private (Vector3, Quaternion) GetPosition()
    {
        Ray ray = new Ray(rightArmGameObject.transform.position, rightArmGameObject.transform.forward);
        //RaycastHit hit;
        Plane floorPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        Vector3 hitPoint;

        if (floorPlane.Raycast(ray, out distance))
        {
            hitPoint = ray.GetPoint(distance);
            Vector3 controllerEuler = rightArmGameObject.transform.eulerAngles;
            Quaternion spawnRot = Quaternion.Euler(0f, controllerEuler.y, 0f);

            return (hitPoint, spawnRot);
        }
        return (Vector3.zero, Quaternion.identity);
    }

    public void StartMovement(Action<bool> onComplete)
    {
        StartCoroutine(CalibratePos(result =>
        {
            ClearTrackingData();

            calibratedStartPos = result;
            Debug.Log("Start Pos: " + result);
            etcTxt.text = "start pos calibrated";
            lastTrackedPos = result;

            spawnedStartJumpPoint = CheckInstantiatedObject(spawnedStartJumpPoint, pointPrefabStart, new Vector3(result.x, 0, result.z), Quaternion.identity);

            //add 1st pos
            MotionPoint point = new MotionPoint
            {
                position = result,
                rotation = trackedGameObject.transform.rotation,
                timestamp = Time.time,
                velocity = Vector3.zero
            };

            moveSamples.Add(point);
            Record(true);

            trajectoryRecorder.StartRecording();
            path.StartRecording();

            onComplete?.Invoke(true);
            //basically call the function to record the movement,n/x
            //so the track can be calc'd after the start point is calibrated
        }));
    }

    public JumpResult EndMovement()
    {
        jumpedPos = Vector3.zero;
        jumpedPos = trackedGameObject.transform.position;

        spawnedEndJumpPoint = CheckInstantiatedObject(spawnedEndJumpPoint, pointPrefabEnd, new Vector3(jumpedPos.x, 0, jumpedPos.z), Quaternion.identity);

        float res = CalcDist(new Vector3(jumpedPos.x, 0, jumpedPos.z), new Vector3(calibratedStartPos.x, 0, calibratedStartPos.z));

        float highestPoint = 0;
        //float lowestPoint = 0;
        foreach (var point in moveSamples)
        {
            if (point.position.y > highestPoint)
                highestPoint = point.position.y;
        }

        jumpTxt.text = $"jumped {res * 100} cm, highest {(highestPoint - calibratedStartPos.y) * 100} cm";

        //if still recording, end it
        if (isRecording)
            Record(false);

        trajectoryRecorder.StopRecording();
        path.EndRecording();

        path.DisplayPath();
        etcTxt.text = "Displaying path";

        return new JumpResult
        {
            success = true,
            distance = res * 100,
            height = (highestPoint - calibratedStartPos.y) * 100
        };
    }
    
}

public struct JumpResult
{
    public bool success;
    public float distance;
    public float height;
}

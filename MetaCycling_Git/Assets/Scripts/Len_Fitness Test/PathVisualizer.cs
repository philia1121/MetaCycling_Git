using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PathVisualizer : MonoBehaviour
{
    public static PathVisualizer instance;
    private bool isRecording;

    [SerializeField] private float interval;
    [SerializeField] private float playbackSpeed = 1f;

    [Header("GameObject Refs")]
    public Transform HMD_Transform;
    public Transform LHand_Transform, RHand_Transform;

    [Header("Prefab Refs")]
    [SerializeField] private Transform hmdPlayback;
    [SerializeField] private Transform lHandPlayback;
    [SerializeField] private Transform rHandPlayback;

    [SerializeField] private Transform hmdPlaybackTrail;
    [SerializeField] private Transform lHandPlaybackTrail;
    [SerializeField] private Transform rHandPlaybackTrail;


    private List<MotionPointSimple> hmdMotionPoints;
    private List<MotionPointSimple> rHandMotionPoints;
    private List<MotionPointSimple> lHandMotionPoints;

    private List<GameObject> hmdMotionPath;
    private List<GameObject> rHandMotionPath;
    private List<GameObject> lHandMotionPath;

    private Coroutine sampleRoutine;
    private Coroutine loopRoutineHead, loopRoutineLHand, loopRoutineRHand;

    private bool isDisplayingTrailingPath = true;
    private int displayID = 0;
    private int displayAmount = 10;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        hmdMotionPoints = new List<MotionPointSimple>();
        rHandMotionPoints = new List<MotionPointSimple>();
        lHandMotionPoints = new List<MotionPointSimple>();

        hmdMotionPath = new List<GameObject>();
        lHandMotionPath = new List<GameObject>();
        rHandMotionPath = new List<GameObject>();
    }
    private void Start()
    {
        PlaybackObjSetActive(false);
    }

    public void StartRecording()
    {
        if (isRecording)
            return;
        
        ClearMotionData();
        isRecording = true;
        sampleRoutine = StartCoroutine(SampleMotion());
    }

    public void EndRecording()
    {
        if (!isRecording)
            return;
        isRecording = false;
        StopCoroutine(sampleRoutine);
    }

    public void DisplayPath()
    {
        PlaybackObjSetActive(true);

        if (hmdMotionPoints.Count <= 0 || lHandMotionPoints.Count <= 0 || rHandMotionPoints.Count <= 0)
            return;

        loopRoutineHead = StartCoroutine(PlayPathLoop(hmdMotionPoints, hmdPlayback));
        loopRoutineLHand = StartCoroutine(PlayPathLoop(lHandMotionPoints, lHandPlayback));
        loopRoutineRHand = StartCoroutine(PlayPathLoop(rHandMotionPoints, rHandPlayback));
    }

    public void DisplayTrailingPath()
    {
        displayID++;
        if (displayID >= 5) displayID = 1;

        if (displayID == 1)
        {
            isDisplayingTrailingPath = true;
            displayAmount = 10;
        }
        if (displayID == 2)
        {
            isDisplayingTrailingPath = true;
            displayAmount = 20;
        }
        if(displayID == 3)
        {
            isDisplayingTrailingPath = true;
            displayAmount = -5;
        }
        if (displayID == 4)
        {
            isDisplayingTrailingPath = false;

            if (isDisplayingTrailingPath)
                return;
            foreach (GameObject g in hmdMotionPath)
                g.SetActive(false);
            foreach (GameObject b in lHandMotionPath)
                b.SetActive(false);
            foreach (GameObject c in rHandMotionPath)
                c.SetActive(false);
        }
        
    }

    IEnumerator SampleMotion()
    {
        float lastSampleTime = Time.time;

        while (true)
        {
            float now = Time.time;
            float deltaTime = now - lastSampleTime;
            lastSampleTime = now;

            if (deltaTime > 0f)
            {
                AddMotion(hmdMotionPoints, HMD_Transform.position, HMD_Transform.rotation);
                AddMotion(lHandMotionPoints, LHand_Transform.position, LHand_Transform.rotation);
                AddMotion(rHandMotionPoints, RHand_Transform.position, RHand_Transform.rotation);
            }

            CreateHiddenMarker(hmdMotionPath, hmdPlaybackTrail, HMD_Transform);
            CreateHiddenMarker(lHandMotionPath, lHandPlaybackTrail, LHand_Transform);
            CreateHiddenMarker(rHandMotionPath, rHandPlaybackTrail, RHand_Transform);

            yield return new WaitForSeconds(interval);
        }
    }

    private void AddMotion(List<MotionPointSimple> list, Vector3 pos, Quaternion rot)
    {
        MotionPointSimple point = new MotionPointSimple
        {
            position = pos,
            rotation = rot,
        };
        list.Add(point);
    }
    private void CreateHiddenMarker(List<GameObject> list,Transform prefab, Transform source)
    {

        Transform marker = Instantiate(prefab, source.position, source.rotation);
        //marker.GetComponent<Nametag>().isDisplayName = false;
        marker.localScale = prefab.localScale * 0.5f;

        // Hide it immediately so it doesn't clutter the recording view
        marker.gameObject.SetActive(false);

        list.Add(marker.gameObject);
    }

    private void ActivateHiddenMarker(List<GameObject> list, int startIndex, int windowSize)
    {
        if (!isDisplayingTrailingPath)
            return;

        //set this to view everything
        if(startIndex<0 && windowSize < 0)
        {
            foreach (GameObject g in list)
            {
                g.SetActive(true);
            }
            return;
        }

        //view certain numbers
        if (startIndex < 0)
            startIndex = 0;
        for (int i = 0; i < list.Count; i++)
        {
            bool shouldBeActive = (i >= startIndex && i < startIndex + windowSize);

            if (list[i].activeSelf != shouldBeActive)
            {
                list[i].SetActive(shouldBeActive);
            }
        }
    }

    private IEnumerator PlayPathLoop(List<MotionPointSimple> path, Transform target)
    {
        while (true) // loop forever
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                MotionPointSimple start = path[i];
                MotionPointSimple end = path[i + 1];

                int markerIndex = i / 2;

                ActivateHiddenMarker(hmdMotionPath, displayAmount/2, displayAmount);
                ActivateHiddenMarker(lHandMotionPath, displayAmount/2, displayAmount);
                ActivateHiddenMarker(rHandMotionPath, displayAmount/2, displayAmount);

                float t = 0f;

                while (t < 1f)
                {
                    t += Time.deltaTime * playbackSpeed;

                    target.position = Vector3.Lerp(start.position, end.position, t);
                    target.rotation = Quaternion.Slerp(start.rotation, end.rotation, t);

                    yield return null;
                }
            }
        }
    }

    public void ClearMotionData()
    {
        hmdMotionPoints.Clear();
        lHandMotionPoints.Clear();
        rHandMotionPoints.Clear();

        hmdMotionPoints = new List<MotionPointSimple>();
        rHandMotionPoints = new List<MotionPointSimple>();
        lHandMotionPoints = new List<MotionPointSimple>();

        foreach (GameObject g in hmdMotionPath) Destroy(g);
        foreach (GameObject g in lHandMotionPath) Destroy(g);
        foreach (GameObject g in rHandMotionPath) Destroy(g);

        hmdMotionPath = new List<GameObject>();
        lHandMotionPath = new List<GameObject>();
        rHandMotionPath = new List<GameObject>();

        if (loopRoutineHead!=null) StopCoroutine(loopRoutineHead);
        if (loopRoutineLHand!= null) StopCoroutine(loopRoutineLHand);
        if (loopRoutineRHand != null) StopCoroutine(loopRoutineRHand);

        PlaybackObjSetActive(false);
    }

    private void PlaybackObjSetActive(bool active)
    {
        hmdPlayback.gameObject.SetActive(active);
        lHandPlayback.gameObject.SetActive(active);
        rHandPlayback.gameObject.SetActive(active);
    }
}

[Serializable]
public struct MotionPointSimple
{
    public Vector3 position;
    public Quaternion rotation;
}

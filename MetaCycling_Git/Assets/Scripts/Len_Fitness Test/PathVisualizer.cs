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

    [Header("Ghost Prefab Refs")]
    [SerializeField] private Transform hmdGhost;
    [SerializeField] private Transform lHandGhost;
    [SerializeField] private Transform rHandGhost;

    [Header("Cone")]
    [SerializeField] private Transform PlaybackTrail;

    [Header("Line Renderers")]
    [SerializeField] private bool useLines = true;
    [SerializeField] private LineRenderer hmdLine;
    [SerializeField] private LineRenderer lHandLine;
    [SerializeField] private LineRenderer rHandLine;

    [Header("Ghost Line Renderers")]
    [SerializeField] private LineRenderer hmdghostLine;
    [SerializeField] private LineRenderer lHandGhostLine;
    [SerializeField] private LineRenderer rHandGhostLine;

    [Header("Visibility Settings")]
    [Range(1, 4)]
    [SerializeField] private int pathDensity = 1;

    private List<MotionPointSimple> hmdMotionPoints;
    private List<MotionPointSimple> rHandMotionPoints;
    private List<MotionPointSimple> lHandMotionPoints;

    private List<GameObject> hmdMotionPath;
    private List<GameObject> rHandMotionPath;
    private List<GameObject> lHandMotionPath;

    private Coroutine sampleRoutine;
    private Coroutine loopRoutineHead, loopRoutineLHand, loopRoutineRHand;
    private Coroutine loopGhostRoutineHead, loopGhostRoutineLHand, loopGhostRoutineRHand;

    private bool isDisplayingTrailingPath = true;
    public int displayID = 1;
    private int displayAmount = 10;
    private int currentPlaybackIndex = 0;

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
        PlaybackObjSetActive(false, true);
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

    public void OnDensityChanged(float newValue)
    {
        pathDensity = Mathf.RoundToInt(newValue);

        // Refresh the visuals for all 3 limbs immediately
        ActivateHiddenMarker(hmdMotionPath, currentPlaybackIndex, displayAmount);
        ActivateHiddenMarker(lHandMotionPath, currentPlaybackIndex, displayAmount);
        ActivateHiddenMarker(rHandMotionPath, currentPlaybackIndex, displayAmount);
    }


    public void DisplayPath()
    {
        StopAllPlayback(false); // Stop standard playback
        PlaybackObjSetActive(true, false);

        if (hmdMotionPoints.Count <= 0 || lHandMotionPoints.Count <= 0 || rHandMotionPoints.Count <= 0)
            return;

        loopRoutineHead = StartCoroutine(PlayPathLoop(hmdMotionPoints, hmdMotionPath, hmdPlayback, hmdLine));
        loopRoutineLHand = StartCoroutine(PlayPathLoop(lHandMotionPoints, lHandMotionPath, lHandPlayback, lHandLine));
        loopRoutineRHand = StartCoroutine(PlayPathLoop(rHandMotionPoints, rHandMotionPath, rHandPlayback, rHandLine));
    }

    public void DisplayGhostPath(ReplayData data)
    {
        StopAllPlayback(true); // Stop existing ghost playback
        PlaybackObjSetActive(true, true);

        loopGhostRoutineHead = StartCoroutine(PlayPathLoop(data.hmdMotionPoints, null, hmdGhost, hmdghostLine));
        loopGhostRoutineLHand = StartCoroutine(PlayPathLoop(data.lHandMotionPoints, null, lHandGhost, lHandGhostLine));
        loopGhostRoutineRHand = StartCoroutine(PlayPathLoop(data.rHandMotionPoints, null, rHandGhost, rHandGhostLine));
    }

    public void DisplayTrailingPath()
    {
        displayID++;
        if (displayID > 3) displayID = 1;

        if (displayID == 1)
        {
            isDisplayingTrailingPath = true;
            displayAmount = 20;
            Debug.Log("Mode: Trailing Tail");
        }
        else if (displayID == 2)
        {
            isDisplayingTrailingPath = true;
            displayAmount = -1; // Special flag for 'All'
            Debug.Log("Mode: Show All");
        }
        else if (displayID == 3)
        {
            isDisplayingTrailingPath = false;
            HideAllMarkers();
            Debug.Log("Mode: Hide All");
        }
    }

    private void HideAllMarkers()
    {
        foreach (GameObject g in hmdMotionPath) g.SetActive(false);
        foreach (GameObject g in lHandMotionPath) g.SetActive(false);
        foreach (GameObject g in rHandMotionPath) g.SetActive(false);
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

            CreateHiddenMarker(hmdMotionPath, PlaybackTrail, HMD_Transform);
            CreateHiddenMarker(lHandMotionPath, PlaybackTrail, LHand_Transform);
            CreateHiddenMarker(rHandMotionPath, PlaybackTrail, RHand_Transform);

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

    private void CreateHiddenMarker(List<GameObject> list, Transform prefab, Transform source)
    {
        Quaternion combinedRotation = source.rotation * prefab.rotation;

        Transform marker = Instantiate(prefab, source.position, combinedRotation);
        marker.localScale = prefab.localScale * 0.5f;

        // Hide it immediately so it doesn't clutter the recording view
        marker.gameObject.SetActive(false);

        list.Add(marker.gameObject);
    }

    private void ActivateHiddenMarker(List<GameObject> list, int currentIndex, int windowSize)
    {
        if (!isDisplayingTrailingPath)
        {
            foreach (GameObject g in list) g.SetActive(false);
            return;
        }

        //view certain numbers
        int start = (windowSize < 0) ? 0 : currentIndex - windowSize;

        for (int i = 0; i < list.Count; i++)
        {
            bool isInWindow = (windowSize < 0) || (i >= start && i <= currentIndex);
            bool matchesDensity = (i % pathDensity == 0);
            bool shouldBeActive = isInWindow && matchesDensity;

            if (list[i] != null && list[i].activeSelf != shouldBeActive)
            {
                list[i].SetActive(shouldBeActive);
            }

        }
    }

    private IEnumerator PlayPathLoop(List<MotionPointSimple> path, List<GameObject> motionPath, Transform target, LineRenderer line)
    {
        while (true) // loop forever
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                MotionPointSimple start = path[i];
                MotionPointSimple end = path[i + 1];

                currentPlaybackIndex = i;

                if (motionPath != null) ActivateHiddenMarker(motionPath, i, displayAmount);
                UpdateLine(line, path, i, displayAmount);

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

    private void UpdateLine(LineRenderer line, List<MotionPointSimple> points, int currentIndex, int windowSize)
    {
        if (line == null || !useLines) return;

        // Mode: Show All
        if (windowSize < 0)
        {
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                line.SetPosition(i, points[i].position);
            }
        }
        // Mode: Trailing Tail
        else
        {
            int start = Mathf.Max(0, currentIndex - windowSize);
            int count = currentIndex - start + 1;

            line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                line.SetPosition(i, points[start + i].position);
            }
        }
    }

    public void ClearMotionData()
    {
        StopAllPlayback(false);
        StopAllPlayback(true);

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

        PlaybackObjSetActive(false, true);
    }

    private void StopAllPlayback(bool ghostsOnly)
    {
        if (ghostsOnly)
        {
            if (loopGhostRoutineHead != null) StopCoroutine(loopGhostRoutineHead);
            if (loopGhostRoutineLHand != null) StopCoroutine(loopGhostRoutineLHand);
            if (loopGhostRoutineRHand != null) StopCoroutine(loopGhostRoutineRHand);
        }
        else
        {
            if (loopRoutineHead != null) StopCoroutine(loopRoutineHead);
            if (loopRoutineLHand != null) StopCoroutine(loopRoutineLHand);
            if (loopRoutineRHand != null) StopCoroutine(loopRoutineRHand);
        }
    }

    private void PlaybackObjSetActive(bool active, bool ghost)
    {
        hmdPlayback.gameObject.SetActive(active);
        lHandPlayback.gameObject.SetActive(active);
        rHandPlayback.gameObject.SetActive(active);

        if (!ghost)
            return;

        hmdGhost.gameObject.SetActive(active);
        lHandGhost.gameObject.SetActive(active);
        rHandGhost.gameObject.SetActive(active);
    }
}
    



    [Serializable]
    public struct MotionPointSimple
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    public static PathVisualizer instance;
    public bool isRecording { get; private set; }

    [SerializeField] private float interval;
    [SerializeField] private float playbackSpeed = 1f;
    public float speedMult = 1f;

    [Header("GameObject Refs")]
    public Transform HMD_Transform;
    public Transform LCont_Transform, RCont_Transform; 
    public Transform LHand_Transform, RHand_Transform;

    [Header("Prefab Refs")]
    [SerializeField] private GameObject nomrPlaybackPar;
    [SerializeField] private Transform hmdPlayback;
    [SerializeField] private Transform lContPlayback;
    [SerializeField] private Transform lHandPlayback;
    [SerializeField] private Transform rContPlayback;
    [SerializeField] private Transform rHandPlayback;

    [Header("Ghost Prefab Refs")]
    [SerializeField] private GameObject ghostPlaybackPar;
    [SerializeField] private Transform hmdGhost;
    [SerializeField] private Transform lContGhost;
    [SerializeField] private Transform lHandGhost;
    [SerializeField] private Transform rContGhost;
    [SerializeField] private Transform rHandGhost;

    [Header("trail parent")]
    [SerializeField] private Transform hmdTrailFolder;
    [SerializeField] private Transform lContTrailFolder;
    [SerializeField] private Transform rContTrailFolder;
    [SerializeField] private Transform lHandTrailFolder;
    [SerializeField] private Transform rHandTrailFolder;

    [Header("mesh")]
    [SerializeField] private GameObject[] disableMesh;

    [Header("Cone")]
    [SerializeField] private Transform PlaybackTrail;

    [Header("Line Renderers")]
    [SerializeField] private bool useLines = true;
    [SerializeField] private LineRenderer hmdLine;
    [SerializeField] private LineRenderer lContLine;
    [SerializeField] private LineRenderer lHandLine;
    [SerializeField] private LineRenderer rContLine;
    [SerializeField] private LineRenderer rHandLine;

    [Header("Ghost Line Renderers")]
    [SerializeField] private LineRenderer hmdghostLine;
    [SerializeField] private LineRenderer lContGhostLine;
    [SerializeField] private LineRenderer lHandGhostLine;
    [SerializeField] private LineRenderer rContGhostLine;
    [SerializeField] private LineRenderer rHandGhostLine;

    [Header("Visibility Settings")]
    [Range(1, 4)]
    [SerializeField] private int pathDensity = 1;

    public List<MotionPointSimple> hmdMotionPoints;
    public List<MotionPointSimple> lContMotionPoints;
    public List<MotionPointSimple> rContMotionPoints; 
    public List<MotionPointSimple> lHandMotionPoints; 
    public List<MotionPointSimple> rHandMotionPoints;

    private List<GameObject> hmdMotionPath;
    private List<GameObject> lContMotionPath;
    private List<GameObject> rContMotionPath; 
    private List<GameObject> lHandMotionPath;
    private List<GameObject> rHandMotionPath;

    private Coroutine sampleRoutine;
    private Coroutine loopRoutineHead, loopRoutineLCont, loopRoutineRCont, loopRoutineLHand, loopRoutineRHand;
    private Coroutine loopGhostRoutineHead, loopGhostRoutineLCont, loopGhostRoutineRCont, loopGhostRoutineLHand, loopGhostRoutineRHand;

    private bool isHidden;

    private bool isDisplayingTrailingPath = true;
    private bool handsActiveSelf = true;
    public int displayID = 1;
    private int displayAmount = 10;
    private int currentPlaybackIndex = 0;
    public float currentPlaybackTimeRaw { get; private set; }
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        hmdMotionPoints = new List<MotionPointSimple>();
        lContMotionPoints = new List<MotionPointSimple>();
        rContMotionPoints = new List<MotionPointSimple>();
        lHandMotionPoints = new List<MotionPointSimple>();
        rHandMotionPoints = new List<MotionPointSimple>();

        hmdMotionPath = new List<GameObject>();
        lContMotionPath = new List<GameObject>();
        rContMotionPath = new List<GameObject>();
        lHandMotionPath = new List<GameObject>();
        rHandMotionPath = new List<GameObject>();
    }
    private void Start()
    {
        PlaybackObjSetActive(false, true);
    }

    #region start and end recording
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
                AddMotion(lContMotionPoints, LCont_Transform.position, LCont_Transform.rotation);
                AddMotion(rContMotionPoints, RCont_Transform.position, RCont_Transform.rotation);
                AddMotion(lHandMotionPoints, LHand_Transform.position, LHand_Transform.rotation);
                AddMotion(rHandMotionPoints, RHand_Transform.position, RHand_Transform.rotation);
            }

            CreateHiddenMarker(hmdMotionPath, PlaybackTrail, HMD_Transform, hmdTrailFolder);
            CreateHiddenMarker(lContMotionPath, PlaybackTrail, LCont_Transform, lContTrailFolder);
            CreateHiddenMarker(rContMotionPath, PlaybackTrail, RCont_Transform, rContTrailFolder);
            CreateHiddenMarker(lHandMotionPath, PlaybackTrail, LHand_Transform, lHandTrailFolder);
            CreateHiddenMarker(rHandMotionPath, PlaybackTrail, RHand_Transform, rHandTrailFolder);

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

    private void CreateHiddenMarker(List<GameObject> list, Transform prefab, Transform source, Transform parent)
    {
        Quaternion combinedRotation = source.rotation * prefab.rotation;

        Transform marker = Instantiate(prefab, source.position, combinedRotation, parent);
        marker.localScale = prefab.localScale * 0.5f;

        // Hide it immediately so it doesn't clutter the recording view
        marker.gameObject.SetActive(false);

        list.Add(marker.gameObject);
    }
    #endregion

    #region trailing changes
    public void OnDensityChanged(float newValue)
    {
        pathDensity = Mathf.RoundToInt(newValue);

        // Refresh the visuals for all 3 limbs immediately
        ActivateHiddenMarker(hmdMotionPath, currentPlaybackIndex, displayAmount);
        ActivateHiddenMarker(lContMotionPath, currentPlaybackIndex, displayAmount);
        ActivateHiddenMarker(rContMotionPath, currentPlaybackIndex, displayAmount);
        ActivateHiddenMarker(lHandMotionPath, currentPlaybackIndex, displayAmount);
        ActivateHiddenMarker(rHandMotionPath, currentPlaybackIndex, displayAmount);
    }
    public void DisplayPath()
    {
        StopAllPlayback(false); // Stop standard playback
        PlaybackObjSetActive(true, false);

        if (hmdMotionPoints.Count <= 0 || lContMotionPoints.Count <= 0 || rContMotionPoints.Count <= 0)
            return;

        loopRoutineHead = StartCoroutine(PlayPathLoop(hmdMotionPoints, hmdMotionPath, hmdPlayback, hmdLine));
        loopRoutineLCont = StartCoroutine(PlayPathLoop(lContMotionPoints, lContMotionPath, lContPlayback, lContLine));
        loopRoutineRCont = StartCoroutine(PlayPathLoop(rContMotionPoints, rContMotionPath, rContPlayback, rContLine));

        loopRoutineLHand = StartCoroutine(PlayPathLoop(lHandMotionPoints, lHandMotionPath, lHandPlayback, lHandLine));
        loopRoutineRHand = StartCoroutine(PlayPathLoop(rHandMotionPoints, rHandMotionPath, rHandPlayback, rHandLine));
    }

    public void DisplayGhostPath(ReplayData data)
    {
        StopAllPlayback(true); // Stop existing ghost playback
        PlaybackObjSetActive(true, true);

        loopGhostRoutineHead = StartCoroutine(PlayPathLoop(data.hmdMotionPoints, null, hmdGhost, hmdghostLine));
        loopGhostRoutineLCont = StartCoroutine(PlayPathLoop(data.lContMotionPoints, null, lContGhost, lContGhostLine));
        loopGhostRoutineRCont = StartCoroutine(PlayPathLoop(data.rContMotionPoints, null, rContGhost, rContGhostLine));

        loopGhostRoutineLHand = StartCoroutine(PlayPathLoop(data.lHandMotionPoints, null, lHandGhost, lHandGhostLine)); 
        loopGhostRoutineRHand = StartCoroutine(PlayPathLoop(data.rHandMotionPoints, null, rHandGhost, rHandGhostLine));
    }    
    
    public void DisplayTrailingPath(int _i)
    {
        displayID = (displayID % 3) + 1;

        if (displayID == 1)
        {
            UpdatePathDisplayMode(true, 20, "Mode: Trailing Tail");
        }
        else if (displayID == 2)
        {
            UpdatePathDisplayMode(true, -1, "Mode: Show All");
        }
        else if (displayID == 3)
        {
            UpdatePathDisplayMode(false, 0, "Mode: Hide All");
        }
    }

    private void UpdatePathDisplayMode(bool trailingPathActive, int windowSize, string logMessage)
    {
        isDisplayingTrailingPath = trailingPathActive;
        displayAmount = windowSize;
        Debug.Log(logMessage);

        SetAllMarkersActive(trailingPathActive);
    }

    private void SetAllMarkersActive(bool _b)
    {
        foreach (GameObject g in hmdMotionPath) g.SetActive(_b);
        foreach (GameObject g in lContMotionPath) g.SetActive(_b);
        foreach (GameObject g in rContMotionPath) g.SetActive(_b);
        foreach (GameObject g in lHandMotionPath) g.SetActive(_b);
        foreach (GameObject g in rHandMotionPath) g.SetActive(_b);
    }
    #endregion
    //its long as fuck because my eyes hort looking at it without breaklines
    public void SetReplayVisibility(bool visible)
    {
        isHidden = visible;
        hmdPlayback.gameObject.SetActive(visible);
        hmdLine.gameObject.SetActive(visible);
        hmdTrailFolder.gameObject.SetActive(visible);

        lContPlayback.gameObject.SetActive(visible);
        lContLine.gameObject.SetActive(visible);
        lContTrailFolder.gameObject.SetActive(visible);

        rContPlayback.gameObject.SetActive(visible);
        rContLine.gameObject.SetActive(visible);
        rContTrailFolder.gameObject.SetActive(visible);

        if (handsActiveSelf)
        {
            lHandPlayback.gameObject.SetActive(visible);
            lHandLine.gameObject.SetActive(visible);
            lHandTrailFolder.gameObject.SetActive(visible);

            rHandPlayback.gameObject.SetActive(visible);
            rHandLine.gameObject.SetActive(visible);
            rHandTrailFolder.gameObject.SetActive(visible);
        }

        hmdGhost.gameObject.SetActive(visible);
        hmdghostLine.gameObject.SetActive(visible);

        lContGhost.gameObject.SetActive(visible);
        lContGhostLine.gameObject.SetActive(visible);

        rContGhost.gameObject.SetActive(visible);
        rContGhostLine.gameObject.SetActive(visible);

        if (handsActiveSelf)
        {
            lHandGhost.gameObject.SetActive(visible);
            lHandGhostLine.gameObject.SetActive(visible);

            rHandGhost.gameObject.SetActive(visible);
            rHandGhostLine.gameObject.SetActive(visible);
        }
    }

    private void ActivateHiddenMarker(List<GameObject> list, int currentIndex, int windowSize)
    {
        if (displayID == 3 || !isDisplayingTrailingPath)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].activeSelf)
                    list[i].SetActive(false);
            }
            return;
        }

        if (windowSize < 0)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    // Show based on density (casting density to int if it's a float)
                    bool matchesDensity = (i % pathDensity == 0);
                    list[i].SetActive(matchesDensity);
                }
            }
            return;
        }

        //view certain numbers
        bool isRewinding = speedMult < 0;
        int start = isRewinding ? currentIndex : Mathf.Max(0, currentIndex - windowSize);
        int end = isRewinding ? Mathf.Min(list.Count - 1, currentIndex + windowSize) : currentIndex;


        for (int i = 0; i < list.Count; i++)
        {
            bool isInWindow = (i >= start && i <= end);
            bool matchesDensity = (i % pathDensity == 0) || (i == currentIndex);

            bool shouldBeActive = isInWindow && matchesDensity;

            if (list[i] != null && list[i].activeSelf != shouldBeActive)
            {
                list[i].SetActive(shouldBeActive);
            }
        }
    }

    private IEnumerator PlayPathLoop(List<MotionPointSimple> path, List<GameObject> motionPath, Transform target, LineRenderer line, float startAtFrame = 0f)
    {
        if (path == null || path.Count == 0 || target == null) yield break;

        // jump to direct point of time
        float _play = startAtFrame;
        float _totPts = path.Count - 1;

        while (true) // loop forever
        {
            _play += Time.deltaTime * ((1/interval)*.5f) * speedMult; //only change speedMult so it dont fuck up anything

            //clamp playhead
            if (_play > _totPts) _play = 0;
            if (_play < 0) _play = _totPts;

            if (motionPath == hmdMotionPath)
            {
                currentPlaybackTimeRaw = (_play/ _totPts);
            }

            int currentIndex = Mathf.FloorToInt(_play);
            int nextIndex = (currentIndex + 1) % path.Count;

            float t = _play - currentIndex; // The fractional part (0.0 to 1.0)

            // Apply Movement
            target.position = Vector3.Lerp(path[currentIndex].position, path[nextIndex].position, t);
            target.rotation = Quaternion.Slerp(path[currentIndex].rotation, path[nextIndex].rotation, t);

            // Update visuals
            currentPlaybackIndex = currentIndex;
            UpdateLine(line, path, currentIndex, displayAmount);

            if (motionPath != null && motionPath.Count > 0)
            {
                ActivateHiddenMarker(motionPath, currentIndex, displayAmount);
            }

            yield return null;
        }
    }

    private void UpdateLine(LineRenderer line, List<MotionPointSimple> points, int currentIndex, int windowSize)
    {
        if (line == null || !useLines) return;

        // Mode: Show All
        if (windowSize <= 0)
        {
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
                line.SetPosition(i, points[i].position);
            return;
        }

        // Mode: Trailing Tail
        bool isRewinding = speedMult < 0;

        int start = isRewinding ? currentIndex : Mathf.Max(0, currentIndex - windowSize);
        int end = isRewinding ? Mathf.Min(points.Count - 1, currentIndex + windowSize) : currentIndex;

        int count = end - start + 1;
        line.positionCount = count;

        for (int i = 0; i < count; i++)
        {
            line.SetPosition(i, points[start + i].position);
        }
    }
    public void ClearMotionData()
    {
        StopAllPlayback(false);
        StopAllPlayback(true);

        hmdMotionPoints.Clear();
        lContMotionPoints.Clear();
        rContMotionPoints.Clear();
        lHandMotionPoints.Clear();
        rHandMotionPoints.Clear();

        hmdMotionPoints = new List<MotionPointSimple>();
        lContMotionPoints = new List<MotionPointSimple>();
        rContMotionPoints = new List<MotionPointSimple>();
        lHandMotionPoints = new List<MotionPointSimple>();
        rHandMotionPoints = new List<MotionPointSimple>();

        foreach (GameObject g in hmdMotionPath) if (g != null) Destroy(g);
        foreach (GameObject g in lContMotionPath) if (g != null) Destroy(g);
        foreach (GameObject g in rContMotionPath) if (g != null) Destroy(g);
        foreach (GameObject g in lHandMotionPath) if (g != null) Destroy(g);
        foreach (GameObject g in rHandMotionPath) if (g != null) Destroy(g);

        hmdMotionPath = new List<GameObject>();
        lContMotionPath = new List<GameObject>();
        rContMotionPath = new List<GameObject>();
        lHandMotionPath = new List<GameObject>();
        rHandMotionPath = new List<GameObject>();

        PlaybackObjSetActive(false, true);
    }

    private void StopAllPlayback(bool ghostsOnly)
    {
        if (ghostsOnly)
        {
            if (loopGhostRoutineHead != null) StopCoroutine(loopGhostRoutineHead);
            if (loopGhostRoutineLCont != null) StopCoroutine(loopGhostRoutineLCont);
            if (loopGhostRoutineRCont != null) StopCoroutine(loopGhostRoutineRCont);
            if (loopGhostRoutineLHand != null) StopCoroutine(loopGhostRoutineLHand);
            if (loopGhostRoutineRHand != null) StopCoroutine(loopGhostRoutineRHand);
        }
        else
        {
            if (loopRoutineHead != null) StopCoroutine(loopRoutineHead);
            if (loopRoutineLCont != null) StopCoroutine(loopRoutineLCont);
            if (loopRoutineRCont != null) StopCoroutine(loopRoutineRCont);
            if (loopRoutineLHand != null) StopCoroutine(loopRoutineLHand);
            if (loopRoutineRHand != null) StopCoroutine(loopRoutineRHand);
        }
    }

    private void PlaybackObjSetActive(bool active, bool ghost)
    {
        if (!isHidden)
            return;

        hmdPlayback.gameObject.SetActive(active);
        lContPlayback.gameObject.SetActive(active);
        rContPlayback.gameObject.SetActive(active);
        lHandPlayback.gameObject.SetActive(active);
        rHandPlayback.gameObject.SetActive(active);

        if (!ghost)
            return;

        hmdGhost.gameObject.SetActive(active);
        lContGhost.gameObject.SetActive(active);
        rContGhost.gameObject.SetActive(active);
        lHandGhost.gameObject.SetActive(active);
        rHandGhost.gameObject.SetActive(active);
    }

    public void PlaybackMeshObjSetActive(bool active)
    {
        if (disableMesh.Length <= 0)
            return;
        foreach(GameObject g in disableMesh)
        {
            g.SetActive(active);
        }
    }

    public void SetHandsVisibility(bool visible)
    {
        handsActiveSelf = visible;
        // 1. Turn off the actual playback avatars
        if (lHandPlayback != null) lHandPlayback.gameObject.SetActive(visible);
        if (rHandPlayback != null) rHandPlayback.gameObject.SetActive(visible);
        if (lHandGhost != null) lHandGhost.gameObject.SetActive(visible);
        if (rHandGhost != null) rHandGhost.gameObject.SetActive(visible);

        // 2. Disabling the parent transforms instantly hides all nested child trails!
        if (lHandTrailFolder != null) lHandTrailFolder.gameObject.SetActive(visible);
        if (rHandTrailFolder != null) rHandTrailFolder.gameObject.SetActive(visible);

        // 3. Toggle line components
        if (lHandLine != null) lHandLine.enabled = visible;
        if (rHandLine != null) rHandLine.enabled = visible;
        if (lHandGhostLine != null) lHandGhostLine.enabled = visible;
        if (rHandGhostLine != null) rHandGhostLine.enabled = visible;
    }

    public void ScrubToTimePercentage(float normalizedTime)
    {
        if (hmdMotionPoints == null || hmdMotionPoints.Count == 0) return;

        float totalPoints = hmdMotionPoints.Count - 1;
        float targetFrame = Mathf.Clamp(normalizedTime, 0f, 1f) * totalPoints;

        StopAllPlayback(false);

        if (displayID != 3 && isDisplayingTrailingPath)
        {
            int targetIndex = Mathf.FloorToInt(targetFrame);
            ActivateHiddenMarker(hmdMotionPath, targetIndex, displayAmount);
            ActivateHiddenMarker(lContMotionPath, targetIndex, displayAmount);
            ActivateHiddenMarker(rContMotionPath, targetIndex, displayAmount);
            ActivateHiddenMarker(lHandMotionPath, targetIndex, displayAmount);
            ActivateHiddenMarker(rHandMotionPath, targetIndex, displayAmount);
        }

        loopRoutineHead = StartCoroutine(PlayPathLoop(hmdMotionPoints, hmdMotionPath, hmdPlayback, hmdLine, targetFrame));
        loopRoutineLCont = StartCoroutine(PlayPathLoop(lContMotionPoints, lContMotionPath, lContPlayback, lContLine, targetFrame));
        loopRoutineRCont = StartCoroutine(PlayPathLoop(rContMotionPoints, rContMotionPath, rContPlayback, rContLine, targetFrame));
        loopRoutineLHand = StartCoroutine(PlayPathLoop(lHandMotionPoints, lHandMotionPath, lHandPlayback, lHandLine, targetFrame));
        loopRoutineRHand = StartCoroutine(PlayPathLoop(rHandMotionPoints, rHandMotionPath, rHandPlayback, rHandLine, targetFrame));
    }
}

    [Serializable]
    public struct MotionPointSimple
    {
        public Vector3 position;
        public Quaternion rotation;
    }

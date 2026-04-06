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

    private List<MotionPointSimple> hmdMotionPoints;
    private List<MotionPointSimple> rHandMotionPoints;
    private List<MotionPointSimple> lHandMotionPoints;

    private Coroutine sampleRoutine;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        hmdMotionPoints = new List<MotionPointSimple>();
        rHandMotionPoints = new List<MotionPointSimple>();
        lHandMotionPoints = new List<MotionPointSimple>();
    }
    private void Start()
    {
        PlaybackObjSetActive(false);
    }
    private void Update()
    {

    }

    public void StartRecording()
    {
        if (isRecording)
            return;
        
        PlaybackObjSetActive(false);
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

        StartCoroutine(PlayPathLoop(hmdMotionPoints, hmdPlayback));
        StartCoroutine(PlayPathLoop(lHandMotionPoints, lHandPlayback));
        StartCoroutine(PlayPathLoop(rHandMotionPoints, rHandPlayback));
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

    private IEnumerator PlayPathLoop(List<MotionPointSimple> path, Transform target)
    {
        while (true) // loop forever
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                MotionPointSimple start = path[i];
                MotionPointSimple end = path[i + 1];

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

    private void ClearMotionData()
    {
        hmdMotionPoints.Clear();
        lHandMotionPoints.Clear();
        rHandMotionPoints.Clear();

        hmdMotionPoints = new List<MotionPointSimple>();
        rHandMotionPoints = new List<MotionPointSimple>();
        lHandMotionPoints = new List<MotionPointSimple>();
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

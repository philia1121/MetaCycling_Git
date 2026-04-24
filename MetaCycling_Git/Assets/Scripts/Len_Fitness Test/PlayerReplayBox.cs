using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReplayBox : MonoBehaviour
{
    [SerializeField] private Transform statueHead;
    [SerializeField] private Transform statueLHand;
    [SerializeField] private Transform statueRHand;

    [Header("Settings")]
    public float playbackSpeed = 60f;
    [Tooltip("The interval used during recording (e.g., 0.015)")]
    public float recordingInterval = 0.015f;

    private float playhead = 0f;
    private bool isPlaying = false;

    // Data references
    private List<MotionPointSimple> hmdData;
    private List<MotionPointSimple> leftData;
    private List<MotionPointSimple> rightData;

    private PathVisualizer m_Path;
    public static PlayerReplayBox instance;

    private void Awake()
    {
        if(instance==null) instance = this;
    }

    private void Start()
    {
        m_Path = PathVisualizer.instance;
    }

    public void StartReplay(List<MotionPointSimple> head, List<MotionPointSimple> left, List<MotionPointSimple> right)
    {
        hmdData = head;
        leftData = left;
        rightData = right;
        playhead = 0f;
        isPlaying = true;

        statueHead.gameObject.SetActive(true);
        statueLHand.gameObject.SetActive(true);
        statueRHand.gameObject.SetActive(true);
    }

    public void EndReplay()
    {
        hmdData.Clear();
        leftData.Clear();
        rightData.Clear();
        playhead = 0f;
        isPlaying = false;

        statueHead.gameObject.SetActive(false);
        statueLHand.gameObject.SetActive(false);
        statueRHand.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isPlaying || hmdData == null || hmdData.Count < 2) return;

        // 1. Advance Playhead
        float pointsPerSecond = 1f / recordingInterval;
        playhead += Time.deltaTime * pointsPerSecond * m_Path.speedMult;

        if (playhead >= hmdData.Count - 1) playhead = 0;

        int curr = Mathf.FloorToInt(playhead);
        int next = (curr + 1) % hmdData.Count;
        float t = playhead - curr;

        // 2. Process "Stationary" Movement
        // We treat the Head as the center (Vector3.zero) 
        // and calculate where the hands are RELATIVE to that head.
        UpdateGhostPart(statueHead, hmdData, curr, next, t, true);
        UpdateGhostPart(statueLHand, leftData, curr, next, t, false, hmdData);
        UpdateGhostPart(statueRHand, rightData, curr, next, t, false, hmdData);
    }

    private void UpdateGhostPart(Transform ghostPart, List<MotionPointSimple> partData, int curr, int next, float t, bool isHead, List<MotionPointSimple> headRef = null)
    {
        // Interpolate World Positions/Rotations from data
        Vector3 worldPos = Vector3.Lerp(partData[curr].position, partData[next].position, t);
        Quaternion worldRot = Quaternion.Slerp(partData[curr].rotation, partData[next].rotation, t);

        if (isHead)
        {
            // The head stays at the center of the box, but rotates
            ghostPart.localPosition = Vector3.zero;
            ghostPart.localRotation = worldRot;
        }
        else
        {
            // Calculate where the head was at this exact moment in the recording
            Vector3 headWorldPos = Vector3.Lerp(headRef[curr].position, headRef[next].position, t);
            Quaternion headWorldRot = Vector3.Lerp(headRef[curr].position, headRef[next].position, t) == headWorldPos ? headRef[curr].rotation : Quaternion.Slerp(headRef[curr].rotation, headRef[next].rotation, t);

            // MAGIC STEP: Get the relative offset
            // We find the vector from head to hand, then rotate that vector 
            // by the inverse of the head's rotation so the ghost doesn't "spin" 
            // when the player turns their head.
            Vector3 relativePos = worldPos - headWorldPos;

            // Apply to ghost hand
            ghostPart.localPosition = relativePos;
            ghostPart.localRotation = worldRot;
        }
    }
}

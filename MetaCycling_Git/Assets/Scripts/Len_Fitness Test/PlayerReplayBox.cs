using Oculus.Interaction.PoseDetection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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
    private List<MotionPointSimple> hmdData = new List<MotionPointSimple>();
    private List<MotionPointSimple> leftData = new List<MotionPointSimple>();
    private List<MotionPointSimple> rightData = new List<MotionPointSimple>();

    private Coroutine loopMovement; 

    private PathVisualizer m_Path;
    public static PlayerReplayBox instance;

    private void Awake()
    {
        if(instance==null) instance = this;
    }

    private void Start()
    {
        m_Path = PathVisualizer.instance;
        //EnableAllGameObject(false);
    }
    private void FixedUpdate()
    {
        // ONLY call Start if we aren't already playing and recording has stopped
        if (!m_Path.isRecording && m_Path.hmdMotionPoints.Count > 2 && m_Path.lHandMotionPoints.Count > 2 && m_Path.rHandMotionPoints.Count > 2)
        {
            if (!isPlaying) // THE GUARD
            {
                StartReplay(m_Path.hmdMotionPoints, m_Path.lHandMotionPoints, m_Path.rHandMotionPoints);
            }
        }

        // ONLY call End if we are currently playing but a new recording started
        if (m_Path.isRecording && isPlaying)
        {
            EndReplay();
        }
    }

    public void StartReplay(List<MotionPointSimple> head, List<MotionPointSimple> left, List<MotionPointSimple> right)
    {
        playhead = 0f;
        isPlaying = true;
        EnableAllGameObject(true);
        RecalcMotionPoints(head, left, right);

        loopMovement = StartCoroutine(PlayPathLoop());
    }

    public void EndReplay()
    {
        playhead = 0f;
        isPlaying = false;
        hmdData.Clear();
        leftData.Clear();
        rightData.Clear();
        EnableAllGameObject(false);

        StopCoroutine(loopMovement );
    }

    private IEnumerator PlayPathLoop()
    {
        float _play = 0f; //serves as the pointer on what's playing now
        float _totPts = hmdData.Count - 1;

        while (true) // loop forever
        {
            _play += Time.deltaTime * ((1 / recordingInterval) * .5f) * m_Path.speedMult; //only change speedMult so it dont fuck up anything

            //clamp playhead
            if (_play > _totPts) _play = 0;
            if (_play < 0) _play = _totPts;

            int currentIndex = Mathf.FloorToInt(_play);
            int nextIndex = (int)((currentIndex + 1) % _totPts);

            float t = _play - currentIndex; // The fractional part (0.0 to 1.0)

            // Move Head (Relative to the ReplayBox)
            statueHead.localPosition = Vector3.Lerp(hmdData[currentIndex].position, hmdData[nextIndex].position, t);
            statueHead.localRotation = Quaternion.Slerp(hmdData[currentIndex].rotation, hmdData[nextIndex].rotation, t);

            // Move Hands (Relative to the Head)
            statueLHand.localPosition= Vector3.Lerp(leftData[currentIndex].position, leftData[nextIndex].position, t);
            statueLHand.localRotation= Quaternion.Slerp(leftData[currentIndex].rotation, leftData[nextIndex].rotation, t);

            statueRHand.localPosition= Vector3.Lerp(rightData[currentIndex].position, rightData[nextIndex].position, t);
            statueRHand.localRotation= Quaternion.Slerp(rightData[currentIndex].rotation, rightData[nextIndex].rotation, t);

            yield return null;
        }
    }
    private void RecalcMotionPoints(List<MotionPointSimple> head, List<MotionPointSimple> left, List<MotionPointSimple> right)
    {
        hmdData = new List<MotionPointSimple>();
        leftData = new List<MotionPointSimple>();
        rightData = new List<MotionPointSimple>();

        Vector3 statueScale = new Vector3(0.05f, 0.05f, 0.05f);

        for (int i = 0; i < head.Count; i++)
        {
            // 1. Head stays at 0,y,0 relative to the Replay Box
            hmdData.Add(new MotionPointSimple
            {
                position = new Vector3(0, head[i].position.y, 0),
                rotation = head[i].rotation
            });

            // 2. Matrix TRS now includes the SCALE of your root object
            Matrix4x4 headMatrix = Matrix4x4.TRS(head[i].position, head[i].rotation, statueScale);

            leftData.Add(new MotionPointSimple
            {
                // No changes needed here if the matrix is correct, 
                // but MultiplyPoint3x4 will now return 'statue-sized' offsets
                position = headMatrix.inverse.MultiplyPoint3x4(left[i].position),
                rotation = Quaternion.Inverse(head[i].rotation) * left[i].rotation
            });

            rightData.Add(new MotionPointSimple
            {
                position = headMatrix.inverse.MultiplyPoint3x4(right[i].position),
                rotation = Quaternion.Inverse(head[i].rotation) * right[i].rotation
            });
        }
        //for (int i = 0; i < head.Count; i++)
        //{
        //    Vector3 headOffset = head[i].position;

        //    hmdData.Add(new MotionPointSimple
        //    {
        //        position = new Vector3(0, head[i].position.y, 0),
        //        rotation = head[i].rotation

        //    });

        //    3.Subtract that head offset from the hands
        //    leftData.Add(new MotionPointSimple
        //    {
        //        position = left[i].position,
        //        rotation = left[i].rotation
        //    });



        //    rightData.Add(new MotionPointSimple
        //    {
        //        position = right[i].position,
        //        rotation = right[i].rotation
        //    });

        //    Matrix4x4 headMatrix = Matrix4x4.TRS(head[i].position, head[i].rotation, Vector3.one);

        //    leftData.Add(new MotionPointSimple
        //    {
        //        position = headMatrix.inverse.MultiplyPoint3x4(left[i].position),
        //        rotation = Quaternion.Inverse(head[i].rotation) * left[i].rotation
        //    });

        //    rightData.Add(new MotionPointSimple
        //    {
        //        position = headMatrix.inverse.MultiplyPoint3x4(right[i].position),
        //        rotation = Quaternion.Inverse(head[i].rotation) * right[i].rotation
        //    });
        //}
    }

    //private void RecalcMotionPoints(List<MotionPointSimple> head, List<MotionPointSimple> left, List<MotionPointSimple> right)
    //{
    //    hmdData.Clear();
    //    leftData.Clear();
    //    rightData.Clear();

    //    for (int i = 0; i < head.Count; i++)
    //    {
    //        Vector3 headOffset = head[i].position;

    //        hmdData.Add(new MotionPointSimple
    //        {
    //            position = new Vector3(0, head[i].position.y, 0),
    //            rotation = head[i].rotation
    //        });

    //        // 3. Subtract that head offset from the hands 
    //        leftData.Add(new MotionPointSimple
    //        {
    //            position = left[i].position - headOffset,
    //            rotation = left[i].rotation
    //        });

    //        rightData.Add(new MotionPointSimple
    //        {
    //            position = right[i].position - headOffset,
    //            rotation = right[i].rotation
    //        });
    //    }
    //}

    private void EnableAllGameObject(bool enable)
    {
        statueHead.gameObject.SetActive(enable);
        statueLHand.gameObject.SetActive(enable);
        statueRHand.gameObject.SetActive(enable);
    }

}

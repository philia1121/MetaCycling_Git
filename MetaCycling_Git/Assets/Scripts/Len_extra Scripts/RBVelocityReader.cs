using Oculus.Interaction.UnityCanvas;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RBVelocityReader : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject target;
    public Vector3 Velocity { get; private set; }                   //setting this public so its easily viewable

    [Header("Graph Settings")]
    [SerializeField] private int maxSamples = 30;
    [SerializeField] private float maxVelocity = 20f;               //expected max velocity
    [SerializeField] private GameObject canvastGameObj;             //canvas GameObject
    
    //store canvas values 
    private RectTransform canvasRect;     
    private float graphHeight;
    private float graphWidth;      

    [Header("Sampling")]
    [SerializeField] private float sampleInterval = 0.02f;          //roughly almost the same as FixedUpdate

    //sampling variables
    private UILineRenderer lineRenderer;
    private float[] samples;
    private int sampleIndex;
    private Vector3 lastPos;

    private Coroutine samplingRoutine;                              //coroutine to make it easier to control outside the script
    private bool hasLast = true;                                    //please call this if it losses track

    void Awake()
    {
        //get canvas width and height
        canvasRect = canvastGameObj.GetComponent<RectTransform>();
        graphWidth = canvasRect.sizeDelta.x;
        graphHeight = canvasRect.sizeDelta.y;

        //setup the required vars for UILineRenderer
        lineRenderer = canvastGameObj.GetComponent<UILineRenderer>();
        samples = new float[maxSamples];
    }

    private void Start()
    {
        Invoke("StartGraph", .5f);
    }

    #region Public Functions

    public void StartGraph()
    {
        if (samplingRoutine != null)
            return;

        samplingRoutine = StartCoroutine(SampleVelocity());
    }

    public void StopGraph()
    {
        if (samplingRoutine == null)
            return;

        StopCoroutine(samplingRoutine);
        samplingRoutine = null;
    }

    public void ClearGraph()
    {
        for (int i = 0; i < samples.Length; i++)
            samples[i] = 0f;

        sampleIndex = 0;
        UpdateGraph();
    }

    public void UpdateControllerInfo(bool _b)
    {
        hasLast = _b;
    }

    #endregion

    IEnumerator SampleVelocity()
    {
        float lastSampleTime = Time.time;

        while (true)
        {
            if (target)
            {
                float now = Time.time;
                float deltaTime = now - lastSampleTime;
                lastSampleTime = now;

                Vector3 currPos = target.transform.position;

                if (!hasLast)
                {
                    lastPos = currPos;
                    hasLast = true;
                    Velocity = Vector3.zero;
                }
                else if (deltaTime > 0f)
                {
                    Velocity = (currPos - lastPos) / deltaTime;
                    lastPos = currPos;

                    samples[sampleIndex] = Velocity.magnitude;
                    sampleIndex = (sampleIndex + 1) % maxSamples;

                    UpdateGraph();
                }
            }

            yield return new WaitForSeconds(sampleInterval);
        }
    }

    void UpdateGraph()
    {
        Vector2[] points = new Vector2[maxSamples];
        float xStep = graphWidth / (maxSamples - 1);

        for (int i = 0; i < maxSamples; i++)
        {
            int index = (sampleIndex + i) % maxSamples;
            float value = samples[index];

            float normalized = Mathf.Clamp01(value / maxVelocity);
            float y = normalized * graphHeight;

            points[i] = new Vector2(i * xStep, y);
        }

        //redraw line renderer use setverticesdirty so its a little bit lighter
        lineRenderer.points = points;
        lineRenderer.SetVerticesDirty();
    }
}


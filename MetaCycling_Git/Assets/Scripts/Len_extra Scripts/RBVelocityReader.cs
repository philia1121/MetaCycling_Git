using Oculus.Interaction.UnityCanvas;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RBVelocityReader : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject target;
    public Vector3 velocity { get; private set; }                   //setting this public so its easily viewable
    public Vector3 acceleration { get; private set; }                   //setting this public so its easily viewable
    private Vector3 smoothedVelocity;

    [Header("Graph Settings")]
    [SerializeField] private int maxSamples = 30;
    [SerializeField] private float maxVelocity = 20f;               //expected max velocity
    [SerializeField] private float maxAccel = 5f;                  //expected max acceleration

    [Header("Velocity Line Renderer")]
    [SerializeField] private AxisGraph lineRendererVelocityX;
    [SerializeField] private AxisGraph lineRendererVelocityY;
    [SerializeField] private AxisGraph lineRendererVelocityZ;
    
    [Header("Acceleration Line Renderer")]
    [SerializeField] private AxisGraph lineRendererAccelX;
    [SerializeField] private AxisGraph lineRendererAccelY;
    [SerializeField] private AxisGraph lineRendererAccelZ;   

    [Header("Sampling")]
    [SerializeField] private float sampleInterval = 0.02f;          //roughly almost the same as FixedUpdate
    [SerializeField] private float velocitySmoothing = 0.15f;

    //sampling variables                
    private Vector3[] velocitySamples;                              //save the velocity samples for drawing
    private Vector3[] accelerationSamples;                          //save the accel samples for drawing
    private int sampleIndex;

    private Vector3 lastPos;
    private Vector3 lastVelocity;
    private bool hasLast = true;                                    //please call this if it losses track

    private Coroutine samplingRoutine;                              //coroutine to make it easier to control outside the script
    void Awake()
    {
        CacheRect(lineRendererVelocityX);
        CacheRect(lineRendererVelocityY);
        CacheRect(lineRendererVelocityZ);

        CacheRect(lineRendererAccelX);
        CacheRect(lineRendererAccelY);
        CacheRect(lineRendererAccelZ);

        //setup the required vars for UILineRenderer
        velocitySamples = new Vector3[maxSamples];
        accelerationSamples = new Vector3[maxSamples];
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

        samplingRoutine = StartCoroutine(SampleMotion());
    }

    public void StopGraph()
    {
        if (samplingRoutine == null)
            return;

        StopCoroutine(samplingRoutine);
        samplingRoutine = null;
    }

    public void UpdateControllerInfo(bool _b)
    {
        hasLast = _b;
    }

    #endregion

    IEnumerator SampleMotion()
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
                    lastVelocity = Vector3.zero;
                    smoothedVelocity = Vector3.zero;
                    hasLast = true;
                }
                else if (deltaTime > 0f)
                {
                    Vector3 rawVelocity = (currPos - lastPos) / deltaTime;
                    smoothedVelocity = Vector3.Lerp(smoothedVelocity, rawVelocity, velocitySmoothing);

                    acceleration = (smoothedVelocity - lastVelocity) / deltaTime;

                    lastVelocity = smoothedVelocity;
                    velocity = smoothedVelocity;
                    lastPos = currPos;

                    velocitySamples[sampleIndex] = velocity;
                    accelerationSamples[sampleIndex] = acceleration;

                    sampleIndex = (sampleIndex + 1) % maxSamples;

                    DrawAllGraphs();
                }
            }

            yield return new WaitForSeconds(sampleInterval);
        }
    }

    #region Visualization

    void DrawAllGraphs()
    {
        DrawAxisGraph(lineRendererVelocityX, velocitySamples, v => v.x, maxVelocity);
        DrawAxisGraph(lineRendererVelocityY, velocitySamples, v => v.y, maxVelocity);
        DrawAxisGraph(lineRendererVelocityZ, velocitySamples, v => v.z, maxVelocity);

        DrawAxisGraph(lineRendererAccelX, accelerationSamples, a => a.x, maxAccel);
        DrawAxisGraph(lineRendererAccelY, accelerationSamples, a => a.y, maxAccel);
        DrawAxisGraph(lineRendererAccelZ, accelerationSamples, a => a.z, maxAccel);
    }

    void DrawAxisGraph(
        AxisGraph _axis,
        Vector3[] source,
        System.Func<Vector3, float> selector,
        float maxValue)
    {

        Vector2[] points = new Vector2[maxSamples];
        float xStep = _axis.rect.sizeDelta.x / (maxSamples - 1);

        for (int i = 0; i < maxSamples; i++)
        {
            int index = (sampleIndex + i) % maxSamples;
            float value = selector(source[index]);

            float normalized = Mathf.InverseLerp(-maxValue, maxValue, value);
            float y = normalized * _axis.rect.sizeDelta.y;

            points[i] = new Vector2(i * xStep, y);
        }

        _axis.line.points = points;
        _axis.line.SetVerticesDirty();
    }

    #endregion

    public void CacheRect(AxisGraph g)
    {
        if (g.line != null)
        {
            g.rect = g.line.gameObject.GetComponent<RectTransform>();
        }
    }
}

[System.Serializable]
public class AxisGraph
{
    public UILineRenderer line;
    [HideInInspector] public RectTransform rect;
}
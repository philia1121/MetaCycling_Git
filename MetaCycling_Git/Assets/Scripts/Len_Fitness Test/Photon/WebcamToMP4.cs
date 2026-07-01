using System.Collections;
using System.IO;
using UnityEngine;

// Only compile the Recorder assemblies inside the Unity Editor
#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
#endif

public class WebcamToMP4 : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.RawImage targetRawImage;

    private WebCamTexture m_WebcamTexture;
    private RenderTexture m_RecordingRenderTexture;
    private bool m_IsRecording = false;

    public static WebcamToMP4 instance;

#if UNITY_EDITOR
    private RecorderController m_RecorderController;
    private MovieRecorderSettings m_Settings = null;
#endif

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    public void PreviewWebcam(string webcamName)
    {
        // Shuts down existing texture streams cleanly if switching sources
        if (m_WebcamTexture != null)
        {
            m_WebcamTexture.Stop();
        }

        m_WebcamTexture = new WebCamTexture(webcamName, 1920, 1080, 30);

        if (targetRawImage != null)
            targetRawImage.texture = m_WebcamTexture;

        m_WebcamTexture.Play();
    }

    public void StartWebcamAndRecording(string webcamName, string targetFileName)
    {
        // If the webcam isn't running yet or we are switching, boot it up cleanly
        if (m_WebcamTexture == null || m_WebcamTexture.deviceName != webcamName)
        {
            PreviewWebcam(webcamName);
        }

#if UNITY_EDITOR
        m_RecordingRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        m_RecordingRenderTexture.Create();

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        m_RecorderController = new RecorderController(controllerSettings);

        var mediaOutputFolder = new DirectoryInfo(Path.Combine(Application.dataPath, "..", "SampleRecordings"));

        // Video Settings Definition
        m_Settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        m_Settings.name = "Blitted Webcam Recorder";
        m_Settings.Enabled = true;

        m_Settings.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4
        };
        m_Settings.CaptureAlpha = false;

        // Link the asset directly to our RenderTextureInput settings wrapper
        m_Settings.ImageInputSettings = new RenderTextureInputSettings
        {
            RenderTexture = m_RecordingRenderTexture
        };

        m_Settings.OutputFile = Path.Combine(mediaOutputFolder.FullName, targetFileName);

        controllerSettings.AddRecorderSettings(m_Settings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60.0f;

        RecorderOptions.VerboseMode = false;
        m_RecorderController.PrepareRecording();
        m_RecorderController.StartRecording();

        // Flip the flag to let Update start processing frames
        m_IsRecording = true;

        Debug.Log($"Blit recording pipeline active: {m_Settings.OutputFile}.mp4");
#else
        Debug.LogWarning("MP4 Recording engine is only operational inside the Unity Editor layout environment.");
#endif
    }

    private void Update()
    {
        if (m_IsRecording && m_WebcamTexture != null && m_WebcamTexture.didUpdateThisFrame && m_RecordingRenderTexture != null)
        {
            Graphics.Blit(m_WebcamTexture, m_RecordingRenderTexture);
        }
    }

    public void StopRecording()
    {
        m_IsRecording = false;

#if UNITY_EDITOR
        if (m_RecorderController != null)
        {
            m_RecorderController.StopRecording();
            m_RecorderController = null;
        }
#endif

        if (m_WebcamTexture != null)
        {
            m_WebcamTexture.Stop();
            m_WebcamTexture = null;
        }

        if (m_RecordingRenderTexture != null)
        {
            m_RecordingRenderTexture.Release();
            m_RecordingRenderTexture = null;
        }
    }

    void OnDisable()
    {
        StopRecording();
    }
}

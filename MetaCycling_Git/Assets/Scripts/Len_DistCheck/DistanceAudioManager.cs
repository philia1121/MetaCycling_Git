using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceAudioManager : MonoBehaviour
{
    [Header("bgm vars")]
    [SerializeField]private AudioSource bgmSrc;
    [SerializeField] private AudioClip bgm;

    [Header("fire vars")]
    [SerializeField]private AudioSource fireSrc;
    [SerializeField] private AudioClip fireL;
    [SerializeField] private AudioClip fireR;

    ControlMap controlMap;
    private PathVisualizer path;

    private void Awake()
    {
        controlMap = new ControlMap();
        controlMap.Prototype.Enable();

        controlMap.Prototype.Left_Trigger.started += ctx => PlayRandomSound(true);
        controlMap.Prototype.Left_Grip.started += ctx => PlayRandomSound(true);
        controlMap.Prototype.Right_Trigger.started += ctx => PlayRandomSound(false);
        controlMap.Prototype.Right_Grip.started += ctx => PlayRandomSound(false);

        controlMap.Prototype.X.started += ctx => PlayRandomSound(true);
        controlMap.Prototype.B.started += ctx => PlayRandomSound(false);
        controlMap.Prototype.A.started += ctx => PlayRandomSound(false);
        controlMap.Prototype.Y.started += ctx => PlayRandomSound(true);
    }

    private void Start()
    {
        if (path == null)
            path = PathVisualizer.instance;

        bgmSrc.enabled = true;
        bgmSrc.clip = bgm;
        bgmSrc.Play();

        path.StartRecording();
    }

    private void PlayRandomSound(bool L)
    {
        AudioClip _c = L ? fireL : fireR;
        fireSrc.PlayOneShot(_c);
    }

    private void Update()
    {
        if (path == null)
            path = PathVisualizer.instance;
    }

    private void OnApplicationQuit()
    {
        path.EndRecording();
    }
}

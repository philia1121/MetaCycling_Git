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
    [SerializeField] private AudioClip[] fires;

    ControlMap controlMap;
    private PathVisualizer path;

    private void Awake()
    {
        controlMap = new ControlMap();
        controlMap.Prototype.Enable();

        controlMap.Prototype.A.started += ctx =>
        {
            path.StartRecording();
        };

        controlMap.Prototype.X.started += ctx =>
        {
            path.StartRecording();
        };

        controlMap.Prototype.Left_Trigger.started += ctx => PlayRandomSound();
        controlMap.Prototype.Right_Trigger.started += ctx => PlayRandomSound();

        controlMap.Prototype.B.started += ctx => PlayRandomSound();
        controlMap.Prototype.Y.started += ctx => PlayRandomSound();
    }

    private void Start()
    {
        if (path == null)
            path = PathVisualizer.instance;

        bgmSrc.enabled = true;
        bgmSrc.clip = bgm;
        bgmSrc.Play();
    }

    private void PlayRandomSound()
    {
        int i = Random.Range(0, fires.Length);
        AudioClip _c = fires[i];
        fireSrc.PlayOneShot(_c);
    }

    private void Update()
    {
        if (path == null)
            path = PathVisualizer.instance;
    }
}

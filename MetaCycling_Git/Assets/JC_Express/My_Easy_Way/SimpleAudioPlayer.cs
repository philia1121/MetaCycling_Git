using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleAudioPlayer : MonoBehaviour
{
    [Header("Basic Setings")]
    [SerializeField]private AudioSource audioSource;
    [SerializeField]private AudioClip[] audioClips;

    [Header("Random Setings")]
    [SerializeField]private float minInterval;
    [SerializeField]private float maxInterval;
    [SerializeField]private bool keepRandomPlay = false;
    bool randomPlay = false;
    IEnumerator cor;

    public void PlayAudio_RandomPick()
    {
        audioSource.clip = audioClips[audioClips.Length == 1? 0 : Random.Range(0, audioClips.Length)];
        if(audioSource.clip != null && !audioSource.isPlaying) audioSource.Play();
    }
    public void PlayAudio_Assigned(AudioClip clip)
    {
        audioSource.clip = clip;
        if(audioSource.clip != null && !audioSource.isPlaying) audioSource.Play();
    }

    public void SetRandomPlay(bool play)
    {
        randomPlay = play;
        if(play)
        {
            cor = PlayRandomSound();
            StartCoroutine(cor);
        }
        else
        {
            if(cor != null) StopCoroutine(cor);
        }
    }
    IEnumerator PlayRandomSound()
    {
        while(randomPlay)
        {
            float interval = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(interval);
            if(randomPlay) PlaySound();
            if(!keepRandomPlay) randomPlay = false;
        }
    }
    void PlaySound()
    {
        if (audioClips.Length > 0)
            audioSource.clip = audioClips[audioClips.Length == 1? 0 : Random.Range(0, audioClips.Length)];

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }
}

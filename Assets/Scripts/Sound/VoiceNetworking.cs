using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FragLabs.Audio.Codecs;

[RequireComponent(typeof(AudioSource))]
public class VoiceNetworking : MonoBehaviour
{
    public AudioSource audioSource { get; private set; }
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 10, 44100);
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        var x = OpusEncoder.Create(44100, 1, FragLabs.Audio.Codecs.Opus.Application.Voip);
        
    }
}
//15:30
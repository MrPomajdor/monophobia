using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FragLabs.Audio.Codecs;
using System;
using System.Threading;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Player))]
public class VoiceNetworking : MonoBehaviour
{
    public AudioSource audioSource { get; private set; }

    private AudioSource audioIncomingSource;
    ConnectionManager conMan;
    OpusEncoder encoder;
    private OpusDecoder decoder;
    private int frameSize = 1600; 
    // The sampling rate of the microphone audio. Ensure this matches the Opus encoder's configuration.
    private int samplingRate = 16000;
    // The number of channels to use. Mono = 1, Stereo = 2.
    private int channels = 1;
    public bool SendAudio = false;
    private Queue<float> audioDataQueue = new Queue<float>();
    public Queue<byte[]> Voice = new Queue<byte[]>();
    private Player pl;
    public bool mute_self;
    ThreadManager thm;
    void Start()
    {
        thm = FindObjectOfType<ThreadManager>();
        pl = GetComponent<Player>();
        conMan = FindObjectOfType<ConnectionManager>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioIncomingSource = gameObject.AddComponent<AudioSource>();
        //                                     name, lengthSamples, channels, frequency, stream, pcmreadercallback,
        audioIncomingSource.clip = AudioClip.Create("recieved_audio", samplingRate * 2, 1, samplingRate, false, OnAudioRead);

        decoder = OpusDecoder.Create(samplingRate, 1);
        if (decoder == null)
        {
            Debug.LogError("Failed to create Opus decoder!");
            return;
        }
        if (!pl.playerInfo.isLocal)
            return;

        encoder = OpusEncoder.Create(samplingRate, channels, FragLabs.Audio.Codecs.Opus.Application.Voip);
        if (encoder == null)
        {
            Debug.LogError("Failed to create Opus encoder!");
            return;
        }
        StartCoroutine(VoiceUpstream());
        StartMicrophone();
    }


    void StartMicrophone()
    {
        audioSource.clip = Microphone.Start(null, true, 10, samplingRate);
        audioSource.loop = true;
        audioSource.volume = 0;
        while (!(Microphone.GetPosition(null) > 0)) { }
        audioSource.Play();
    }
    private IEnumerator VoiceUpstream()
    {
        while (true)
        {

            if (!SendAudio || mute_self)
                yield return null;
            if (Microphone.GetPosition(null) >= frameSize)
            {
                float[] samples = new float[frameSize];
                audioSource.clip.GetData(samples, Microphone.GetPosition(null) - frameSize);

                // Convert float array to byte array
                byte[] sampleBytes = new byte[samples.Length * sizeof(float)];
                Buffer.BlockCopy(samples, 0, sampleBytes, 0, sampleBytes.Length);

                // Encode the audio bytes
                int encodedLength;
                byte[] encodedAudioData = encoder.Encode(sampleBytes, frameSize, out encodedLength);

                //TODO: send voice data
                Voice.Enqueue(encodedAudioData);
                Debug.Log($"there's the data :D");

                yield return null;
            }
            else
            {
                Debug.Log($"waiting data...");

                yield return null;
            }


        }
    }
    private void VoiceDownstream()
    {
        //TODO : please just do voicechat
    }
    void OnAudioRead(float[] data)
    {
        for (int i = 0; i < data.Length; ++i)
        {
            if (audioDataQueue.Count > 0)
            {
                data[i] = audioDataQueue.Dequeue();
            }
            else
            {
                data[i] = 0; // Fill remaining buffer with silence if no data available
            }
        }
    }
    public void ReceiveAudioData(byte[] receivedData)
    {
        // Decode Opus data to PCM float
        int decodedLen;
        byte[] frames = decoder.Decode(receivedData, receivedData.Length, out decodedLen);
        float[] floatArray = new float[decodedLen];
        Buffer.BlockCopy(frames, 0, floatArray, 0, frames.Length);
        foreach (var sample in floatArray)
        {
            audioDataQueue.Enqueue(sample);
        }
    }
    void OnDisable()
    {
        Microphone.End(null); // Stop the microphone
    }
}

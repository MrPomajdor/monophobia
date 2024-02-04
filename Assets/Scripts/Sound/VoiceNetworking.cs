using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POpusCodec;
using System;
using System.Threading;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Player))]
public class VoiceNetworking : MonoBehaviour
{

    public AudioSource audioSource { get; private set; }
    ConnectionManager conMan;
    public bool SendAudio = false;
    private Player pl;
    public bool mute_self;
    ThreadManager thm;
    POpusCodec.Enums.Channels opusChannels = POpusCodec.Enums.Channels.Mono;
    POpusCodec.Enums.SamplingRate opusSamplingRate = POpusCodec.Enums.SamplingRate.Sampling48000;
    private OpusEncoder encoder;
    private int packageSize;
    private OpusDecoder decoder;
    List<float> micBuffer;
    List<float> receiveBuffer;

    void Start()
    {
        receiveBuffer = new List<float>();
        micBuffer = new List<float>();
        thm = FindObjectOfType<ThreadManager>();
        pl = GetComponent<Player>();
        conMan = FindObjectOfType<ConnectionManager>();

        audioSource = gameObject.GetComponent<AudioSource>();
        if (!pl.playerInfo.isLocal)
        {
            AudioClip myClip = AudioClip.Create("Recieved", (int)opusSamplingRate, (int)opusChannels, (int)opusSamplingRate, true, OnAudioRead);
            audioSource.loop = true;
            audioSource.clip = myClip;
            audioSource.Play();

            decoder = new OpusDecoder(opusSamplingRate, opusChannels);
            if (decoder == null)
            {
                Debug.LogError("Failed to create Opus decoder!");
            }

        }
        else
        {//---------------------\/ LOCAL CODE--------------------------


            encoder = new OpusEncoder(opusSamplingRate, opusChannels);//, 1600, POpusCodec.Enums.OpusApplicationType.Voip);
            encoder.EncoderDelay = POpusCodec.Enums.Delay.Delay20ms;
            packageSize = encoder.FrameSizePerChannel * (int)opusChannels;
            if (encoder == null)
            {
                Debug.LogError("Failed to create Opus encoder!");
            }

            StartMicrophone();
        }
    }



    void StartMicrophone()
    {
        audioSource.clip = Microphone.Start(null, true, 1, AudioSettings.outputSampleRate);
        audioSource.loop = true;
        audioSource.Play();
    }
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!mute_self)
        {
            // add mic data to buffer
            micBuffer.AddRange(data);
            //Debug.LogWarning(micBuffer.Count);
            //if (micBuffer.Count > packageSize * 2)

            //Debug.Log("OpusNetworked.OnAudioFilterRead: " + data.Length);
        }

        // clear array so we dont output any sound
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }
    }

    private void VoiceDownstream()
    {
        //TODO : please just do voicechat
    }
    void OnAudioRead(float[] data)
    {
        Debug.LogWarning("OnAudioRead!");

        int pullSize = Mathf.Min(data.Length, receiveBuffer.Count);
        float[] dataBuf = receiveBuffer.GetRange(0, pullSize).ToArray();
        dataBuf.CopyTo(data, 0);
        receiveBuffer.RemoveRange(0, pullSize);

        // clear rest of data
        for (int i = pullSize; i < data.Length; i++)
        {
            data[i] = 0;
        }
    }
    public void ReceiveAudioData(byte[] data)
    {
        ///Debug.LogWarning("Opustest.OnAudioRead: " + data.Length);

        int pullSize = Mathf.Min(data.Length, receiveBuffer.Count);
        float[] dataBuf = receiveBuffer.GetRange(0, pullSize).ToArray();
        dataBuf.CopyTo(data, 0);
        receiveBuffer.RemoveRange(0, pullSize);

        // clear rest of data
        for (int i = pullSize; i < data.Length; i++)
        {
            data[i] = 0;
        }

    }

    private void OnAudioSetPosition(float[] data)
    {
    }
    void OnDisable()
    {
        Microphone.End(null); // Stop the microphone
    }

    public bool DataAvailable()
    {
        return pl.playerInfo.isLocal && micBuffer.Count > packageSize;
    }
    public byte[] GetVoiceData()
    {
        //Debug.LogWarning(micBuffer.Count);
        if (pl.playerInfo.isLocal && micBuffer.Count > packageSize)
        {
            float[] mic_data = micBuffer.GetRange(0, packageSize).ToArray();
            //Debug.Log($"Non encoded len:{mic_data.Length} package size {packageSize}");
            byte[] encodedData = encoder.Encode(mic_data);
            //Debug.Log("encoded len " + encodedData.Length);
            micBuffer.RemoveRange(0, packageSize);
            return encodedData;

        }
        else
        {
            return null;
        }
    }
}

using POpusCodec;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POpusCodec.Enums;



public class VoiceNetworking : MonoBehaviour
{
    public AudioSource micSource;
    public AudioSource playSource;

    List<float> micBuffer = new List<float>();
    List<float> receiveBuffer = new List<float>();

    public List<byte[]> PacketsReady { get; set; } = new List<byte[]>();
    public List<byte[]> InputPackets { get; set; } = new List<byte[]>();

    public SamplingRate samplerate = SamplingRate.Sampling48000;
    public Channels opusChannels = Channels.Mono;
    public Channels clipChannels = Channels.Mono;
    public Delay delay = Delay.Delay40ms;
    public bool shit;
    private OpusEncoder encoder;
    private OpusDecoder decoder;
    private int frameSize;

    float xd;
    int dupa;
    private float spierdalajxd;
    public float lastMicVolume, lastRecievedVolume = 0;
    public bool MuteSelf { get; set; }
    public bool SendAudio { get; set; }

    private bool firstTimeRecv = true;
    public static float CalculateAverageVolume(float[] pcmData)
    {
        if (pcmData == null || pcmData.Length == 0 || pcmData.Length % 2 != 0)
            return 0;

        float sum = 0;
        int sampleCount = pcmData.Length / 2;

        for (int i = 0; i < pcmData.Length; i += 2)
        {
            // Convert two bytes to one short
            float sample = pcmData[i];

            // Add the absolute value to the sum
            sum += Math.Abs(sample);
        }

        // Calculate the average volume
        return (sum / sampleCount)*1000;
    }

    // Start is called before the first frame update
    void Start()
    {

        if (SendAudio)
        {
            //AUDIO INIT
            micSource = gameObject.AddComponent<AudioSource>();
            micSource.clip = Microphone.Start(null, true, 1, (int)samplerate);
            micSource.loop = true;
            micSource.Play();
        }

        playSource = Instantiate(new GameObject(), gameObject.transform).AddComponent<AudioSource>();
        playSource.clip = AudioClip.Create("test", (int)samplerate * 2, (int)clipChannels, (int)samplerate, true, OnAudioPlaybackRead);
        playSource.loop = true;
        playSource.Play();


        //ENCODING INIT
        encoder = new OpusEncoder(samplerate, opusChannels, (int)samplerate * 2, POpusCodec.Enums.OpusApplicationType.Voip);
        encoder.EncoderDelay = delay;
        frameSize = encoder.FrameSizePerChannel * (int)opusChannels;
        decoder = new OpusDecoder(samplerate, opusChannels);
        
        
    }

    private void OnDisable()
    {
        Microphone.End(null);
    }
    private void OnAudioPlaybackRead(float[] data)
    {
        if (receiveBuffer.Count < frameSize)
            return;
        int pullSize = Mathf.Min(data.Length, receiveBuffer.Count);
        float[] dataBuf = receiveBuffer.GetRange(0, pullSize).ToArray();
        dataBuf.CopyTo(data, 0);
        print("Copied data");
        receiveBuffer.RemoveRange(0, pullSize);

        lastRecievedVolume = CalculateAverageVolume(dataBuf);

        // clear rest of data
        for (int i = pullSize; i < data.Length; i++)
        {
            data[i] = 0;
        }

    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (MuteSelf || !SendAudio)
            return;

        // add mic data to buffer
        micBuffer.AddRange(data);

        lastMicVolume = CalculateAverageVolume(data);
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }
        Debug.Log("OpusNetworked.OnAudioFilterRead: " + data.Length);


    }


    void Update()
    {

        

        if (SendAudio && micBuffer.Count >= frameSize)
        {
            PacketsReady.Add(encoder.Encode(micBuffer.GetRange(0, frameSize).ToArray()));
            micBuffer.RemoveRange(0, frameSize);
        }
        if (!SendAudio && InputPackets.Count > 0)
        {
            if (firstTimeRecv)
            {
                receiveBuffer.AddRange(decoder.DecodePacketLostFloat());
                firstTimeRecv = false;
            }
            receiveBuffer.AddRange(decoder.DecodePacketFloat(InputPackets[0]));
            InputPackets.RemoveAt(0);
        }
    }

    public byte[] GetPacket()
    {
        byte[] re = PacketsReady[0];
        PacketsReady.RemoveAt(0);
        return re;
    }
}

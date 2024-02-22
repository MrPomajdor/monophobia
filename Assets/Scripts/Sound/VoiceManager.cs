using POpusCodec;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POpusCodec.Enums;
public class VoiceManager : MonoBehaviour
{
    public AudioSource mainAudioSource;

    List<float> micBuffer = new List<float>();
    List<float> receiveBuffer = new List<float>();

    public List<byte[]> PacketsReady { get; set; } = new List<byte[]>();
    public List<byte[]> InputPackets { get; set; } = new List<byte[]>();

    public SamplingRate samplerate = SamplingRate.Sampling48000;
    public SamplingRate samplerateMic = SamplingRate.Sampling48000;
    public Channels opusChannels = Channels.Mono;
    public Channels clipChannels = Channels.Mono;
    public Delay delay = Delay.Delay40ms;

    private OpusEncoder encoder;
    private OpusDecoder decoder;
    private int frameSize;
    private int pull;
    float xd;
    int pullCount;
    private float spierdalajxd;
    public bool isLocal=false;
    public float lastMicVolume { get; private set; }
    public float lastRecievedVolume { get; private set; }

    public bool MuteSelf { get; set; }
    public float Sensitivity=5;
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
        return (sum / sampleCount) * 1000;
    }

    // Start is called before the first frame update
    void Start()
    {
        //AUDIO INIT
        mainAudioSource = gameObject.GetComponent<AudioSource>();
        if (isLocal)
        {
            mainAudioSource.clip = Microphone.Start(null, true, 1, (int)samplerateMic);
            while (!(Microphone.GetPosition(null) > 0)) { Debug.Log("Waiting for mic..."); }
        }
        else 
            mainAudioSource.clip = AudioClip.Create("recv", (int)samplerate * 2, (int)clipChannels, (int)samplerate, true, OnAudioPlaybackRead);
        mainAudioSource.loop = true;
        mainAudioSource.Play();


        //ENCODING INIT
        decoder = new OpusDecoder(samplerate, opusChannels);
        encoder = new OpusEncoder(samplerate, opusChannels, (int)samplerate * 2, POpusCodec.Enums.OpusApplicationType.Voip);
        encoder.EncoderDelay = delay;
        frameSize = encoder.FrameSizePerChannel * (int)opusChannels;

        print($"opus freq: {(int)encoder._inputSamplingRate}");
        micBuffer.Clear();
    }

    private void OnDisable()
    {
        decoder.Dispose();
        encoder.Dispose();
        Microphone.End(null);
    }
    private void OnAudioPlaybackRead(float[] data)
    {
        //if (receiveBuffer.Count < frameSize) bro frame size is for opus
        //    return;


    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!MuteSelf && isLocal)
        {
            // add mic data to buffer
            lastMicVolume = CalculateAverageVolume(data);
            if(lastMicVolume > Sensitivity)
                micBuffer.AddRange(data);
            
        }

        // clear array 
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }

        if (!isLocal)
        {
            pull += data.Length;
            pullCount += 1;
            int pullSize = Mathf.Min(data.Length, receiveBuffer.Count);

            float[] dataBuf = receiveBuffer.GetRange(0, pullSize).ToArray();
            dataBuf.CopyTo(data, 0);
            print("Copied data");
            receiveBuffer.RemoveRange(0, pullSize);

            // clear rest of data
            for (int i = pullSize; i < data.Length; i++)
            {
                data[i] = 0;
            }
        }
    }


    void Update()
    {
        if (pullCount > 0) //remove the delay (audio that wont get played anyway)
        {
            int averageLen = (pull / pullCount);
            if (receiveBuffer.Count / averageLen > 2)
            {
                receiveBuffer.RemoveRange(receiveBuffer.Count - averageLen - 1, averageLen);
            }
        }
        if (micBuffer.Count >= frameSize) //encode micophone audio
        {
            PacketsReady.Add(encoder.Encode(micBuffer.GetRange(0, frameSize).ToArray()));
            micBuffer.RemoveRange(0, frameSize);
        }

        if (InputPackets.Count > 0) //decode recieved audio
        {
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

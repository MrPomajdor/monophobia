using POpusCodec;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using POpusCodec.Enums;
public class spierdalaj : MonoBehaviour
{
    public AudioSource micSource;
    public AudioSource playSource;

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

    

    public bool MuteSelf { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        //AUDIO INIT
        micSource = gameObject.AddComponent<AudioSource>();
        micSource.clip = Microphone.Start(null, true, 1, (int)samplerateMic);
        micSource.loop = true;
        while (!(Microphone.GetPosition(null) > 0)) { Debug.Log("Waiting for mic..."); }
        micSource.Play();
        print($"mic source channels: {micSource.clip.channels}");

        playSource = Instantiate(new GameObject(),gameObject.transform).AddComponent<AudioSource>();
        playSource.clip = AudioClip.Create("test", (int)samplerate * 2, (int)clipChannels, (int)samplerate, true,OnAudioPlaybackRead);
        playSource.loop = true;
        playSource.Play();
        print($"play source freq: {playSource.clip.frequency}");

        //ENCODING INIT
        decoder = new OpusDecoder(samplerate, opusChannels);
        encoder = new OpusEncoder(samplerate, opusChannels, (int)samplerate*2, POpusCodec.Enums.OpusApplicationType.Voip);
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
        if (!MuteSelf)
        {
            // add mic data to buffer
            micBuffer.AddRange(data);
            Debug.Log("OpusNetworked.OnAudioFilterRead: " + data.Length);
        }

        // clear array so we dont output any sound
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }


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


    void Update()
    {
        //if(micBuffer.Count > frameSize * 3)
        //   micBuffer.RemoveAt(micBuffer.Count - 1);
        int averageLen = (pull / pullCount);
        if (receiveBuffer.Count / averageLen > 2)
        {
            receiveBuffer.RemoveRange(receiveBuffer.Count - averageLen - 1, averageLen);
        }
        if (micBuffer.Count >= frameSize)
        {
            PacketsReady.Add(encoder.Encode(micBuffer.GetRange(0, frameSize).ToArray()));
            micBuffer.RemoveRange(0, frameSize);
        }

        if (PacketsReady.Count > 0)
        {
            receiveBuffer.AddRange(decoder.DecodePacketFloat(PacketsReady[0]));
            PacketsReady.RemoveAt(0);
        }
    }
}

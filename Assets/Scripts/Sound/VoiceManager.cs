using POpusCodec;
using POpusCodec.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.Intrinsics;
using UnityEngine;
public class VoiceManager : MonoBehaviour
{
    public enum Type
    {
        Remote,
        Local
    }

    private Type _type;

    public AudioSource mainAudioSource;

    List<float> micBuffer = new List<float>();
    List<float> receiveBuffer = new List<float>();

    public List<byte[]> PacketsReady { get; set; } = new List<byte[]>();
    public List<byte[]> InputPackets { get; set; } = new List<byte[]>();

    public SamplingRate samplerate = SamplingRate.Sampling48000;
    public SamplingRate samplerateMic = SamplingRate.Sampling48000;
    public SamplingRate bitrateOpus = SamplingRate.Sampling48000;
    public Bandwidth bandwith = Bandwidth.Fullband;
    public Channels opusChannels = Channels.Mono;
    public Channels clipChannels = Channels.Mono;
    public Delay delay = Delay.Delay40ms;

    private OpusEncoder encoder;
    private OpusDecoder decoder;
    public int frameSize;
    private int pull;
    float xd;
    int pullCount;
    private float spierdalajxd;
    
    public float lastMicVolume { get; private set; }
    public float lastRecievedVolume { get; private set; }

    public bool MuteSelf { get; set; }
    public float Sensitivity = 5;
    public bool initialized;
    bool isFocused = false;

    public bool MicrophoneActive { get { return se || falloffHold; }  }

    public bool playLocally { get; set; }
    public ConnectionManager conMan { get; private set; }

    [field: SerializeField]
    public int micBufferSize { get; private set; }
    [field: SerializeField]
    public int receiveBufferSize { get; private set; }
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

    public void Initialize(Type type)
    {
        conMan = FindObjectOfType<ConnectionManager>();
        _type = type;
        string x = _type==Type.Local ? "Local" : "Remote";
        Debug.Log($"{x} voice inializing....");
        //AUDIO INIT
        mainAudioSource = gameObject.GetComponent<AudioSource>();
        if (_type==Type.Local)
        {
            mainAudioSource.clip = Microphone.Start(null, true, 1, (int)samplerateMic);
            while (!(Microphone.GetPosition(null) > 0)) { /*nop*/ }
        }
        else
            mainAudioSource.clip = AudioClip.Create("recv", (int)samplerate * 2, (int)clipChannels, (int)samplerate, true, OnAudioPlaybackRead);
        mainAudioSource.loop = true;
        mainAudioSource.Play();


        //ENCODING INIT
        decoder = new OpusDecoder(samplerate, opusChannels);
        encoder = new OpusEncoder(samplerate, opusChannels, (int)samplerate * 2, POpusCodec.Enums.OpusApplicationType.Voip); //TODO : Do not create a decoder if not needed! (pls do this)
        encoder.EncoderDelay = delay;
        encoder.Bitrate = (int)bitrateOpus;
        encoder.MaxBandwidth = bandwith;
        encoder.ExpectedPacketLossPercentage = 10;
        frameSize = encoder.FrameSizePerChannel;// * (int)opusChannels ;
        
        print($"opus freq: {(int)encoder._inputSamplingRate}");
        micBuffer.Clear();
        initialized = true;

        Debug.Log($"{x} voice inialized!");
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
    float falloffTracker = 0;
    bool falloffHold;
    bool se;
    

    void OnAudioFilterRead(float[] data, int channels)
    {
        if ((_type == Type.Local && !MuteSelf) && isFocused)
        {
            // add mic data to buffer
            lastMicVolume = CalculateAverageVolume(data);
            if (se || falloffHold) micBuffer.AddRange(data);
            else micBuffer.Clear();

        }
        else
        {
            micBuffer.Clear();
        }

        
        // clear array 
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }

        if (_type == Type.Remote && receiveBuffer.Count > 0)
        {
            
            if (receiveBuffer.Count < frameSize)
                return;
            //pullCount += 1;
            int pullSize = Mathf.Min(data.Length, receiveBuffer.Count);
            if (data.Length > receiveBuffer.Count)
                Debug.LogWarning("SRANIE");
            //pull += pullSize;
            float[] dataBuf = receiveBuffer.GetRange(0, pullSize).ToArray();
            dataBuf.CopyTo(data, 0);
            receiveBuffer.RemoveRange(0, pullSize);
           
        }


    }


    void Update()
    {
        micBufferSize = micBuffer.Count;
        receiveBufferSize = receiveBuffer.Count;
        isFocused =  Application.isFocused;
        if (_type == Type.Local)

            if (lastMicVolume > Sensitivity)
            {
                se = true;
                falloffTracker = 0;
            }
            else
            {
                se = false;

                if (falloffTracker > .5f)
                    falloffHold = false;
                else
                {
                    falloffHold = true;
                    falloffTracker += Time.deltaTime;
                }
            }

        SendData();
       /* if (micBuffer.Count >= frameSize) //encode micophone audio
        {
            PacketsReady.Add(encoder.Encode(micBuffer.GetRange(0, frameSize).ToArray()));

            micBuffer.RemoveRange(0, frameSize);
        }

        if (InputPackets.Count > 0) //decode recieved audio
        {
            receiveBuffer.AddRange(decoder.DecodePacketFloat(InputPackets[0]));
            InputPackets.RemoveAt(0);
        }*/
    }
    public void ReceiveData(byte[] encodedData)
    {

        // the data would need to be sent over the network, we just decode it now to test the result
        receiveBuffer.AddRange(decoder.DecodePacketFloat(encodedData));
    }

    void SendData()
    {
        if (_type == Type.Local)
        {
            // take pieces of buffer and send data
            while (micBuffer.Count > frameSize)
            {
                byte[] encodedData = encoder.Encode(micBuffer.GetRange(0, frameSize).ToArray());
                conMan.SendVoiceData(encodedData);
                if (playLocally) ReceiveData(encodedData);
                micBuffer.RemoveRange(0, frameSize);
            }
        }
    }

    public byte[] GetPacket()
    {
        byte[] re = PacketsReady[0];
        PacketsReady.RemoveAt(0);
        return re;
    }
}

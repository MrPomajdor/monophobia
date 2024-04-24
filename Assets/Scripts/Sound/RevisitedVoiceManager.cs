using POpusCodec;
using POpusCodec.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public enum Type
{

}

public class RevisitedVoiceManager : MonoBehaviour
{
    public enum Type
    {
        Remote,
        Local
    }
    public int micDeviceId = 0;
    //Mic audio
    AudioSource audiorecorder;
    //playback audio
    public AudioSource audioplayer;

    private Type _type;
    List<float> micBuffer;
    List<float> receiveBuffer;
    int packageSize;
    OpusEncoder encoder;
    OpusDecoder decoder;

    Channels opusChannels = Channels.Stereo;
    SamplingRate opusSamplingRate = SamplingRate.Sampling48000;
    public SamplingRate Bitrate = SamplingRate.Sampling24000;
    public Delay delay = Delay.Delay40ms;

    public bool capture = false;
    public bool playLocally = false;
    private ConnectionManager conMan;
    public float lastMicVolume {  get; private set; }
    public bool MicrophoneActive { get; private set; }
    private void Start()
    {
        //Initialize(Type.Local);
    }
    public void Initialize(Type voiceType)
    {
        conMan = FindObjectOfType<ConnectionManager>();
        _type = voiceType;
        micBuffer = new List<float>();
        audiorecorder = GetComponent<AudioSource>();
        if (_type == Type.Local)
        {
            
            encoder = new OpusEncoder(opusSamplingRate, opusChannels);
            encoder.EncoderDelay = delay;
            encoder.ForceChannels = ForceChannels.NoForce;
            encoder.Bitrate = (int)Bitrate;
            encoder.MaxBandwidth = Bandwidth.SuperWideband;
            Debug.Log("Opustest.Start: framesize: " + encoder.FrameSizePerChannel + " " + encoder.InputChannels);
            // the encoder delay has some influence on the amout of data we need to send, but it's not a multiplication of it
            packageSize = encoder.FrameSizePerChannel *  (int)opusChannels;

            // setup a microphone audio recording
            Debug.Log("Opustest.Start: setup mic with " + Microphone.devices[micDeviceId] + " " + AudioSettings.outputSampleRate);
            
            audiorecorder.loop = true;
            audiorecorder.clip = Microphone.Start(
                Microphone.devices[micDeviceId],
                true,
                1,
                AudioSettings.outputSampleRate);
            audiorecorder.Play();

        }
        else
        {
            audiorecorder.clip = AudioClip.Create("recv", (int)opusSamplingRate * 2, (int)opusChannels, (int)opusSamplingRate, true, OnAudioRead);
            audiorecorder.loop = true;
            audiorecorder.Play();
        }
        // playback stuff
        decoder = new OpusDecoder(opusSamplingRate, opusChannels);
        
        receiveBuffer = new List<float>();

        // setup a playback audio clip, length is set to 1 sec (should not be used anyways)
        AudioClip myClip = AudioClip.Create("MyPlayback", (int)opusSamplingRate, (int)opusChannels, (int)opusSamplingRate, true, OnAudioRead, OnAudioSetPosition);
        audioplayer.loop = true;
        audioplayer.clip = myClip;
        audioplayer.Play();
    }
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (capture && _type==Type.Local)
        {
            // add mic data to buffer
            micBuffer.AddRange(data);
        }

        // clear array so we dont output any sound
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0;
        }

        //use the same audio source to play the recieved remote sound.
        if (_type == Type.Remote)
        {
            int pullSize = Mathf.Min(data.Length, receiveBuffer.Count);
            float[] dataBuf = receiveBuffer.GetRange(0, pullSize).ToArray();
            dataBuf.CopyTo(data, 0);
            receiveBuffer.RemoveRange(0, pullSize);
        }
    }

    void OnAudioRead(float[] data)
    {
        //nop
    }
    void OnAudioSetPosition(int newPosition)
    {
        //nop
    }

    void SendData()
    {
        if (_type==Type.Local)
        {
            // take pieces of buffer and send data
            while (micBuffer.Count > packageSize)
            {
                byte[] encodedData = encoder.Encode(micBuffer.GetRange(0, packageSize).ToArray());
                conMan.SendVoiceData(encodedData);
                if (playLocally) ReceiveData(encodedData);
                micBuffer.RemoveRange(0, packageSize);
            }
        }
    }

   public void ReceiveData(byte[] encodedData)
    {
       /* if (_type==Type.Remote && !playLocally)
        {
            Debug.Log("VoiceManagerReceiveData: discard! " + encodedData.Length);
            return;
        }*/


        // the data would need to be sent over the network, we just decode it now to test the result
        receiveBuffer.AddRange(decoder.DecodePacketFloat(encodedData));
    }

    // Update is called once per frame
    void Update()
    {
        SendData();
    }
}

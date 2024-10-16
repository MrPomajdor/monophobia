using POpusCodec;
using POpusCodec.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class RevisitedVoiceManager : MonoBehaviour
{
    public enum Role { Sender, Receiver }
    public Role currentRole;

    public AudioSource audioSource;
    public float LastMicVolume { get; private set; }
    public bool MicActive { get { return isSpeaking; } }

    public float voiceThreshold = 0.01f; // Set the threshold to detect when the user is talking
    public float silenceDuration = 0.5f; // Time after speaking ends to stop sending data
    private float timeSinceLastSpeech = 0f;
    private bool isSpeaking = false;

    private OpusEncoder opusEncoder;
    private OpusDecoder opusDecoder;
    private const int sampleRate = 16000;
    private const int channels = 1;
    private const int frameSize = 320;
    private const int bitrate = 16000;

    List<float> receiveBuffer = new List<float>();
    public Queue<float[]> micBuffer = new Queue<float[]>();

    //private AudioClip micClip;
    //private int micPosition = 0;
    //private bool isMicActive;

    private Player owner;
    private void Start()
    {
        owner = GetComponent<Player>();
    }
    private void Update()
    {
        if (currentRole == Role.Sender)
        {
            if (LastMicVolume > voiceThreshold)
            {
                isSpeaking = true;
                timeSinceLastSpeech = 0f;
            }
            else
            {
                timeSinceLastSpeech += Time.deltaTime;
                if (isSpeaking && timeSinceLastSpeech >= silenceDuration)
                {
                    isSpeaking = false;
                    SendVoiceData(new byte[0]); // Send empty data to signal end of transmission
                }

            }

            while (micBuffer.Count>0)
            {


                // Calculate how many samples to process for this time period
                float[] frame = micBuffer.Dequeue();

                byte[] encodedData = EncodeAudioData(frame);
                SendVoiceData(encodedData);
            }

        }
    }
    public void Initialize(Role role)
    {
        currentRole = role;
        audioSource = gameObject.GetComponent<AudioSource>();
        if (role == Role.Sender)
        {
            // Initialize Opus encoder for sending
            opusEncoder = new OpusEncoder((SamplingRate)sampleRate, (Channels)channels, bitrate, OpusApplicationType.Voip);
            //opusEncoder.Bitrate = bitrate;
            audioSource.clip = Microphone.Start(null, true, 1, sampleRate); // 1 second buffer length
            while (Microphone.GetPosition(null) <= 0) { } // Wait until the microphone starts
        }
        else if (role == Role.Receiver)
        {
            // Initialize Opus decoder for receiving
            opusDecoder = new OpusDecoder((SamplingRate)sampleRate, (Channels)channels);
            audioSource.clip = AudioClip.Create("recv", sampleRate * 2, channels, sampleRate, true, OnAudioPlaybackRead);

        }

        audioSource.loop = true;
        audioSource.Play();
    }
    void OnAudioPlaybackRead(float[] data) { }

    // Unity's OnAudioFilterRead is called every audio frame, before the data is passed to the output
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (currentRole == Role.Sender)
        {
            HandleAudioData(data);
        }

        System.Array.Clear(data, 0, data.Length);

        if (currentRole == Role.Receiver)
        {
            if (receiveBuffer.Count >= data.Length)
            {
                float[] dataBuf = receiveBuffer.GetRange(0, data.Length).ToArray();
                receiveBuffer.RemoveRange(0, data.Length);
                dataBuf.CopyTo(data, 0);
            }
        }
        // You can implement receiving audio data in a different thread or using an event system.
    }

    // This method handles audio data and voice detection
    private void HandleAudioData(float[] data)
    {
        // Check if volume exceeds threshold
        float volume = data.Average(Mathf.Abs);
        LastMicVolume = volume;

        if (isSpeaking)
        {
            micBuffer.Enqueue(data);
        }
        



    }

    // Encodes audio data using POpusCodec
    private byte[] EncodeAudioData(float[] data)
    {
        short[] pcmData = new short[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            pcmData[i] = (short)(Mathf.Clamp(data[i], -1f, 1f) * short.MaxValue);
        }

        byte[] encoded = opusEncoder.Encode(pcmData);
        return encoded;
    }


    private void SendVoiceData(byte[] data)
    {
        Global.connectionManager.SendVoiceData(data);
    }

    
    public void ReceiveVoiceData(byte[] encodedData)
    {
        if (currentRole == Role.Receiver && encodedData.Length > 0)
        {
            short[] decodedPcm = new short[frameSize * channels];
            decodedPcm = opusDecoder.DecodePacket(encodedData);

            float[] pcmData = new float[decodedPcm.Length];
            for (int i = 0; i < decodedPcm.Length; i++)
            {
                pcmData[i] = decodedPcm[i] / (float)short.MaxValue;
            }

            receiveBuffer.AddRange(pcmData);

        }
    }

    private void OnDestroy()
    {
        if (opusEncoder != null)
        {
            opusEncoder.Dispose();
        }
        if (opusDecoder != null)
        {
            opusDecoder.Dispose();
        }

        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
        }
    }

    private void OnEnable()
    {
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.voice[0], ParseVoiceData);
    }
    private void OnDisable()
    {
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.voice[0], ParseVoiceData);
    }


    private void ParseVoiceData(Packet packet)
    {
        int player_id = -1;
        byte[] _pay;
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {

            player_id = reader.ReadInt32();
            int pay_len = reader.ReadInt32();
            _pay = reader.ReadBytes(pay_len);
        }
        if (player_id == owner.playerInfo.id)
            return;

        ReceiveVoiceData(_pay);


    }
}

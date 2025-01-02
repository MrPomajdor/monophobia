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


    private const int sampleRate = 16000;
    private const int channels = 1;

    List<float> receiveBuffer = new List<float>();
    public List<float> micBuffer = new List<float>();

    private int frameSize = (int)(sampleRate * 0.035f)*2;
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

            if(micBuffer.Count>= frameSize)//SoundCompression.encoder.FrameSize)
            {


                // Calculate how many samples to process for this time period
                float[] frame = new float[frameSize];
                micBuffer.GetRange(0, frameSize).CopyTo(frame);
                micBuffer.RemoveRange(0, frameSize);
                Debug.Log($"Got mic data! {frame.Length}");
                EncodeAudioData(frame);
                
            }

        }
    }
    public void Initialize(Role role)
    {
        currentRole = role;
        audioSource = gameObject.GetComponent<AudioSource>();
        if (role == Role.Sender)
        {
            //opusEncoder.Bitrate = bitrate;
            audioSource.clip = Microphone.Start(null, true, 1, sampleRate); // 1 second buffer length
            while (Microphone.GetPosition(null) <= 0) { } // Wait until the microphone starts
        }
        else if (role == Role.Receiver)
        {
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
            micBuffer.AddRange(data);
        }
        



    }

    // Encodes audio data using POpusCodec
    private void EncodeAudioData(float[] data)
    {
        Debug.Log("Sending uncompressed audio for compression...");
        SoundCompression.Encode(data, OnCompressionFinished);

    }
    private void OnCompressionFinished(byte[] data)
    {
        Debug.Log("Audio compression finished!");
        if (currentRole == Role.Sender)
            SendVoiceData(data);
    }

    private void OnDecompressionFinished(float[] data)
    {
        Debug.Log("Finished decompressing");
        receiveBuffer.AddRange(data);
    }

    private void SendVoiceData(byte[] data)
    {
        Global.connectionManager.SendVoiceData(data);
    }

    
    public void ReceiveVoiceData(byte[] encodedData)
    {
        if (currentRole == Role.Receiver && encodedData.Length > 0)
        {
            Debug.Log("Decoding voice data");
            SoundCompression.Decode(encodedData, OnDecompressionFinished);

        }
    }

    private void OnDestroy()
    {
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

        Debug.Log("Received voice data");
        ReceiveVoiceData(_pay);


    }
}

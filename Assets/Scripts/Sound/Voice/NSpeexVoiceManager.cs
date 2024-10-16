using NSpeex;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityVOIP;

public class NSpeexVoiceManager : MonoBehaviour
{
    public enum Role { Sender, Receiver }
    public Role currentRole;

    public float voiceThreshold = 0.01f;
    private float silenceDuration = 0.5f;
    private float timeSinceLastSpeech = 0f;
    public bool micIsActive = false;
    public bool isSpeaking = false;

    private SpeexEncoder speexEncoder;
    private SpeexDecoder speexDecoder;
    private const int sampleRate = 16000; // Sample rate for Speex (narrowband mode)
    private const int channels = 1;
    private const int frameSize = 320; // 20ms worth of samples for 16kHz
    private AudioClip micClip;
    private int micPosition = 0;
    private List<float> micBuffer = new List<float>();
    private Queue<float[]> receivedAudioQueue = new Queue<float[]>();

    public NSpeexVoiceManager re;
    private AudioSource mainAudioSource;

    private void Start()
    {
        if (currentRole == Role.Sender)
        {
            speexEncoder = new SpeexEncoder(BandMode.Wide); // Use narrowband (8 kHz) or wideband (16 kHz)
            speexEncoder.Quality = 6; // Set the encoding quality level (range 0-10)

            // Initialize microphone input
            micClip = Microphone.Start(null, true, 1, sampleRate); // 1-second buffer
            while (Microphone.GetPosition(null) <= 0) { }
        }
        else if (currentRole == Role.Receiver)
        {
            mainAudioSource = GetComponent<AudioSource>();
            speexDecoder = new SpeexDecoder(BandMode.Wide); // Make sure it matches the sender's mode
            mainAudioSource.clip = AudioClip.Create("recv", (int)sampleRate * 2, (int)channels, (int)sampleRate, true, OnAudioPlaybackRead);
            mainAudioSource.loop = true;
            mainAudioSource.Play();
        }
    }

    private void OnAudioPlaybackRead(float[] data)
    {
       
    }


    private void Update()
    {
        if (currentRole == Role.Sender)
        {
            int micPos = Microphone.GetPosition(null); // Current microphone position
            if (micPos < micPosition) micPosition = 0; // Handle mic buffer wrapping

            int numSamples = micPos - micPosition;
            if (numSamples > 0)
            {
                float[] samples = new float[numSamples];
                micClip.GetData(samples, micPosition); // Pull samples from mic
                micPosition = micPos; // Update mic position

                micBuffer.AddRange(samples); // Add mic samples to buffer

                // Process mic buffer if enough data is available
                while (micBuffer.Count >= frameSize)
                {
                    float[] frame = micBuffer.GetRange(0, frameSize).ToArray();
                    micBuffer.RemoveRange(0, frameSize);

                    // Check volume for voice activity detection
                    float volume = frame.Average(Mathf.Abs);
                    micIsActive = volume > voiceThreshold;

                    if (micIsActive)
                    {
                        byte[] encodedData = EncodeAudioData(frame);
                        SendVoiceData(encodedData);
                    }
                    else
                    {
                        timeSinceLastSpeech += Time.deltaTime;

                        if (isSpeaking && timeSinceLastSpeech >= silenceDuration)
                        {
                            isSpeaking = false;
                            SendVoiceData(new byte[0]); // Signal end of transmission
                        }
                    }
                }
            }
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (currentRole == Role.Receiver)
        {
            System.Array.Clear(data, 0, data.Length);

            if (receivedAudioQueue.Count > 0)
            {
                float[] decodedAudio = receivedAudioQueue.Dequeue();
                for (int i = 0; i < data.Length && i < decodedAudio.Length; i++)
                {
                    data[i] = decodedAudio[i];
                }
            }
        }
    }

    private byte[] EncodeAudioData(float[] data)
    {
        short[] pcmData = new short[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            pcmData[i] = (short)(Mathf.Clamp(data[i], -1f, 1f) * short.MaxValue);
        }

        byte[] encodedData = new byte[200]; // Allocate enough space for the encoded data
        int encodedLength = speexEncoder.Encode(pcmData, 0, pcmData.Length, encodedData, 0, encodedData.Length);

        return encodedData.Take(encodedLength).ToArray();
    }

    private void SendVoiceData(byte[] data)
    {
        re.ReceiveVoiceData(data);
    }

    public void ReceiveVoiceData(byte[] encodedData)
    {
        if (currentRole == Role.Receiver && encodedData.Length > 0)
        {
            short[] decodedPcm = new short[frameSize];
            speexDecoder.Decode(encodedData, 0, encodedData.Length, decodedPcm, 0, false);

            float[] pcmData = new float[decodedPcm.Length];
            for (int i = 0; i < decodedPcm.Length; i++)
            {
                pcmData[i] = decodedPcm[i] / (float)short.MaxValue;
            }

            receivedAudioQueue.Enqueue(pcmData);
            Debug.Log("Rec");
        }
    }

    private void OnDestroy()
    {
        if (Microphone.IsRecording(null)) Microphone.End(null);
    }
}

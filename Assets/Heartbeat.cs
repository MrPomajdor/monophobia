using CSCore.XAudio2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heartbeat : MonoBehaviour
{
    public AudioSource HeartbeatSource;
    [SerializeField]
    private float targetBPM;
    
    public float CurrentBPM { get; private set; }
    public float HeartbeatVolume=1;
    float timer;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1)
        {
            targetBPM -= 5;
            timer = 0;
        }

        targetBPM = Mathf.Clamp(targetBPM, 71.36f, 300f);
        CurrentBPM = CurrentBPM + (targetBPM - CurrentBPM) * Time.deltaTime*0.5f;
        HeartbeatSource.pitch = CurrentBPM / 71.36f;

        HeartbeatSource.volume = 1 - Mathf.Clamp01(90f / CurrentBPM);
        HeartbeatSource.volume *= HeartbeatVolume;
        
    }

    public void SetBPM(float bpm=71.36f)
    {
       targetBPM = bpm;
    }

    public void IncreaseBPM(float amount)
    {
        targetBPM += amount;
    }
}

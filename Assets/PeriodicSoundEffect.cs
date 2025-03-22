using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PeriodicSoundEffect : MonoBehaviour
{
    public List<AudioClip> clips = new List<AudioClip>();
    public AudioSource source;
    public bool RandomInterval;
    public float Interval;
    public float RandomMin;
    public float RandomMax;

    float timer;
    float chosen=-1;
    void Update()
    {
        timer += Time.deltaTime;
        if (RandomInterval)
        {
            if(chosen<0)
                chosen = Random.Range(RandomMin, RandomMax);
            if (timer >= chosen)
            {
                source.PlayOneShot(clips[Random.Range(0, clips.Count)]);
                timer = 0;
                chosen = -1;
            }
        }
        else
        {
            if (timer >= Interval)
            {
                source.PlayOneShot(clips[Random.Range(0, clips.Count)]);
                timer = 0;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootstepsSFX : MonoBehaviour
{
    public AudioClip[] StepSounds;
    public AudioSource audioSource;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayStepSound()
    {
        audioSource.PlayOneShot(StepSounds[Random.Range(0, StepSounds.Length)]);
    }
}

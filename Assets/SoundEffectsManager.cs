using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class SoundEffectsManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void PlaySound(AudioClip clip, Vector3 position, float volume = 1)
    {
        GameObject ob = new GameObject();
        ob.transform.position = position;

        AudioSource src = ob.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialize = true;
        src.loop = false;
        Destroy(ob, clip.length);

    }
    public void PlaySound(AudioClip clip,float volume = 1)
    {
        GameObject ob = new GameObject();
        ob.transform.SetParent(transform);

        AudioSource src = ob.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialize = true;
        src.loop = false;
        Destroy(ob,clip.length);
    }


}

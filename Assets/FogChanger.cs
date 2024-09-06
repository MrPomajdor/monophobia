using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
public class FogChanger : MonoBehaviour
{
    VolumeProfile profile;
    public Color c;
    void Start()
    {
        profile = GetComponent<Volume>().sharedProfile;    
    }

    // Update is called once per frame
    void Update()
    {
        SetAlbedo(c);
    }

    void SetAlbedo(Color color)
    {
        if(!profile.TryGet<Fog>(out var fog))
        {
            fog = profile.Add<Fog>(false);
        }
        fog.albedo.overrideState = true;
        fog.albedo.value = color;
    }
}

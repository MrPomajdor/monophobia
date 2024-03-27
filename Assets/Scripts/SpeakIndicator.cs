using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SpeakIndicator : MonoBehaviour
{
    public float _vol;
    float max;
    Material material;
    Color color;
    float t;
    Player player;
    ConnectionManager conMan;
    void Start()
    {
        material = GetComponent<Renderer>().material;  
        color = material.color;
        player = transform.root.GetComponent<Player>();
        conMan = FindAnyObjectByType<ConnectionManager>();
        if (player.playerInfo.isLocal)
            gameObject.SetActive(false);
            
    }

    // Update is called once per frame
    void Update()
    {
        if (player.playerInfo.isLocal)
            return;
        t += Time.deltaTime;
        if (t > 5)
        {
            max = 1;
            t = 0;
        }
        if (_vol > max)
            max = _vol;
        color.a = math.remap(0,max,0,1,math.clamp(_vol, 0, max));
        material.color = color;
        
        transform.LookAt(conMan.client_self.connectedPlayer.cam.transform.position);
    }

    public void SetCurrentVolume(float vol)
    {
        _vol = vol;
        
    }
}

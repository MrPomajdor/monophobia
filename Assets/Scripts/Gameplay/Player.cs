
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerInfo playerInfo;
    public Camera cam;
    public MouseRotation mouseRotation;
    public Movement movement;
    ConnectionManager conMan;
    public Rigidbody rb;
    public Transforms transforms;
    public float lastTime;
    public VoiceManager voice;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        conMan = FindObjectOfType<ConnectionManager>();
    }
    Vector3 prev, prevrot;
    // Update is called once per frame
    float lt2;
    bool s;
    GUIStyle style = new GUIStyle();
    private void OnGUI()
    {
        if (!playerInfo.isLocal) return;

        style.fontSize = 30;
        style.normal.textColor = Color.green;
        GUI.Label(new Rect(10, 10, 500, 1000),$"Local Player ID: {playerInfo.id}\nName: {conMan.client_self.name}\nServer ip: {conMan._IPAddress}\n\nMIC VOL: {voice.lastMicVolume}\nRECV VOL: {voice.lastRecievedVolume}", style);
    }
    void Update()
    {
        

        lt2 += Time.deltaTime;
        if (!playerInfo.isLocal)
        {  //-----------------------------------------REMOTE CODE------------------------------------------------
            Tools.UpdatePos(transform, rb, transforms,true); 
        }
        else
        {  //-----------------------------------------LOCAL CODE-------------------------------------------------

            //voice
            if (playerInfo.isLocal)
            {
                if (voice.PacketsReady.Count>0)
                {
                    //Debug.Log("Voice data avaliable!");
                    conMan.SendVoiceData(voice.GetPacket());
                }
                else
                {
                   // Debug.Log("no voice data");
                }
            }

            //periodic position sending
            if (lt2 > 0.15f)
            {
                conMan.SendPlayerLocationInfo(this);
                lt2 = 0;
            }
        }
    }

    


}

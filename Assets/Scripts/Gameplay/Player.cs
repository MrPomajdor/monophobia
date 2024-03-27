
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Stats))]
[RequireComponent(typeof(SoundEffectsManager))]
public class Player : MonoBehaviour
{
    public PlayerInfo playerInfo;
    public Camera cam { get; private set; }
    public MouseRotation mouseRotation;
    public Movement movement;
    ConnectionManager conMan;
    public Rigidbody rb { get; private set; }
    public Transforms transforms;
    public float lastTime;
    public VoiceManager voice { get; private set; }
    public Stats stats { get; private set; }
    private SoundEffectsManager sfxManager;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        conMan = FindObjectOfType<ConnectionManager>();
        stats = GetComponent<Stats>();
        sfxManager = GetComponent<SoundEffectsManager>();
    }
    Vector3 prev, prevrot;
    // Update is called once per frame
    float lt2, lt3;
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
        lt3 += Time.deltaTime;
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
            

            //TEMP CODE ONLY FOR TESTING REMOVE IT LATER
            if(lt3 > 5)
            {
                if (conMan.clients.Count == 0)
                    return;
                if(conMan.clients[0].connectedPlayer.voice.VoicePieces.Count > 0) //ugly \/
                    sfxManager.PlaySound(conMan.clients[0].connectedPlayer.voice.VoicePieces[UnityEngine.Random.Range(0, conMan.clients[0].connectedPlayer.voice.VoicePieces.Count-1)]);
                lt3 = 0;
            }

        }
    }

    


}

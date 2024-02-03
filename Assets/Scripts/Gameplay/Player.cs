using Newtonsoft.Json;
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
    public Vector3 real_velocity, position, rotation, target_velocity;
    public float lastTime;
    public VoiceNetworking voice;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        conMan = FindObjectOfType<ConnectionManager>();
    }
    Vector3 prev, prevrot;
    // Update is called once per frame
    float lt, lt2;
    bool s;
    GUIStyle style = new GUIStyle();
    private void OnGUI()
    {
        if (!playerInfo.isLocal) return;

        style.fontSize = 30;
        style.normal.textColor = Color.green;
        GUI.Label(new Rect(10, 10, 500, 1000),$"Local Player ID: {playerInfo.id}\nLast Packet time: {lastTime}", style);
    }
    void Update()
    {
        

        lt += Time.deltaTime;
        lt2 += Time.deltaTime;
        if (!playerInfo.isLocal)
        {  //-----------------------------------------REMOTE CODE------------------------------------------------
            //Vector3 predictedPos = transform.position + (target_velocity * (lastTime-Time.realtimeSinceStartup));

            transform.rotation = Quaternion.Euler(0, rotation.y, 0);
            rb.velocity = real_velocity;
            rb.velocity +=  position - transform.position;
            rb.velocity += target_velocity;
            if (Vector3.Distance(transform.position, position) > 2)
                transform.position = position;
            




        }
        else
        {  //-----------------------------------------LOCAL CODE-------------------------------------------------
           
            //voice
            byte[] vo_packet;
            if (voice.Voice.TryDequeue(out vo_packet))
            {
                conMan.SendVoiceData(vo_packet);
            }

            //periodic position sending
            if (lt2 > 0.15f)
            {
                conMan.SendLocationInfo(this);
                lt2 = 0;
            }
        }
    }

    


}

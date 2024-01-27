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
    UDPHandler udp;
    public Rigidbody rb;
    public Vector3 velocity, postion, rotation;
    public float lastTime;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        udp = FindObjectOfType<UDPHandler>();
        
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
        GUI.Label(new Rect(10, 10, 500, 1000),$"Local udp port : {udp.local_port}\nLocal Player ID: {playerInfo.id}\nLast Packet time: {lastTime}", style);
    }
    void Update()
    {
        lt += Time.deltaTime;
        lt2 += Time.deltaTime;
        if (!playerInfo.isLocal)
        {  //-----------------------------------------REMOTE CODE------------------------------------------------
            Vector3 predictedPos = transform.position + (velocity * (lastTime-Time.realtimeSinceStartup));
           

            rb.velocity = velocity;
            if (Vector3.Distance(transform.position, predictedPos) > 0.2f)
            {
               rb.velocity += predictedPos - transform.position;
            }

            if (Vector3.Distance(transform.position, postion) > 1)
            {
                transform.position = postion;
            }




        }
        else
        {  //-----------------------------------------LOCAL CODE-------------------------------------------------

            if (!movement.isMoving)
            {
                if (!s)
                {
                    SendLocationZeroVel();
                    s = true;
                }
            }
            else
                s = false;
            if (Vector3.Distance(movement.rb.velocity, prev) > 1f || Vector3.Distance(mouseRotation.transform.eulerAngles, prevrot) > 20 || (movement.rb.velocity.magnitude < 0.1f && Vector3.Distance(movement.rb.velocity, prev) > 0.1f))
            {
                if (lt < 0.15f)
                    return;
                lt = 0;
                SendLocationInfo();
                prev = movement.rb.velocity;
                prevrot = mouseRotation.transform.eulerAngles;
            }
            else
                if (lt2 > 5)
            {
                lt2 = 0;
                SendLocationInfo();
            }
        }
    }

    public void SendLocationInfo()
    {
        Transforms transforms_ = new Transforms();
        transforms_.position = movement.transform.position;
        transforms_.velocity = movement.rb.velocity;
        transforms_.rotation = movement.GetAngles();

        PlayerData playerData = new PlayerData();
        playerData.id = playerInfo.id;
        playerData.transforms = transforms_;

        string mes = JsonUtility.ToJson(playerData);
        udp.SendString(mes);
    }

    public void SendLocationZeroVel()
    {
        Transforms transforms_ = new Transforms();
        transforms_.position = movement.transform.position;
        transforms_.velocity = Vector3.zero;
        transforms_.rotation = movement.GetAngles();

        PlayerData playerData = new PlayerData();
        playerData.id = playerInfo.id;
        playerData.transforms = transforms_;

        string mes = JsonUtility.ToJson(playerData);
        udp.SendString(mes);
    }
}

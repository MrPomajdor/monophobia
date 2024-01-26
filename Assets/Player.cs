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
    void Start()
    {
        udp = FindObjectOfType<UDPHandler>();
        
    }
    Vector3 prev, prevrot;
    // Update is called once per frame
    float lt, lt2;
    void Update()
    {
        lt += Time.deltaTime;
        lt2 += Time.deltaTime;
        if (!playerInfo.isLocal) return;

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
}

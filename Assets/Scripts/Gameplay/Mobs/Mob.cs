using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mob : MonoBehaviour
{

    //okay so a mob should have an id too, so the host can send info about the position to the clients
    public MobData mobSettings;
    ConnectionManager conMan;
    void Start()
    {
        conMan = FindAnyObjectByType<ConnectionManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (conMan.client_self.connectedPlayer.playerInfo.isHost) // I hate how that looks
        {

        }
        else
        {

        }
    }
}

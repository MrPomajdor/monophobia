using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ClientHandle
{
    public int id=-1;
    public int health=100;
    public string name;
    public Player connectedPlayer;
    public PlayerInfo ToPlayerInfo()
    {
        return new PlayerInfo { id = id, name = name };
    }
}

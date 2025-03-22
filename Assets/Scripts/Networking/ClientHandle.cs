using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ClientHandle
{
    public int id=-1;
    public int health=100;
    public int sanity=100;
    public string name;
    public Player connectedPlayer;
    public LobbyInfo lobbyInfo;
    public string steamID;
    public PlayerInfo ToPlayerInfo()
    {
        return new PlayerInfo { id = id, name = name };
    }
}

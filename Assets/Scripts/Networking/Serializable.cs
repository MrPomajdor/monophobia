
using UnityEngine;
using System;

[Serializable]
public class PlayerInfo
{
    //TODO: Add more Player Class settings if needed
    public string name;
    public int id;
    public string[] cosmetics;
    public string skin;
    public bool isHost;
    public bool isLocal;
}

[Serializable]
public class MapInfo
{
    //TODO: Add more map class settings
    public string mapName = "grid0";
    public int time = 300;
    public PlayerInfo[] players;

}


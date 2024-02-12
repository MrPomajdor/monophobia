
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
public class MiscSettings
{
    public int max_voice_distance;
}

[Serializable]
public class LobbyInfo
{
    //TODO: Add more map class settings if needed
    public string mapName = "grid0";
    public int time = 300;
    public MiscSettings miscSettings;
    public PlayerInfo[] players;
       

}



using UnityEngine;
using System;
using JetBrains.Annotations;
using System.Collections.Generic;

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
    //TODO: public int health;
    //TODO: public int sanity;

}
[Serializable]
public class MiscSettings
{
    public int max_voice_distance;
}


[Serializable]
public class ItemPosData
{
    public int id;
    public Transforms transforms;
}

[Serializable]
public class ItemInteractionInfo
{
    public int itemID;
    public bool pickedUp;
    public int pickedUpPlayerID;
    public bool activated;
}

[Serializable]
public class ItemList
{
    public List<ItemStruct> items = new List<ItemStruct>();
}
[Serializable]
public class ItemStruct
{

    public int id;
    [Header("Name should be the same as the filename in Resources")]
    public string name;
    public Transforms transforms;
    public bool canBeActivated;
    //wonder if that will be enough

}

[Serializable]
public class Tooltip
{
    public string title;
    public string description;
    public string use;
}

[Serializable]
public class LobbyInfo
{
    //TODO: Add more map class settings if needed
    public string lobbyName;
    public string mapName = "grid0";
    public int time = 300;
    public MiscSettings miscSettings;
    public PlayerInfo[] players;
       

}


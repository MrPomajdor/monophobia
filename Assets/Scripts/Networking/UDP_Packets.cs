using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;





//b'{"type":"PlayerPositionData","transforms":{"position":{"x":-0.3116506040096283,"y":1.0,"z":4.648215293884277},"euler":{"x":350.8000183105469,"y":358.1000061035156,"z":0.0},"velocity":{"x":-0.12243340164422989,"y":0.0,"z":3.6878068447113039}},"id":0}'

[Serializable]
public class UDPPacket
{
    public string type;
}

[Serializable]
public class PlayerData 
{
    public string type = "PlayerPositionData";
    public Transforms transforms;
    public int id;

}



[Serializable]
public class Transforms
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 velocity;

}


[Serializable]
public class PlayersDataPacket
{
    public string type = "OtherPlayersPositionData";
    public PlayerData[] players;

}






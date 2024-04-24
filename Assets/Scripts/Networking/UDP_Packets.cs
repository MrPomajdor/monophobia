
using JetBrains.Annotations;
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
public class Inputs
{
    public bool isSprinting;
    public bool isMoving;
    public bool isCrouching;
    public Vector3 MoveDirection;

}

[Serializable]
public class SerializableStats
{
    public float alcohol;
    public float sanity;
    //more should be in the future
    //like eg. no idea what

}
[Serializable]
public class PlayerData 
{
    public string type = "PlayerPositionData";
    public Transforms transforms;
    public Inputs inputs;
    public int id=-1;
    public SerializableStats stats;
}


[Serializable]
public class Transforms
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 target_velocity;
    public Vector3 real_velocity;

}


[Serializable]
public class PlayersDataPacket
{
    public string type = "OtherPlayersPositionData";
    public PlayerData[] players;

}


[Serializable]
public class ObjectPositionData
{
    public string type = "ObjectPositionData";
    public int id;
    public Transforms transforms;

}


[Serializable]
public class MobDataPacket
{
    public string type = "MobData";
    public int id;
    public Transforms transforms;
    public Transform target_tr;
    public Vector3 target_vec;

}

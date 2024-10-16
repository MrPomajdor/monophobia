using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Behaviour
{
    WanderAround,
    StandStill,
    RunToTarger,
    WalkToTarget,
    StalkTargerBehind,
    StalkTargetAhead
}

public enum Aggressivity
{
    Passive = 1,
    Calm = 2,
    MildlyInconvenienced = 3,
    Annoyed = 4,
    Angry = 5,
    WillKill = 6
}
public enum TargetSelectionMode
{
    Random,
    Closest,
    Farthest,
    TargetInFOV
}
public enum TargetPointMode
{
    Point,
    Player
}
public enum Personality
{
    Curious,
    Fucker,
    Dumb
}

[CreateAssetMenu(fileName = "Unnamed Mob",menuName = "Mob Settings")]
public class MobData : ScriptableObject
{
    public string mobName;
    public float health;
    public float maxHealth;
    public Behaviour FocusedBehaviour; 
    public Behaviour UnfocusedBehaviour;
    public Aggressivity aggresivness;
    public TargetSelectionMode targetSelectionMode;
    public TargetPointMode targetPointMode;
    public Personality personality;
    public bool WillJumpscare;
    public float WanderRange;
    public float WalkSpeed=1;
    public float RunSpeed=1;

}

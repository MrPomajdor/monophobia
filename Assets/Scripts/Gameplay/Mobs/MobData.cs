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
    Passive,
    Calm,
    MildlyInconvenienced,
    Annoyed,
    Angry,
    WillKill
}
public enum TargetSelectionMode
{
    Random,
    Closest,
    Farthest,
    TargetInFOV
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
    public Personality personality;
    public bool WillJumpscare;

}

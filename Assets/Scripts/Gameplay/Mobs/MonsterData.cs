using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum AbilityType
{
    Passive,
    Active
}

[CreateAssetMenu(fileName = "Unnamed Mob", menuName = "Monsters/Monster Settings")]
public class MonsterData : ScriptableObject
{
    
    public string Name;
    public string Description;
    public string CodeName;
    public Sprite SplashArt;
    public MonsterAbility[] Abilities;

    public MonsterDataSerialized dataSerialized()
    {
        MonsterDataSerialized s = new MonsterDataSerialized();
        s.CodeName = CodeName;
        return s;
    }
}


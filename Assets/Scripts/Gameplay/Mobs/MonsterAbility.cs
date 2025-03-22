using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Unnamed Ability", menuName = "Monsters/Monster Ability")]
public class MonsterAbility : ScriptableObject
{

    public string Name;
    public string Description;
    public float Cooldown;
    public int MaxUses;
    public Texture2D UITexture;
    public AbilityType Type;

}

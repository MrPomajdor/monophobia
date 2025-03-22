using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestAbility : MonsterAbilityScript
{
    public override void UseAbility()
    {
        Debug.Log(gameObject.name);
    }

}

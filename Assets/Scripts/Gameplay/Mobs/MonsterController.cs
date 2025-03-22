using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : MonoBehaviour
{
    public MonsterData monsterData;
    private MonsterAbilityScript[] _abilities;
    private Dictionary<MonsterAbility, MonsterAbilityScript> _abilitiesMap = new Dictionary<MonsterAbility, MonsterAbilityScript>();
    void Start()
    {
        _abilities = GetComponents<MonsterAbilityScript>();
        foreach (MonsterAbilityScript ability in _abilities)
        {
            _abilitiesMap.Add(ability.AbilityData, ability);
        }

    }

    public void UseAbility(MonsterAbility ability)
    {
        if (_abilitiesMap.ContainsKey(ability))
            _abilitiesMap[ability].UseAbility();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) {
            UseAbility(monsterData.Abilities[0]);
            Debug.Log(monsterData.Abilities[0].GetInstanceID());
        }
        
    }
}

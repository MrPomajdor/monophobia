using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootBox : Interactable
{
    public List<string> LootNames = new List<string>();

    public override void Interact(Player interactee)
    {
        //if(State.used) return;
        //State.used = true;

        if (!interactee.playerInfo.isHost) return;
        
        string LootName = LootNames[Random.Range(0, LootNames.Count)];
        Item spawnedLoot = Instantiate(Resources.Load<GameObject>(LootName),transform.position,transform.rotation).GetComponent<Item>();

        interactee.inventoryManager.PickUpItem(spawnedLoot);
        
        
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : Interactable
{

    public override void Interact(Player interactee)
    {
        /*
         * if interactee is local then
         *  parse message
         * else
         *  do own logic send message
         */


        Debug.Log("Interacted");
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;




public abstract class Interactable : NetworkObject
{

    

    public abstract void Interact(Player interactee);


    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    

}

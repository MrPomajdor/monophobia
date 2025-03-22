using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : Interactable
{
    [SyncVariable]
    bool open = false;
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        float dg = open ? -90f : 0f;
        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(new Vector3(0,dg,0)), Time.deltaTime*3f);
    }

    public override void Interact(Player interactee)
    {
        open = !open;
        SyncVariables();
    }


}

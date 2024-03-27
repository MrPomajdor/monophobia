using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    PickUp,
    StaticInteractive
}
public class Item : MonoBehaviour
{
    public static List<int> otherItems = new List<int>();
    public ItemType type;
    private ConnectionManager conMan;
    public ItemStruct item;
    public Tooltip tooltip;
    public bool pickedUp = false;
    public Rigidbody rb {  get; private set; }
    public Transforms transforms { get; set; } 
    void Start()
    {
        conMan = FindAnyObjectByType<ConnectionManager>();
        rb = GetComponent<Rigidbody>(); 
        if (conMan == null)
            return;
        if (conMan.IsSelfHost)
        {
            //if we are the host, set an id
            item.id = otherItems.Count;
            otherItems.Add(item.id);
        }
        else
        {
            //if not, wait for id. If me not recieve id, me self destroy
        }

    }

    void Update()
    {
        if (conMan.IsSelfHost || pickedUp)
        {
            transforms.position = transform.position;
            transforms.rotation = transform.eulerAngles;
            transforms.real_velocity = rb.velocity;
            if(!pickedUp) transforms.target_velocity = rb.velocity; //TODO: When picking up an item replace this externally
            conMan.SendItemLocationInfo(this);
        }
        else
        {
            Tools.UpdatePos(transform, rb, transforms);
        }
    }

}

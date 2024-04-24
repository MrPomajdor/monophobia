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
    public Rigidbody rb { get; private set; }
    public ItemInteractionInfo interactionInfo  = new ItemInteractionInfo();
    private float falloffTracker;
    private bool falloffHold;
    private bool se;

    [field: SerializeField]
    public float lastNetworkTime { get; private set; }
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
            //if not, me self destroy :c
            //Destroy(gameObject);

            //UPDATE:
            //this is not the way to do it. It won't work.
            //The map loader script will destroy every Item (if we are NOT a host), then send a [please give me list of the current items] packet, and the host will respond with the list!
            //(propably it will)
         

        }

    }
    public void UpdateInteractionNetwork()
    {
        conMan.SendItemInteractionInfo(interactionInfo);
    }

    void Update()
    {
        lastNetworkTime += Time.deltaTime;

        
         


        if (conMan.IsSelfHost)
        {
            if (lastNetworkTime > 0.2f && !interactionInfo.pickedUp)
            {
                lastNetworkTime = 0;
                if (item.transforms == null)
                    item.transforms = new Transforms();
                //TODO: Don't send unnececary location data if the item is stationary. The same for Player.
                item.transforms.position = transform.position;
                item.transforms.rotation = transform.eulerAngles;
                item.transforms.real_velocity = rb.velocity;
                conMan.SendItemLocationInfo(this);
            }
        }
        else
        {

            if (interactionInfo.pickedUp)
            {
                se = true;
                falloffTracker = 0;
            }
            else
            {
                se = false;
                if (falloffTracker > .5f)
                    falloffHold = false;
                else
                {
                    falloffHold = true;
                    falloffTracker += Time.deltaTime;
                }
            }

            if(!se && !falloffHold)
                Tools.UpdatePos(transform, rb, item.transforms);
        }
        

    }

   
}

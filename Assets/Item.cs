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
    public ItemStruct itemStruct;
    public Sprite thumbnail;
    public Tooltip tooltip;
    public Rigidbody rb { get; private set; }
    public ItemInteractionInfo interactionInfo = new ItemInteractionInfo();
    private float falloffTracker;
    private bool falloffHold;
    private bool se;
    public Player heldBy;
    [field: SerializeField]
    public float lastNetworkTime { get; private set; }

    public bool PickedUp = false;
    void Start()
    {
        conMan = FindAnyObjectByType<ConnectionManager>();
        rb = GetComponent<Rigidbody>();
        if (conMan == null)
            return;
        if (conMan.IsSelfHost)
        {
            //if we are the host, set an id
            itemStruct.id = otherItems.Count;
            otherItems.Add(itemStruct.id);
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
            if (lastNetworkTime > 0.2f && !PickedUp)
            {
                {
                    lastNetworkTime = 0;
                    if (itemStruct.transforms == null)
                        itemStruct.transforms = new Transforms();
                    //TODO: Don't send unnececary location data if the item is stationary. The same for Player.
                    itemStruct.transforms.position = transform.position;
                    itemStruct.transforms.rotation = transform.eulerAngles;
                    itemStruct.transforms.real_velocity = rb.velocity;
                    conMan.SendItemLocationInfo(this);
                }
            }
            else
            {
                
                /*
                if (PickedUp)
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

                if (!se && !falloffHold)
                    Tools.UpdatePos(transform, rb, itemStruct.transforms);
                */
            }


        }
        else
        {
            if (!PickedUp)
            {
                Tools.UpdatePos(transform, rb, itemStruct.transforms);
            }
        }


    }
}

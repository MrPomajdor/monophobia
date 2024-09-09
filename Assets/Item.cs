using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public enum ItemType
{
    PickUp,
    StaticInteractive
}
public class Item : MonoBehaviour
{
    public static List<int> otherItems = new List<int>();
    public ItemType type;
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
    //public Vector3 lastNetworkLocation { get; private set; }
    public bool Moved { get; private set; }

    public bool PickedUp = false;

    private void OnEnable()
    {
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.itemData[0], ParseItemTransform);
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.itemIntInf[0], ParseItemInteraction);
    }

    private void OnDisable()
    {
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.itemData[0], ParseItemTransform);
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.itemIntInf[0], ParseItemInteraction);

    }

    public void ParseItemTransform(Packet packet)
    {
        ItemPosData itemPosData = packet.GetJson<ItemPosData>();
        if (itemPosData == null)
            return;

        if(itemPosData.id == itemStruct.id)
            itemStruct.transforms = itemPosData.transforms;
    }

    public void ParseItemInteraction(Packet packet)
    {
        ItemInteractionInfo inf = packet.GetJson<ItemInteractionInfo>();
        if (inf == null)
            return;

        if (inf.itemID == itemStruct.id)
            interactionInfo = inf;
    }
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (Global.connectionManager.IsSelfHost)
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
        Global.connectionManager.SendItemInteractionInfo(interactionInfo);
    }

    void Update()
    {
        if (Global.connectionManager.client_self.connectedPlayer == null) return;

        if (Global.connectionManager.IsSelfHost)
        {
            if (rb.velocity.magnitude > 0.1f)
                Moved = true;

            lastNetworkTime += Time.deltaTime;
            if ((lastNetworkTime > 5f || (Moved && lastNetworkTime > 0.2f )) && !PickedUp)
            {
                Moved = false;

                lastNetworkTime = 0;
                if (itemStruct.transforms == null)
                    itemStruct.transforms = new Transforms();
                
                itemStruct.transforms.position = transform.position;
                itemStruct.transforms.rotation = transform.eulerAngles;
                itemStruct.transforms.real_velocity = rb.velocity;
                //lastNetworkLocation = transform.position;

                Global.connectionManager.SendItemLocationInfo(this);

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

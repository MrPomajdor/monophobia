using System.Collections.Generic;
using UnityEngine;


public enum ItemType
{
    PickUp,
    StaticInteractive
}
public abstract class Item : MonoBehaviour
{
    public static List<int> otherItems = new List<int>();
    public ItemType type;
    public ItemStruct itemStruct;
    public Sprite thumbnail;
    public Tooltip tooltip;
    public Rigidbody rb { get; private set; }
    public ItemInteractionInfo interactionInfo = new ItemInteractionInfo();
    public Player heldBy;
    [field: SerializeField]
    public float lastNetworkTime { get; private set; }
    //public Vector3 lastNetworkLocation { get; private set; }
    public bool Moved { get; private set; }

    public bool PickedUp = false;

    bool showDevInfo = false;
    bool Initialized = false;

    
    public abstract void Interact();
    public abstract void ItemStart();

    public void InternalInteract()
    {
        Interact();
    }
    public void InternalItemStart()
    {
        rb = GetComponent<Rigidbody>();
        if (!Global.connectionManager.items.Contains(this)) Global.connectionManager.mapLoader.RefreshWorldState();
        ItemStart();
        Initialized = true;
    }

    private void OnGUI()
    {
        if (!showDevInfo) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 10;
        style.normal.textColor = Color.green;

        GUI.Label(new Rect(Screen.width / 2, Screen.height / 2, 500, 1000), $"ID : {itemStruct.id}\nName : {itemStruct.name}");
    }

    private void OnMouseEnter()
    {
        showDevInfo = true;
    }

    private void OnMouseExit()
    {
        showDevInfo = false;

    }
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

        Interact();
    }
    void Start()
    {
       

        
        Global.connectionManager.AddLocalPlayerAction(InternalItemStart);

    }

    
    public void UpdateInteractionNetwork()
    {
        interactionInfo.itemID = itemStruct.id; 
        Global.connectionManager.SendItemInteractionInfo(interactionInfo);
    }

    void Update()
    {
        if (Global.connectionManager.client_self.connectedPlayer == null || Initialized==false) return;

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
                itemStruct.transforms.real_angular_velocity = rb.angularVelocity;
                //lastNetworkLocation = transform.position;

                SendItemLocationInfo();

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

    public void SendItemLocationInfo()
    {
        Transforms transforms_ = itemStruct.transforms;
        ItemPosData itemPosData = new ItemPosData();
        itemPosData.transforms = transforms_;
        itemPosData.id = itemStruct.id;


        string mes = JsonUtility.ToJson(itemPosData);
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.itemPos;
        packet.AddToPayload(mes);
        packet.Send(Global.connectionManager.udp_handler.client, Global.connectionManager.udp_handler.remoteEndPoint);
    }
}

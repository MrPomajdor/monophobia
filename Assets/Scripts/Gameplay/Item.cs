using System.Collections.Generic;
using UnityEngine;


public enum ItemType
{
    PickUp,
    StaticInteractive
}
public abstract class Item : NetworkTransform
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
        NetworkTransformID = itemStruct.id; //god dammit that shouldn't be done like that xd
        Initialized = true;
    }
    /*
    private void OnGUI()
    {
        if (!showDevInfo) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 10;
        style.normal.textColor = Color.green;

        GUI.Label(new Rect(Screen.width / 2, Screen.height / 2, 500, 1000), $"ID : {itemStruct.id}\nName : {itemStruct.name}");
    }*/

    private void OnMouseEnter()
    {
        showDevInfo = true;
    }

    private void OnMouseExit()
    {
        showDevInfo = false;

    }
    protected override void OnEnable()
    {
        base.OnEnable();
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.itemIntInf[0], ParseItemInteraction);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.itemIntInf[0], ParseItemInteraction);

    }


    public void ParseItemInteraction(Packet packet)
    {
        ItemInteractionInfo inf = new ItemInteractionInfo();
        if (!packet.GetFromPayload(inf))
            return;
        
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

    protected override void Update()
    {
        base.Update();

        HoldUpdate = !(Global.connectionManager.client_self.connectedPlayer != null || Initialized != false || PickedUp);

    }


}

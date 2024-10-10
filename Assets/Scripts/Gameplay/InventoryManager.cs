using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;



public class InventoryManager : MonoBehaviour
{
    public int InventorySize = 5;
    public bool Remote = false;
    [field: SerializeField]
    public List<Item> items { get; private set; } = new List<Item>();
    [field: SerializeField]
    public Item current { get; private set; }
    public Camera m_Camera;
    Player owner;
    [Header("UI")]
    [SerializeField]
    private Transform InventoryGroup;
    [SerializeField]
    private GameObject InventorySlot;

    void Start()
    {
    }


    private void OnEnable()
    {
        owner = GetComponent<Player>();
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.itemDrop[0], ParseItemDrop);
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.itemPickup[0], ParseItemPickup);
        Global.connectionManager.RegisterFlagReceiver(Flags.Response.inventorySwitch[0], ParseInventorySwitch);
    }
    private void OnDisable()
    {
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.itemDrop[0], ParseItemDrop);
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.itemPickup[0], ParseItemPickup);
        Global.connectionManager.UnregisterFlagReceiver(Flags.Response.inventorySwitch[0], ParseInventorySwitch);

    }

    public void ParseItemDrop(Packet packet)
    {
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int playerID = reader.ReadInt32();
            int itemID = reader.ReadInt32();

            if (playerID != owner.playerInfo.id)
                return;

            if (current.itemStruct.id == itemID)
                DropCurrent();
            else
            {
                SwitchItemByID(itemID);
                DropCurrent();
            }
        }
    }

    public void ParseInventorySwitch(Packet packet)
    {

        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int playerID = reader.ReadInt32();
            int itemID = reader.ReadInt32();

            if (playerID != owner.playerInfo.id) return;

            SwitchItemByID(itemID);
        }

    }

    public void ParseItemPickup(Packet packet)
    {

        
        using (MemoryStream _stream = new MemoryStream(packet.payload))
        using (BinaryReader reader = new BinaryReader(_stream))
        {
            int playerID = reader.ReadInt32();
            int itemID = reader.ReadInt32();

            if (playerID != owner.playerInfo.id) return;

            Item itm = Global.connectionManager.items.FirstOrDefault(x => x.itemStruct.id == itemID);
            if (itm == null) return;

            PickUpItem(itm);
            Debug.Log($"Player {playerID} picked up {itemID}");
        }

    }


    // Update is called once per frame
    void Update()
    {

        if (!owner.playerInfo.isLocal)
            return;

        float axis = Input.GetAxis("Mouse ScrollWheel");

        if (axis > 0)
            SwitchItemDirection(1);

        if (axis < 0)
            SwitchItemDirection(-1);

    }

    /// <summary>Switches selected item by direction</summary>
    /// <param name="direction">1 or -1</param>
    public void SwitchItemDirection(int direction)
    {
        if(items.Count == 0) return;

        int pos = items.IndexOf(current);
        pos+=direction;
        if (pos >= items.Count)
            pos -= items.Count - 1;

        if (pos < 0)
            pos = items.Count - 1;

        current = items[pos];

        UpdateInactive();
        SendItemSelected();
    }
    
    public void SwitchItem(int position)
    {
        if (items.Count == 0) return;

        if (position > items.Count - 1) return;

        current = items[position];

        UpdateInactive();
        SendItemSelected();
    }

    public void SwitchItem(Item target)
    {

        SwitchItemByID(target.itemStruct.id);
    }

    public void SwitchItemByID(int target)
    {
        if (items.Count == 0) return;

        Item i = items.FirstOrDefault(x => x.itemStruct.id == target);
        current = i;
        UpdateInactive();
        SendItemSelected();
    }

    private void UpdateInactive()
    {
        foreach (Item item in items)
        {
            if (item == current)
            {
                item.gameObject.SetActive(true);
                continue;
            }

            item.gameObject.SetActive(false);

        }
    }


    public void DropCurrent()
    {
        if (current != null)
        {
            current.PickedUp = false;
            current.rb.velocity = m_Camera.transform.forward * 5;

            current.transform.root.gameObject.GetComponent<Collider>().isTrigger = false;
            current.transform.root.gameObject.GetComponent<Rigidbody>().freezeRotation = false;
            items.Remove(current);

            RefreshUI();

            SendItemDrop(current);

            current = null;
        }
    }

    public void PickUpItem(Item item)
    {
        if (items.Contains(item) || items.Count >= InventorySize) return;

        items.Add(item);
        item.PickedUp = true;
        if (items.Count == 1)
        {
            SwitchItem(item);
        }

        item.transform.root.gameObject.GetComponent<Collider>().isTrigger = true;  //what the fuck
        item.transform.root.gameObject.GetComponent<Rigidbody>().freezeRotation = true;  //what the fuck

        RefreshUI();

        SendItemPickup(item);


    }

    /*
     * This was an attempt to make live thumbnail generation for items picked up, but it looks like shit (bc I don't know how to do it really)
     * Might come back to this. For now I'm sticking to premade thumbnails
     * 
    Texture2D GenerateThumbnail(GameObject obj)
    {
        Camera cam = new GameObject().AddComponent<Camera>();
        cam.transform.position = new Vector3(-500, -500, -500);
        cam.targetTexture = new RenderTexture(512, 512, 0);


        
        
        //object setup
        GameObject tempObj = Instantiate(obj);
        tempObj.isStatic = true;
        tempObj.transform.position = cam.transform.position + cam.transform.forward*1.5f;
        tempObj.transform.eulerAngles = new Vector3(0,90,0);

        //Camera setup
        cam.orthographic = true;
        cam.farClipPlane = 5;
        
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (MeshFilter f in tempObj.GetComponentsInChildren<MeshFilter>())
        {
            Debug.Log($"Encapsulating {f.gameObject.name}");
            Bounds b = f.mesh.bounds;
            b.size *= f.transform.localScale.magnitude;
            bounds.Encapsulate(b);
        }

        cam.orthographicSize = bounds.max.magnitude;


        var currentRT = RenderTexture.active;
        RenderTexture.active = cam.targetTexture;

        cam.Render();
        GameObject pr = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pr.transform.localScale = bounds.size;
        pr.transform.position = tempObj.transform.position;
        Texture2D img = new Texture2D(cam.targetTexture.width, cam.targetTexture.height);
        img.ReadPixels(new Rect(0, 0, cam.targetTexture.width, cam.targetTexture.height), 0, 0);
        img.Apply();

        RenderTexture.active = currentRT;

        //cleanup
        //Destroy(cam);
        //Destroy(tempObj);

        return img;
    } */

    private void RefreshUI()
    {
        //DESTROY ALL THE CHILDREN!!!
        foreach (Transform t in InventoryGroup)
        {
            Destroy(t.gameObject);
        }

        foreach (Item item in items)
        {
            InventorySlotManager slotManager = Instantiate(InventorySlot, InventoryGroup.transform).GetComponent<InventorySlotManager>();

            slotManager.SetThumbnail(item.thumbnail);
        }
    }

    private void SendItemPickup(Item item)
    {
        if (Remote) return;
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.itemPickup;
        packet.AddToPayload(item.itemStruct.id);
        packet.Send(Global.connectionManager.stream);
    }


    private void SendItemDrop(Item item)
    {
        if (Remote) return;
        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.itemDrop;
        packet.AddToPayload(item.itemStruct.id);
        packet.Send(Global.connectionManager.stream);
    }

    private void SendItemSelected()
    {
        if (Remote) return;
        if (current == null) return;

        Packet packet = new Packet();
        packet.header = Headers.data;
        packet.flag = Flags.Post.inventorySwitch;
        packet.AddToPayload(current.itemStruct.id);
        packet.Send(Global.connectionManager.stream);
    }
}

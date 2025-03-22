using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class ItemInteraction : MonoBehaviour
{
    private Camera m_Camera;
    private Player SelfPlayer;
    private InventoryManager SelfInventory;
    [SerializeField]
    private float m_maxRaycastDistance = 3;
    public Transform holdPoint;
    public bool remotePickedUp { get; private set; }
    public bool remote;

   

    void Start()
    {
        m_Camera = GetComponent<Camera>();
        SelfPlayer = transform.root.GetComponent<Player>();
        transform.root.TryGetComponent<InventoryManager>(out SelfInventory);
    }



    // Update is called once per frame
    void Update()
    {
        


        if (remote) return; //-----------------------------LOCAL CODE BELOW----------------------------

        if (SelfInventory!= null && Input.GetKeyDown(KeyCode.Mouse0) && SelfInventory.current!=null)
        {

            SelfInventory.current.InternalInteract();
            SelfInventory.current.UpdateInteractionNetwork();

        }
        if (Input.GetKeyDown(KeyCode.F))
        {

            RaycastHit hit;
            if (Physics.Raycast(transform.position, transform.forward, out hit, m_maxRaycastDistance))
            {
                print(hit.collider.gameObject.name);
                if (hit.collider == null) return;
                
                if(hit.collider.gameObject.GetComponent<Interactable>()) {
                    hit.collider.gameObject.GetComponent<Interactable>().Interact(SelfPlayer);
                }
                
                else if (SelfInventory != null && hit.collider.gameObject.GetComponent<Item>())
                {
                    Item _itm = hit.collider.gameObject.GetComponent<Item>();

                    if (_itm.PickedUp) return; //this is for checking if another player is currentyl holding it. If yes, go fuck yourself.

                    switch (_itm.type)
                    {
                        case ItemType.PickUp:
                            SelfInventory.PickUpItem(_itm);
                            return;

                        case ItemType.StaticInteractive:
                            return;
                    }
                }
            }
        }

        if(SelfInventory != null && Input.GetKeyDown(KeyCode.G)) {
            if (SelfInventory.current != null)
            {
                SelfInventory.DropCurrent();
                return;
            }
        }
    }

   
}
